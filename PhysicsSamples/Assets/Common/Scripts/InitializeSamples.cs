using Unity.Entities;
using UnityEngine;

// This class sets the upper limit of FPS of a demo that is being run.
// It is used as a workaround for JobTempAlloc issues on CI and to make
// measuring performance easier.
public partial struct InitializeSamplesSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        // Currently, JobTempAlloc is raised also when a job takes more than 4 frames to complete, independent of allocation.
        // Setting the target frame rate to 60 means giving each frame more time to complete, therefore the jobs would complete in less than 4 frames.
        // Also, since FPS will be lower, there will be more frames with physics in them making perf measurements easier.
        Application.targetFrameRate = 60;

        // Disabling updates because the system has nothing to do in OnUpdate method
        state.Enabled = false;
    }
}
