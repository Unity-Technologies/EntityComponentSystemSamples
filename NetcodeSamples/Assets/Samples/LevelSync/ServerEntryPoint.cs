#if UNITY_DOTSRUNTIME
using Unity.Platforms;
using Unity.Runtime;
using Unity.Runtime.EntryPoint;

namespace DOTSRuntime.Server
{
    public class ServerMain
    {
        public static void Main()
        {
            Program.Initialize();
            var unity = UnityInstance.Initialize();
            unity.OnTick = (double timestampInSeconds) =>
            {
                var shouldContinue = unity.Update(timestampInSeconds);
                return shouldContinue;
            };
            PlatformEvents.OnQuit += (sender, evt) =>
            {
                unity.Deinitialize();
                Program.Shutdown();
            };
            RunLoop.EnterMainLoop(unity.OnTick);
        }
    }
}
#endif
