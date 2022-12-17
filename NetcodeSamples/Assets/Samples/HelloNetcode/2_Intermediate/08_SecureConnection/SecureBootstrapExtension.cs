// #define ENABLE_NETCODE_SAMPLE_SECURE

namespace Samples.HelloNetcode
{
#if ENABLE_NETCODE_SAMPLE_SECURE
    [UnityEngine.Scripting.Preserve]
    public class SecureBootStrapExtension : NetCodeBootstrap
    {
        public override bool Initialize(string defaultWorldName)
        {
            // To set up a custom driver the constructor for it needs to be hooked up
            // before world creation in the bootstrap system. The netcode bootstrap is
            // already defined in the main connection sample and as there can
            // be only one set up in the project, we just add this line here on top of
            // the existing NetCodeBootstrap class (normally a project would only have
            // the boostrap defined in one place).
            NetworkStreamReceiveSystem.DriverConstructor = new SecureDriverConstructor();
            return base.Initialize(defaultWorldName);
        }
    }
#endif
}
