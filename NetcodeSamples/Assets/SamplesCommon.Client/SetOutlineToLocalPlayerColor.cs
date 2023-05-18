using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Unity.NetCode.Samples.Common
{
    public class SetOutlineToLocalPlayerColor : MonoBehaviour
    {
        public static List<SetOutlineToLocalPlayerColor> All = new List<SetOutlineToLocalPlayerColor>(0);
        public static int ListVersion;

        public float alpha = 1f;
        public Outline graphic;

        void Awake()
        {
            All.Add(this);
            ListVersion++;
        }

        void OnDestroy()
        {
            All.Remove(this);
        }

        public void Refresh(Color color)
        {
            color.a = alpha;
            graphic.effectColor = color;
        }
    }
}
