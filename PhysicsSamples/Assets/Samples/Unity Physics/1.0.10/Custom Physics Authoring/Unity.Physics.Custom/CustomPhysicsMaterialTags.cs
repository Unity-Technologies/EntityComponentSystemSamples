using System;
using Unity.Mathematics;

namespace Unity.Physics.Authoring
{
    [Serializable]
    public struct CustomPhysicsMaterialTags : IEquatable<CustomPhysicsMaterialTags>
    {
        public static CustomPhysicsMaterialTags Everything => new CustomPhysicsMaterialTags { Value = unchecked((byte)~0) };
        public static CustomPhysicsMaterialTags Nothing => new CustomPhysicsMaterialTags { Value = 0 };

        public bool Tag00;
        public bool Tag01;
        public bool Tag02;
        public bool Tag03;
        public bool Tag04;
        public bool Tag05;
        public bool Tag06;
        public bool Tag07;

        internal bool this[int i]
        {
            get
            {
                SafetyChecks.CheckInRangeAndThrow(i, new int2(0, 7), nameof(i));
                switch (i)
                {
                    case 0: return Tag00;
                    case 1: return Tag01;
                    case 2: return Tag02;
                    case 3: return Tag03;
                    case 4: return Tag04;
                    case 5: return Tag05;
                    case 6: return Tag06;
                    case 7: return Tag07;
                    default: return default;
                }
            }
            set
            {
                SafetyChecks.CheckInRangeAndThrow(i, new int2(0, 7), nameof(i));
                switch (i)
                {
                    case 0: Tag00 = value; break;
                    case 1: Tag01 = value; break;
                    case 2: Tag02 = value; break;
                    case 3: Tag03 = value; break;
                    case 4: Tag04 = value; break;
                    case 5: Tag05 = value; break;
                    case 6: Tag06 = value; break;
                    case 7: Tag07 = value; break;
                }
            }
        }

        public byte Value
        {
            get
            {
                var result = 0;
                result |= (Tag00 ? 1 : 0) << 0;
                result |= (Tag01 ? 1 : 0) << 1;
                result |= (Tag02 ? 1 : 0) << 2;
                result |= (Tag03 ? 1 : 0) << 3;
                result |= (Tag04 ? 1 : 0) << 4;
                result |= (Tag05 ? 1 : 0) << 5;
                result |= (Tag06 ? 1 : 0) << 6;
                result |= (Tag07 ? 1 : 0) << 7;
                return (byte)result;
            }
            set
            {
                Tag00 = (value & (1 << 0)) != 0;
                Tag01 = (value & (1 << 1)) != 0;
                Tag02 = (value & (1 << 2)) != 0;
                Tag03 = (value & (1 << 3)) != 0;
                Tag04 = (value & (1 << 4)) != 0;
                Tag05 = (value & (1 << 5)) != 0;
                Tag06 = (value & (1 << 6)) != 0;
                Tag07 = (value & (1 << 7)) != 0;
            }
        }

        public bool Equals(CustomPhysicsMaterialTags other) => Value == other.Value;

        public override bool Equals(object obj) => obj is CustomPhysicsMaterialTags other && Equals(other);

        public override int GetHashCode() => Value;
    }
}
