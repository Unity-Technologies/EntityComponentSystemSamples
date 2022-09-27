using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.SceneManagement;
using Unity.PerformanceTesting;
using UnityEngine.Profiling;
using NUnit.Framework;

//documentation:
//https://docs.unity3d.com/Packages/com.unity.test-framework.performance@1.3/manual/index.html?_ga=2.60015021.168522641.1574682439-1601175435.1544520344
public class PerformanceTests
{
	public static string[] ScenesToTest()
	{
		return new[]
		{
			"BatchingBenchmark"
		};
	}

    [UnityTest, Performance]
    [Timeout(1500000)]
    [Category("Performance")]
    public IEnumerator Test_PerformanceTests([ValueSource("ScenesToTest")]string sceneName)
    {
	    SceneManager.LoadScene(sceneName,LoadSceneMode.Single);
	    yield return null;

		//We don't want to run test if there is no setting
		var settings = Object.FindObjectOfType<PerformanceTestSettings>();
		if(settings == null) Assert.Ignore("Ignored because PerformanceTestSettingsCustom cannot be found in the scene.");
		
		//Run setup
		settings.SetUp();
		
		//Do frame delay manually
		for (int i = 0; i < settings.frameDelay; i++) yield return null;

		//SAMPLE BEFORE RUN ==================================
		//Memory
	    Measure.Custom(settings.def_allocatedGfxMem, Profiler.GetAllocatedMemoryForGraphicsDriver() / 1048576f);
	    Measure.Custom(settings.def_allocatedMem, Profiler.GetTotalAllocatedMemoryLong() / 1048576f);
		
		//RUN ==================================
		//Profiler
        yield return Measure.Frames()
		//.WarmupCount(settings.warmupCount) //default is 1
		.ProfilerMarkers(settings.def_profiler)
		.MeasurementCount(settings.measureCount)
		.Run();

		//SAMPLE AFTER RUN ===================================
		//Memory
	    Measure.Custom(settings.def_allocatedGfxMem, Profiler.GetAllocatedMemoryForGraphicsDriver() / 1048576f);
	    Measure.Custom(settings.def_allocatedMem, Profiler.GetTotalAllocatedMemoryLong() / 1048576f);
    }
}