﻿using System;
using UnityEngine;
using System.Collections;

[Serializable]
public class RadialWedge
{
    public string wedgeName;
    public Color color;
    public Sprite sprite;
    public Transform trans;
    public RadialMenuWedge rw;
}

public class RadialMenu : MonoBehaviour
{
    public GameObject radialWedge;
    public Transform cursor;
    public AnimationCurve animCurve;
    public RadialWedge[] wedges;
    float wedgeSize;
    float timer;
    float speed = 5f;
    bool showing;


    // Use this for initialization
    void Start()
    {
        GenerateMenu();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            showing = !showing;
            StopCoroutine("SpreadMenu");
            StartCoroutine("SpreadMenu");

            if (showing)
            {
                transform.position = new Vector3(cursor.position.x, cursor.position.y, transform.position.z);
            }
        }


        var pos = cursor.position - transform.position;
        var angle = Mathf.Atan2(pos.x, -pos.y);
        angle = 1f - (angle / Mathf.PI + 1) / 2f;
        var speedWedge = 3f;

        var w = Mathf.FloorToInt(wedges.Length * angle);
        for (int i = 0; i < wedges.Length; i++)
        {
            if (i != w || !showing)
                wedges[i].trans.localScale = Vector3.MoveTowards(wedges[i].trans.localScale, Vector3.one,
                    Time.deltaTime * speedWedge);
            if (Mathf.Approximately(timer, 0f))
            {
                wedges[i].trans.localScale = Vector3.one;
            }
        }

        if (Mathf.Approximately(timer, 1f))
        {
            wedges[w].trans.localScale = Vector3.MoveTowards(wedges[w].trans.localScale, Vector3.one * 1.2f,
                Time.deltaTime * speedWedge);
        }
    }

    public void GenerateMenu()
    {
        if (wedges.Length == 0)
            return;

        wedgeSize = 1f / wedges.Length;

        for (int i = 0; i < wedges.Length; i++)
        {
            var wedge = (GameObject)Instantiate(radialWedge, transform, false);
            wedges[i].rw = wedge.GetComponent<RadialMenuWedge>();
            wedge.GetComponent<MeshRenderer>().material.color = wedges[i].color;
            wedges[i].trans = wedge.transform;
        }
    }

    public IEnumerator SpreadMenu()
    {
        while (!Mathf.Approximately(timer, showing ? 1 : 0))
        {
            timer += Time.deltaTime * speed * (showing ? 1 : -1);

            for (int i = 0; i < wedges.Length; i++)
            {
                var ws = Mathf.Lerp(0f, wedgeSize, animCurve.Evaluate(timer));
                var wsi = Mathf.Lerp(0f, wedgeSize * i, animCurve.Evaluate(timer));
                wedges[i].rw.SetPointRot(ws, wsi);
            }

            timer = Mathf.Clamp01(timer);

            yield return new WaitForEndOfFrame();
        }
    }
}
