using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

public class AnimateGameObjectColor : MonoBehaviour
{
    public int spawnIndex
    {
        get { return m_spawnIndex; }
        set { m_spawnIndex = value; }
    }
    private int m_spawnIndex = 0;

    private Renderer m_renderer;
    private Color m_currentColor;
 
    void Start()
    {
        m_renderer = GetComponent<Renderer>();
        m_currentColor = m_renderer.material.GetColor("_Color");
    }

    void Update()
    {
        var mode = SimulationMode.getCurrentMode();
        if ((mode.type == SimulationMode.ModeType.Color) ||
            (mode.type == SimulationMode.ModeType.PositionAndColor))
        {
            float indexAdd = 0.0123f * (float) spawnIndex;
            var delta = new float3(math.cos(mode.time + indexAdd),
                            math.sin(mode.time + indexAdd), 0) * mode.deltaTime;
 
            m_currentColor = m_currentColor + new Color(delta.x, delta.y, delta.z);
            m_renderer.material.SetColor("_Color", m_currentColor);
        }
    }
}
