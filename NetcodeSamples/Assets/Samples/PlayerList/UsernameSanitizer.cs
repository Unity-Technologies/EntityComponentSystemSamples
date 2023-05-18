using System;
using Unity.Collections;
using Unity.Entities;
using Random = UnityEngine.Random;

namespace Unity.NetCode.Samples.PlayerList
{
    /// <inheritdoc cref="SanitizeUsername" />
    public static class UsernameSanitizer
    {
        /// <summary>
        ///     Usernames should be a min length and not contain any funky characters.
        ///     Basic sanitizing using <see cref="IsAlphanumeric" /> and <see cref="IsValidExtraChar" />.
        /// </summary>
        [GenerateTestsForBurstCompatibility]
        public static FixedString64Bytes SanitizeUsername(FixedString64Bytes input, int networkId, out bool didSanitize)
        {
            FixedString64Bytes sanitized = default;
            for (var i = 0; i < input.Length; i++)
            {
                var c = (char) input[i];
                if (IsAlphanumeric(c) || IsValidExtraChar(c, i, input.Length))
                    sanitized.Append(c);
            }

            didSanitize = sanitized != input;

            // Failed to sanitize and END UP with a usable name, so use this instead.
            if (sanitized.Length < 2)
                sanitized = $"Player{networkId}";
            return sanitized;
        }

        static bool IsAlphanumeric(char c)
        {
            return c >= 48 && c <= 57 || c >= 65 && c <= 90 || c >= 97 && c <= 122;
        }

        static bool IsValidExtraChar(char c, int index, int length)
        {
            var isNotFirst = index > 0;
            var isNotLast = index < length - 1;
            switch (c)
            {
                case '(': return isNotFirst;
                case ')': return isNotFirst;
                case '[': return isNotFirst;
                case ']': return isNotFirst;
                case ' ': return isNotFirst && isNotLast;
                case '-': return isNotFirst && isNotLast;
                case '_': return true;
                case '|': return true;
                case '.': return isNotFirst && isNotLast;
                default: return false;
            }
        }

        public static FixedString64Bytes GetDefaultUsername(World world)
        {
            var username = GetFirstPartOfEnvironmentUsername();
            var processId = Environment.CurrentManagedThreadId;

            if (username.IsEmpty)
            {
#if UNITY_EDITOR || NETCODE_DEBUG
                username = Environment.MachineName;
                if (username.IsEmpty)
#endif
                    username = $"Guest{processId}";
            }
            else
            {
                if (world.IsThinClient())
                    username += ".tc";
            }

#if UNITY_EDITOR || NETCODE_DEBUG
            username += $".{world.SequenceNumber}";
#endif

            return username;
        }

        /// <example>"john.doe" would become "jon".</example>>
        public static FixedString64Bytes GetFirstPartOfEnvironmentUsername()
        {
            var username = Environment.UserName;
            var separator = username.IndexOfAny(new[] {',', ' ', '.', '\n', '\t', '_', '-'}, 0);
            if (separator >= 3)
                username = username[..separator];
            return username;
        }
    }
}
