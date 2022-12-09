using System.Collections;
using System.Collections.Generic;
using Unity.PerformanceTesting;
using UnityEngine;
using Unity.Entities;
using Unity.Scenes;

public class PerformanceTestSettings : MonoBehaviour
{
    public int frameDelay = 10;
    public int measureCount = 200;

    [Header("Profiler Markers")]
    public SampleUnit unitProfiler = SampleUnit.Millisecond;
    public string[] profilerMarkers;

    [Header("AllocatedMemory")]
    public SampleUnit unitAllocatedMem = SampleUnit.Megabyte;

    [Header("AllocatedMemoryForGraphicsDevice")]
    public SampleUnit unitAllocatedGfxDriverMem = SampleUnit.Megabyte;
    
    [HideInInspector] public SampleGroup[] def_profiler;
    [HideInInspector] public SampleGroup def_allocatedMem;
    [HideInInspector] public SampleGroup def_allocatedGfxMem;

    private bool doneSetUp = false;

    public void SetUp()
    {
        if(!doneSetUp)
        {
            def_profiler = new SampleGroup[profilerMarkers.Length];
            for(int i=0; i< def_profiler.Length; i++)
            {
                def_profiler[i] = new SampleGroup(profilerMarkers[i],unitProfiler,false);
            }

            def_allocatedGfxMem = new SampleGroup("TotalAllocatedMemoryForGraphicsDriver", unitAllocatedGfxDriverMem,false);
            def_allocatedMem = new SampleGroup("TotalAllocatedMemory", unitAllocatedMem,false);

            doneSetUp = true;
        }
    }

    void OnDestroy()
    {
        CleanUp();
    }

    void OnDisable()
    {
        CleanUp();
    }

    public void CleanUp()
    {
        if(World.DefaultGameObjectInjectionWorld!=null)
        {
            var world = World.DefaultGameObjectInjectionWorld;
            var entityManager = world.EntityManager;
            var entities = entityManager.GetAllEntities();

            for (int i = 0; i < entities.Length; i++)
            {
                string ename = entityManager.GetName(entities[i]);
                bool isSubscene = entityManager.HasComponent<SubScene>(entities[i]);
                bool isSceneSection = entityManager.HasComponent<SceneSection>(entities[i]);

                //Runtime generated entities requires manual deletion, 
                //but we need to skip for some specific entities otherwise there will be spamming error
                if( ename != "SceneSectionStreamingSingleton" && !isSubscene && !isSceneSection && !ename.Contains("GameObject Scene:") )
                {
                    entityManager.DestroyEntity(entities[i]);
                }
            }
        }

    }
}