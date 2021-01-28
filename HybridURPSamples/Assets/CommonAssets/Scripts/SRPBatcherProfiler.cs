using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine.Profiling;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering
{
    public class SRPBatcherProfiler : MonoBehaviour
    {
        public bool m_Enable = true;
        private const float kAverageStatDuration = 1.0f;            // stats refresh each second
        private int m_frameCount;
		private float m_AccDeltaTime;
        private string m_statsLabel;
        private GUIStyle m_style;
        private bool m_oldBatcherEnable;

        internal class RecorderEntry
        {
            public string name;
            public string oldName;
            public int callCount;
            public float accTime;
            public Recorder recorder;
        };

		enum SRPBMarkers
		{
			kStdRenderDraw,
			kStdShadowDraw,
			kSRPBRenderDraw,
			kSRPBShadowDraw,
			kRenderThreadIdle,
			kStdRenderApplyShader,
			kStdShadowApplyShader,
			kSRPBRenderApplyShader,
			kSRPBShadowApplyShader,
			kPrepareBatchRendererGroupNodes,
		};
		
        RecorderEntry[] recordersList =
        {
			// Warning: Keep that list in the exact same order than SRPBMarkers enum
            new RecorderEntry() { name="RenderLoop.Draw" },
            new RecorderEntry() { name="Shadows.Draw" },
            new RecorderEntry() { name="SRPBatcher.Draw", oldName="RenderLoopNewBatcher.Draw" },
            new RecorderEntry() { name="SRPBatcherShadow.Draw", oldName="ShadowLoopNewBatcher.Draw" },
            new RecorderEntry() { name="RenderLoopDevice.Idle" },
            new RecorderEntry() { name="StdRender.ApplyShader" },
            new RecorderEntry() { name="StdShadow.ApplyShader" },
            new RecorderEntry() { name="SRPBRender.ApplyShader" },
            new RecorderEntry() { name="SRPBShadow.ApplyShader" },
            new RecorderEntry() { name="PrepareBatchRendererGroupNodes" },
        };

        void Awake()
        {
            for (int i = 0; i < recordersList.Length; i++)
            {
                var sampler = Sampler.Get(recordersList[i].name);
                if (sampler.isValid)
                    recordersList[i].recorder = sampler.GetRecorder();
				else if ( recordersList[i].oldName != null )
				{
					sampler = Sampler.Get(recordersList[i].oldName);
					if (sampler.isValid)
						recordersList[i].recorder = sampler.GetRecorder();
				}
            }

            m_style =new GUIStyle();
            m_style.fontSize = 15;
            m_style.normal.textColor = Color.white;
            m_oldBatcherEnable = m_Enable;

            ResetStats();
        }

        void RazCounters()
        {
            m_AccDeltaTime = 0.0f;
            m_frameCount = 0;
            for (int i = 0; i < recordersList.Length; i++)
            {
                recordersList[i].accTime = 0.0f;
                recordersList[i].callCount = 0;
            }
        }

        void    ResetStats()
        {
			m_statsLabel = "Gathering data...";
            RazCounters();
        }
        
        void ToggleStats()
        {
            m_Enable = !m_Enable;
            ResetStats();
        }

        void Update()
        {

            if (Input.GetKeyDown(KeyCode.F9))
            {
                GraphicsSettings.useScriptableRenderPipelineBatching = !GraphicsSettings.useScriptableRenderPipelineBatching;
            }

            if (GraphicsSettings.useScriptableRenderPipelineBatching != m_oldBatcherEnable )
            {
                ResetStats();
                m_oldBatcherEnable = GraphicsSettings.useScriptableRenderPipelineBatching;
            }

            if (Input.GetKeyDown(KeyCode.F8))
            {
                ToggleStats();
            }

			if (m_Enable)
			{

				bool SRPBatcher = GraphicsSettings.useScriptableRenderPipelineBatching;

				m_AccDeltaTime += Time.unscaledDeltaTime;
				m_frameCount++;

				// get timing & update average accumulators
				for (int i = 0; i < recordersList.Length; i++)
				{
					if ( recordersList[i].recorder != null )
					{
						recordersList[i].accTime += recordersList[i].recorder.elapsedNanoseconds / 1000000.0f;      // acc time in ms
						recordersList[i].callCount += recordersList[i].recorder.sampleBlockCount;
					}
				}

				if (m_AccDeltaTime >= kAverageStatDuration)
				{

					float ooFrameCount = 1.0f / (float)m_frameCount;

					float avgStdRender = recordersList[(int)SRPBMarkers.kStdRenderDraw].accTime * ooFrameCount;
					float avgStdShadow = recordersList[(int)SRPBMarkers.kStdShadowDraw].accTime * ooFrameCount;
					float avgSRPBRender = recordersList[(int)SRPBMarkers.kSRPBRenderDraw].accTime * ooFrameCount;
					float avgSRPBShadow = recordersList[(int)SRPBMarkers.kSRPBShadowDraw].accTime * ooFrameCount;
					float RTIdleTime = recordersList[(int)SRPBMarkers.kRenderThreadIdle].accTime * ooFrameCount;
					float avgPIRPrepareGroupNodes = recordersList[(int)SRPBMarkers.kPrepareBatchRendererGroupNodes].accTime * ooFrameCount;

					m_statsLabel = string.Format("Accumulated time for RenderLoop.Draw and ShadowLoop.Draw (all threads)\n{0:F2}ms CPU Rendering time ( incl {1:F2}ms RT idle )\n", avgStdRender + avgStdShadow + avgSRPBRender + avgSRPBShadow + avgPIRPrepareGroupNodes, RTIdleTime);
					if (SRPBatcher)
					{
						m_statsLabel += string.Format("  {0:F2}ms SRP Batcher code path\n", avgSRPBRender + avgSRPBShadow);
						m_statsLabel += string.Format("    {0:F2}ms All objects ( {1} ApplyShader calls )\n", avgSRPBRender, recordersList[(int)SRPBMarkers.kSRPBRenderApplyShader].callCount / m_frameCount);
						m_statsLabel += string.Format("    {0:F2}ms Shadows ( {1} ApplyShader calls )\n", avgSRPBShadow, recordersList[(int)SRPBMarkers.kSRPBShadowApplyShader].callCount / m_frameCount);
					}
					m_statsLabel += string.Format("  {0:F2}ms Standard code path\n", avgStdRender + avgStdShadow);
					m_statsLabel += string.Format("    {0:F2}ms All objects ( {1} ApplyShader calls )\n", avgStdRender, recordersList[(int)SRPBMarkers.kStdRenderApplyShader].callCount / m_frameCount);
					m_statsLabel += string.Format("    {0:F2}ms Shadows ( {1} ApplyShader calls )\n", avgStdShadow, recordersList[(int)SRPBMarkers.kStdShadowApplyShader].callCount / m_frameCount);
					m_statsLabel += string.Format("  {0:F2}ms PIR Prepare Group Nodes ( {1} calls )\n", avgPIRPrepareGroupNodes, recordersList[(int)SRPBMarkers.kPrepareBatchRendererGroupNodes].callCount / m_frameCount);
					m_statsLabel += string.Format("Global Main Loop: {0:F2}ms ({1} FPS)\n", m_AccDeltaTime * 1000.0f * ooFrameCount, (int)(((float)m_frameCount) / m_AccDeltaTime));

					RazCounters();
				}

			}
        }

        void OnGUI()
        {
            float offset = 50;
            if (m_Enable)
            {
                bool SRPBatcher = GraphicsSettings.useScriptableRenderPipelineBatching;

                GUI.color = new Color(1, 1, 1, 1);
                float w = 700, h = 256;
                offset += h + 50;

                if ( SRPBatcher )
                    GUILayout.BeginArea(new Rect(32, 50, w, h), "SRP batcher ON (F9)", GUI.skin.window);
                else
                    GUILayout.BeginArea(new Rect(32, 50, w, h), "SRP batcher OFF (F9)", GUI.skin.window);

                GUILayout.Label(m_statsLabel, m_style);

                GUILayout.EndArea();
            }
        }
    }
}
