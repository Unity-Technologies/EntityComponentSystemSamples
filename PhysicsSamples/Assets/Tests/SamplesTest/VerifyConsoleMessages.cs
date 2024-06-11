using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;

namespace Unity.Physics.Tests
{
    static class VerifyConsoleMessages
    {
        [Serializable]
        class Entries
        {
            public Entry[] Array;
        }

        [Serializable]
        class Entry
        {
            public string Scene;
            public string Logtype;
            public string Message;
        }

        /// <summary>
        /// This is taken directly from unity/unity Runtime\Logging\LogAssert.h. There is no C# equivalent in the editor
        /// so when the native enum changes, this should be updated as well.
        /// </summary>
        [Flags]
        enum LogMessageFlags : int
        {
            kNoLogMessageFlags = 0,
            kError = 1 << 0, // Message describes an error.
            kAssert = 1 << 1, // Message describes an assertion failure.
            kLog = 1 << 2, // Message is a general log message.
            kFatal = 1 << 4, // Message describes a fatal error, and that the program should now exit.
            kAssetImportError = 1 << 6, // Message describes an error generated during asset importing.
            kAssetImportWarning = 1 << 7, // Message describes a warning generated during asset importing.
            kScriptingError = 1 << 8, // Message describes an error produced by script code.
            kScriptingWarning = 1 << 9, // Message describes a warning produced by script code.
            kScriptingLog = 1 << 10, // Message describes a general log message produced by script code.
            kScriptCompileError = 1 << 11, // Message describes an error produced by the script compiler.
            kScriptCompileWarning = 1 << 12, // Message describes a warning produced by the script compiler.

            kStickyLog =
                1 << 13, // Message is 'sticky' and should not be removed when the user manually clears the console window.

            kMayIgnoreLineNumber =
                1 << 14, // The scripting runtime should skip annotating the log callstack with file and line information.

            kReportBug =
                1 << 15, // When used with kFatal, indicates that the log system should launch the bug reporter.

            kDisplayPreviousErrorInStatusBar =
                1 << 16, // The message before this one should be displayed at the bottom of Unity's main window, unless there are no messages before this one.
            kScriptingException = 1 << 17, // Message describes an exception produced by script code.
            kDontExtractStacktrace = 1 << 18, // Stacktrace extraction should be skipped for this message.
            kScriptingAssertion = 1 << 21, // The message describes an assertion failure in script code.

            kStacktraceIsPostprocessed =
                1 << 22, // The stacktrace has already been postprocessed and does not need further processing.
            kIsCalledFromManaged = 1 << 23, // The message is being called from managed code.

            FromEditor = kDontExtractStacktrace | kMayIgnoreLineNumber | kIsCalledFromManaged,

            DebugLog = kScriptingLog | FromEditor,
            DebugWarning = kScriptingWarning | FromEditor,
            DebugError = kScriptingError | FromEditor,
            DebugException = kScriptingException | FromEditor,
            DebugAssert = kScriptingAssertion | FromEditor
        }

        class LogMessageFlagsExtensions
        {
            public static bool IsInfo(int flags)
            {
                return (flags & (int)(LogMessageFlags.kLog | LogMessageFlags.kScriptingLog)) != 0;
            }

            public static bool IsWarning(int flags)
            {
                return (flags & (int)(LogMessageFlags.kScriptCompileWarning | LogMessageFlags.kScriptingWarning |
                    LogMessageFlags.kAssetImportWarning)) != 0;
            }

            public static bool IsError(int flags)
            {
                return (flags & (int)(LogMessageFlags.kFatal | LogMessageFlags.kAssert | LogMessageFlags.kError |
                    LogMessageFlags.kScriptCompileError |
                    LogMessageFlags.kScriptingError | LogMessageFlags.kAssetImportError |
                    LogMessageFlags.kScriptingAssertion | LogMessageFlags.kScriptingException)) != 0;
            }
        }

