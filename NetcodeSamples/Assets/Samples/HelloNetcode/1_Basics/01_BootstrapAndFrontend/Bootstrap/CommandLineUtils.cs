using System;

namespace Samples.HelloNetcode
{
    public static class CommandLineUtils
    {
        public static string GetCommandLineValueFromKey(string key)
        {
            string[] args = System.Environment.GetCommandLineArgs ();
            for (int i = 0; i < args.Length; i++) {
                if (args[i].Equals($"-{key}", StringComparison.OrdinalIgnoreCase)) {
                    return args[i + 1];
                }
            }

            return string.Empty;
        }
    }
}
