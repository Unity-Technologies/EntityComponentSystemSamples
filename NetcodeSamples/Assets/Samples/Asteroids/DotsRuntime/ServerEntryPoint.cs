#if UNITY_DOTSRUNTIME
using Unity.Platforms;
using Unity.Runtime;
using Unity.Runtime.EntryPoint;
using Unity.Logging;

namespace DOTSRuntime.Server
{
    public class ServerMain
    {
        public static void Main()
        {
            Program.Initialize();
            var unity = UnityInstance.Initialize();

            TempMemoryScope.EnterScope();
            Unity.Logging.DefaultSettings.Initialize();
            Log.Info("Logging initialized...");
            TempMemoryScope.ExitScope();

            unity.OnTick = (double timestampInSeconds) =>
            {
                UnityInstance.UpdatePreFrame();
                Unity.Logging.DefaultSettings.UpdateFunction();
                var shouldContinue = unity.Update(timestampInSeconds);
                UnityInstance.UpdatePostFrame(shouldContinue);

                return shouldContinue;
            };
            PlatformEvents.OnQuit += (sender, evt) =>
            {
                Log.Info("Application is shutting down...");
                unity.Deinitialize();
                Unity.Logging.Internal.LoggerManager.FlushAll();
                Program.Shutdown();
            };
            RunLoop.EnterMainLoop(unity.OnTick);
        }
    }
}
#endif