        /// <summary>
        /// This is to avoid a potential instability.
        /// Ex.: Worker0 prints a message in most cases and sometimes worker1 prints the same message.
        /// We avoid this by removing [Workerx] from the message
        /// </summary>
        static readonly Regex WorkerMessage = new Regex("\\[Worker[0-9]\\] ", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        static Entries ParseAllWhitelistedLogMessage()
        {
            var whiteListPath = Path.Combine("Assets", "Tests", "SamplesTest", "sceneLogWhitelist.json");
            return JsonUtility.FromJson<Entries>(File.ReadAllText(whiteListPath));
        }

        static IEnumerable<Entry> GetWhiteListedMessagesForScene(string scenePath)
        {
            return ParseAllWhitelistedLogMessage().Array.Where(x => x.Scene == scenePath);
        }

        [Conditional("UNITY_EDITOR")]
        public static void ClearMessagesInConsole()
        {
#if UNITY_EDITOR
            Assembly assembly = Assembly.GetAssembly(typeof(UnityEditor.Scripting.ManagedDebugger));
            Type logEntries = assembly.CreateInstance("UnityEditor.LogEntries").GetType();
            logEntries.GetMethod("Clear").Invoke(null, null);
#endif
        }

        /// <summary>
        /// Iterate through console entries and verify that warnings and errors have been whitelisted.
        /// If a message is not whitelisted the test fails immediately.
        ///
        /// We do not have access through public APIs to get the specific Console log entries.
        /// We use reflection (I'm sorry) to access the proper APIs. This may be prone to break.
        /// </summary>
        /// <param name="scenePath">Which sample scene the messages originate from.</param>
        /// <exception cref="NotImplementedException">If the message originates from an unknown mode.</exception>
        [Conditional("UNITY_EDITOR")]
        public static void VerifyPrintedMessages(string scenePath)
        {
#if UNITY_EDITOR
            Assembly assembly = Assembly.GetAssembly(typeof(UnityEditor.Scripting.ManagedDebugger));
            Type logEntries = assembly.CreateInstance("UnityEditor.LogEntries").GetType();
            MethodInfo getCount = logEntries.GetMethod("GetCount");

            int logCount = (int)getCount.Invoke(null, null);
            object entry = assembly.CreateInstance("UnityEditor.LogEntry");
            Type entryType = entry.GetType();

            try
            {
                IEnumerable<Entry> expected = GetWhiteListedMessagesForScene(scenePath);
                logEntries.GetMethod("StartGettingEntries").Invoke(null, null);
                for (int i = 0; i < logCount; i++)
                {
                    logEntries.GetMethod("GetEntryInternal").Invoke(null, new[] { i, entry });
                    var message = WorkerMessage.Replace(entryType.GetField("message").GetValue(entry).ToString(), string.Empty);
                    var mode = (int)entryType.GetField("mode").GetValue(entry);

                    if (LogMessageFlagsExtensions.IsInfo(mode))
                    {
                        // skip info messages
                    }
                    else if (LogMessageFlagsExtensions.IsWarning(mode))
                    {
                        var warningMessage = message.Split("UnityEngine.Debug:LogWarning (object)")[0];
                        if (expected.Where(x => x.Logtype.ToLowerInvariant() == "warning")
                            .All(x => !Regex.IsMatch(warningMessage, Regex.Escape(x.Message).Replace("__any__", ".*"))))
                        {
                            Assert.Fail($"{LogType.Warning}: was unexpected with message: {warningMessage}");
                        }
                    }
                    else if (LogMessageFlagsExtensions.IsError(mode))
                    {
                        var errorMessage = message.Split("UnityEngine.Debug:LogError (object)")[0];
                        if (expected.Where(x => x.Logtype.ToLowerInvariant() == "error")
                            .All(x => !Regex.IsMatch(errorMessage, Regex.Escape(x.Message).Replace("__any__", ".*"))))
                        {
                            Assert.Fail($"{LogType.Error}: was unexpected with message: {errorMessage}");
                        }
                    }
                    else
                    {
                        throw new InvalidOperationException($"{Enum.Parse<LogMessageFlags>(mode.ToString())}: mode was not expected for message: {message}");
                    }
                }
            }
            finally
            {
                logEntries.GetMethod("EndGettingEntries").Invoke(null, null);
                logEntries.GetMethod("Clear").Invoke(null, null);
            }
#endif
        }
    }
}
