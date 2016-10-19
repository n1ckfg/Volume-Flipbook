﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using hypercube;
using UnityEngine;
using UnityEngine.UI;

public class FlipPanel
{
    public GameObject gameObject;
    public Transform transform;
    public MeshFilter mf;
    public MeshRenderer mr;
    public Texture2D tex;
}

public class FlipSlice
{
    public GameObject gameObject;
    public Transform transform;
    public List<FlipPanel> fp;
}

public class FlipMaster : MonoBehaviour
{
    #region fields
    public GameObject flipSlicePrefab;
    public GameObject flipPanelPrefab;
    [Range(2, 1024)]
    public int panelWidth = 256;
    [Range(2, 1024)]
    public int panelHeight = 128;
    hypercubeCamera hypercube;
    castMesh castmesh;
    public List<FlipSlice> flipSlices;
    List<List<int>> timetable;
    //this range will have to adjust based on how many slices there are in newer machines
    [Range(0, 9)]
    public int currentSlice;
    public int currentFrame;
    //is hardcoded for now
    int flipSliceCount = 10;
    public int tempSlice;
    Drawing drawing;

    //Controls
    public enum FlipControls
    {
        General,
        Palette,
        Play,
        Save,
        Load
    }
    public FlipControls flipControls;
    public KeyCode changeSliceForward = KeyCode.RightBracket;
    public KeyCode changeSliceBack = KeyCode.LeftBracket;
    Palette palette;
    SaveUI saveUI;

    //Hacky mapping correction
    float correctionRatio = 0.8f;
    #endregion

    // Use this for initialization
    void Start()
    {
        hypercube = FindObjectOfType<hypercubeCamera>();
        castmesh = FindObjectOfType<castMesh>();
        palette = FindObjectOfType<Palette>();
        drawing = FindObjectOfType<Drawing>();
        saveUI = FindObjectOfType<SaveUI>();

        NewFlipbook();
    }

