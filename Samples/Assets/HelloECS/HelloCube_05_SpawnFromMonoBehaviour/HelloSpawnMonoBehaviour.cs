﻿using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Samples.HelloCube_05
{
    public class HelloSpawnMonoBehaviour : MonoBehaviour
    {
        public GameObject Prefab;
        public int CountX = 100;
        public int CountY = 100;
    
        void Start()
        {
            // Create entity prefab from the game object hierarchy once
            Entity prefab = GameObjectConversionUtility.ConvertGameObjectHierarchy(Prefab, World.Active);
            var entityManager = World.Active.EntityManager;
        
            for (int x = 0; x < CountX; x++)
            {
                for (int y = 0; y < CountY; y++)
                {
                    // Efficiently instantiate a bunch of entities from the already converted entity prefab
                    var instance = entityManager.Instantiate(prefab);
                
                    // Place the instantiated entity in a grid with some noise
                    var position = transform.TransformPoint(new float3(x * 1.3F, noise.cnoise(new float2(x, y) * 0.21F) * 2, y * 1.3F));
                    entityManager.SetComponentData(instance, new Translation {Value = position});
                }
            }
        }
    }    
}

