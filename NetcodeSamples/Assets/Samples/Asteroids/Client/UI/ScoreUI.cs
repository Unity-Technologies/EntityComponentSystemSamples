using System;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;
using UnityEngine.UI;

namespace Samples.Asteroids.Client.UI
{
    public class ScoreUI : MonoBehaviour
    {
        Text m_ScoreTextToUpdate;
        EntityQuery m_ScoreQuery;

        void Start()
        {
            m_ScoreTextToUpdate = GetComponent<Text>();
        }

        void Update()
        {
            if (ClientServerBootstrap.ClientWorld == null)
                return;
            if (m_ScoreQuery == default)
                m_ScoreQuery = ClientServerBootstrap.ClientWorld.EntityManager.CreateEntityQuery(typeof(AsteroidScore));
            if (m_ScoreQuery.IsEmpty)
                return;
            var score = m_ScoreQuery.GetSingleton<AsteroidScore>().Value;
            m_ScoreTextToUpdate.text = "Score: " + score;
        }
    }
}