    void Update()
    {
        if (flipControls == FlipControls.General)
        {
            if (Input.GetKeyDown(changeSliceForward))
                SafeIncrement(ref currentSlice, -1, flipSlices.Count);
            if (Input.GetKeyDown(changeSliceBack))
                SafeIncrement(ref currentSlice, 1, flipSlices.Count);
        }

        //Play
        if (Input.GetKeyDown(KeyCode.Return))
        {
            if (flipControls == FlipControls.General)
            {
                SetFlipControls(FlipControls.Play);
                StartCoroutine("PlayAnimation");
            }
            else
            {
                RevertControlsToGeneral();
            }

            print("State is now " + flipControls);
        }

        #region test with frames
        if (Input.GetKeyDown(KeyCode.B))
        {
            print("New blank frame added");
            AddFrame(true);
        }

        if (Input.GetKeyDown(KeyCode.C))
        {
            print("Duplicate frame added");
            AddFrame(false);
        }

        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            SafeIncrement(ref currentFrame, 1, GetFrameCount());
            UpdateFrames();
            print("Moved to frame " + currentFrame);
            print(GetFrameCount());
        }
        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            SafeIncrement(ref currentFrame, -1, GetFrameCount());
            UpdateFrames();
            print("Moved to frame " + currentFrame);
            print(GetFrameCount());
        }
        #endregion
    }

    public void RevertControlsToGeneral()
    {
        flipControls = FlipControls.General;
        var dipshows = FindObjectsOfType<DipShow>();
        foreach (DipShow dipshow in dipshows)
        {
            dipshow.Toggle();
        }
        var inputFields = FindObjectsOfType<InputField>();
        foreach (InputField inputField in inputFields)
        {
            inputField.enabled = false;
        }
        StopCoroutine("PlayAnimation");
        drawing.ResetRefs();
    }

    public void RevertControlsAtEndOfFrame()
    {
        StartCoroutine("RevertEOF");
    }

    IEnumerator RevertEOF()
    {
        yield return new WaitForEndOfFrame();
        RevertControlsToGeneral();
    }

    public void NewFlipbook(string[] slicePaths = null)
    {
        #region scaling to fit in hypercube

        transform.localScale = hypercube.transform.localScale;
        var hyperRatio = hypercube.transform.localScale.x / hypercube.transform.localScale.y;
        var panelRatio = (float)panelWidth / panelHeight;
        if (hyperRatio > panelRatio)
        {
            transform.localScale = transform.localScale.SetY(hypercube.transform.localScale.y);
            transform.localScale = transform.localScale.SetX(transform.localScale.y * panelRatio);
        }
        else
        {
            transform.localScale = transform.localScale.SetX(hypercube.transform.localScale.x);
            transform.localScale = transform.localScale.SetY(transform.localScale.x / panelRatio);
        }

        transform.localScale = transform.localScale.SetX(transform.localScale.x * correctionRatio);

        #endregion

        //Clear children if there are any.
        for (int i = 0; i < transform.childCount; i++)
        {
            Destroy(transform.GetChild(i).gameObject);
        }

        //create the slices and their panels, and store them in flipSlices
        flipSlices = new List<FlipSlice>();
        timetable = new List<List<int>>();

        if (slicePaths != null)
            flipSliceCount = slicePaths.Length;

        for (int i = 0; i < flipSliceCount; i++)
        {
            //make a slice
            flipSlices.Add(NewFlipSlice(i));

            //make a first panel for that slice
            if (slicePaths == null)
                flipSlices[i].fp.Add(NewFlipPanel(flipSlices[i]));
            else
            {
                var pngPaths = Directory.GetFiles(slicePaths[i]);
                for (int j = 0; j < pngPaths.Length; j++)
                {
                    flipSlices[i].fp.Add(NewFlipPanel(flipSlices[i], pngPaths[j]));
                }
            }

            currentSlice = 0;
            currentFrame = 0;
            UpdateFrames();
            drawing.ResetRefs();

            //add an entry to the timetable
            timetable.Add(new List<int>());
        }
    }

    public void SetFlipControls(FlipControls flipControlsIn)
    {
        RevertControlsToGeneral();
        flipControls = flipControlsIn;
        drawing.ResetRefs();
    }

    public void AddFrame(bool blankFrame)
    {
        for (int i = 0; i < flipSliceCount; i++)
        {
            flipSlices[i].fp.Insert(currentFrame + 1, NewFlipPanel(flipSlices[i]));
            if (!blankFrame)
            {
                flipSlices[i].fp[currentFrame + 1].tex.SetPixels(flipSlices[i].fp[currentFrame].tex.GetPixels());
            }
            flipSlices[i].fp[currentFrame + 1].tex.Apply();
        }
        currentFrame += 1;
        UpdateFrames();
    }

    public IEnumerator PlayAnimation()
    {
        var framerate = 12f / 60f;
        var frameWait = new WaitForSeconds(framerate);
        while (true)
        {
            UpdateFrames();
            yield return frameWait;
            SafeIncrement(ref currentFrame, 1, GetFrameCount());
        }
    }

    public void UpdateFrames()
    {
        for (int i = 0; i < flipSlices.Count; i++)
        {
            for (int j = 0; j < flipSlices[i].fp.Count; j++)
            {
                var active = j == currentFrame;
                flipSlices[i].fp[j].gameObject.SetActive(active);
            }
        }
    }

    FlipSlice NewFlipSlice(int slice)
    {
        //slice distance is usually 1 slice distance unless the amount of slices in the machine is at least twice as many as in the animation, then it goes up to 2, etc. 
        var sliceDistance = Mathf.Floor((float)flipSliceCount / castmesh.slices) / castmesh.slices;

        var fsGameObject = (GameObject)Instantiate(flipSlicePrefab, transform);
        fsGameObject.transform.localPosition += Vector3.forward *
                                             (sliceDistance * (slice - flipSliceCount * 0.5f) + 0.5f * sliceDistance);
        fsGameObject.transform.localScale = Vector3.one;
        fsGameObject.name = "Flip Slice " + slice;

        var flipSlice = new FlipSlice()
        {
            gameObject = fsGameObject,
            transform = fsGameObject.transform,
            fp = new List<FlipPanel>()
        };

        return flipSlice;
    }

    FlipPanel NewFlipPanel(FlipSlice flipSlice, string pngPath = null)
    {
        var fpGameObject = (GameObject)Instantiate(flipPanelPrefab, flipSlice.transform);
        fpGameObject.transform.localPosition = Vector3.zero;
        fpGameObject.transform.localScale = Vector3.one;
        fpGameObject.name = "Flip Panel";

        var flipPanel = new FlipPanel
        {
            gameObject = fpGameObject,
            transform = fpGameObject.transform,
            mf = fpGameObject.GetComponent<MeshFilter>(),
            mr = fpGameObject.GetComponent<MeshRenderer>(),
            tex = new Texture2D(panelWidth, panelHeight, TextureFormat.ARGB32, false, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            }
        };

        if (pngPath != null && Path.GetExtension(pngPath).ToLower() == ".png" && File.Exists(pngPath))
        {
            var pngBytes = File.ReadAllBytes(pngPath);
            flipPanel.tex.LoadImage(pngBytes);
        }
        else
        {
            var colors = new Color[flipPanel.tex.width * flipPanel.tex.height];
            for (int i = 0; i < colors.Length; i++)
            {
                colors[i] = Color.black;
            }
            flipPanel.tex.SetPixels(colors);
        }

        flipPanel.tex.Apply();
        flipPanel.mr.material.mainTexture = flipPanel.tex;

        return flipPanel;
    }

    void SafeIncrement(ref int index, int i, int count)
    {
        index += i;
        while (index >= count)
            index -= count;
        while (index < 0)
            index += count;
    }

    public int GetFrameCount()
    {
        var frameCount = flipSlices[0].fp.Count;
        return frameCount;
    }
}