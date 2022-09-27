using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestModeToggler : MonoBehaviour
{
    private List<GameObject> m_Targets;
    private int m_Active;

    // Start is called before the first frame update
    void Start()
    {
        var targets = FindObjectsOfType<TestModeToggleTarget>(true);
        m_Targets = new List<GameObject>(targets.Length);
        foreach (var t in targets)
        {
            if (t.targetEnabled)
                m_Targets.Add(t.gameObject);
            else
                t.gameObject.SetActive(false);
        }

        // Sort the objects by name so they are in consistent order
        m_Targets.Sort((GameObject a, GameObject b) => a.name.CompareTo(b.name));

        SetTarget(0);
    }

    private void SetTarget(int t)
    {
        if (t >= m_Targets.Count)
            t = 0;

        m_Active = t;

        for (int i = 0; i < m_Targets.Count; ++i)
        {
            var go = m_Targets[i];
            if (i == m_Active)
            {
                Debug.Log($"Target: {go}");
                go.SetActive(true);
            }
            else
            {
                go.SetActive(false);
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        bool click = false;

        if (Input.GetMouseButtonDown(0))
            click = true;

        if (Input.touchCount > 0)
        {
            if (Input.GetTouch(0).phase == TouchPhase.Began)
                click = true;
        }

        if (click)
            SetTarget(m_Active + 1);
    }
}
