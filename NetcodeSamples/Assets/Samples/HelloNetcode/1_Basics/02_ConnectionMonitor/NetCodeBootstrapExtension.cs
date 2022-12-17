// #define ENABLE_NETCODE_SAMPLE_TIMEOUT

namespace Samples.HelloNetcode
{
#if ENABLE_NETCODE_SAMPLE_TIMEOUT
    [UnityEngine.Scripting.Preserve]
    public class NetCodeBootstrapExtension : NetCodeBootstrap
    {
        public override bool Initialize(string defaultWorldName)
        {
            // To set up a custom driver the constructor for it needs to be hooked up
            // before world creation in the boostrap system. The netcode bootstrap is
            // already defined in the previous main connection sample and as there can
            // be only one set up in the project, we just add this line here on top of
            // the existing NetCodeBootstrap class (normally a project would only have
            // the boostrap defined in one place).
            NetworkStreamReceiveSystem.DriverConstructor = new DriverConstructor();
            return base.Initialize(defaultWorldName);
        }
    }
#endif
}
