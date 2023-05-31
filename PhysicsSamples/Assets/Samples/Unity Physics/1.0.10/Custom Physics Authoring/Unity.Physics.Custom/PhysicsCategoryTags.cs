using System;
using Unity.Mathematics;

namespace Unity.Physics.Authoring
{
    [Serializable]
    public struct PhysicsCategoryTags : IEquatable<PhysicsCategoryTags>
    {
        public static PhysicsCategoryTags Everything => new PhysicsCategoryTags { Value = unchecked((uint)~0) };
        public static PhysicsCategoryTags Nothing => new PhysicsCategoryTags { Value = 0 };

        public bool Category00;
        public bool Category01;
        public bool Category02;
        public bool Category03;
        public bool Category04;
        public bool Category05;
        public bool Category06;
        public bool Category07;
        public bool Category08;
        public bool Category09;
        public bool Category10;
        public bool Category11;
        public bool Category12;
        public bool Category13;
        public bool Category14;
        public bool Category15;
        public bool Category16;
        public bool Category17;
        public bool Category18;
        public bool Category19;
        public bool Category20;
        public bool Category21;
        public bool Category22;
        public bool Category23;
        public bool Category24;
        public bool Category25;
        public bool Category26;
        public bool Category27;
        public bool Category28;
        public bool Category29;
        public bool Category30;
        public bool Category31;

        internal bool this[int i]
        {
            get
            {
                SafetyChecks.CheckInRangeAndThrow(i, new int2(0, 31), nameof(i));
                switch (i)
                {
                    case  0: return Category00;
                    case  1: return Category01;
                    case  2: return Category02;
                    case  3: return Category03;
                    case  4: return Category04;
                    case  5: return Category05;
                    case  6: return Category06;
                    case  7: return Category07;
                    case  8: return Category08;
                    case  9: return Category09;
                    case 10: return Category10;
                    case 11: return Category11;
                    case 12: return Category12;
                    case 13: return Category13;
                    case 14: return Category14;
                    case 15: return Category15;
                    case 16: return Category16;
                    case 17: return Category17;
                    case 18: return Category18;
                    case 19: return Category19;
                    case 20: return Category20;
                    case 21: return Category21;
                    case 22: return Category22;
                    case 23: return Category23;
                    case 24: return Category24;
                    case 25: return Category25;
                    case 26: return Category26;
                    case 27: return Category27;
                    case 28: return Category28;
                    case 29: return Category29;
                    case 30: return Category30;
                    case 31: return Category31;
                    default: return default;
                }
            }
            set
            {
                SafetyChecks.CheckInRangeAndThrow(i, new int2(0, 31), nameof(i));
                switch (i)
                {
                    case  0: Category00 = value; break;
                    case  1: Category01 = value; break;
                    case  2: Category02 = value; break;
                    case  3: Category03 = value; break;
                    case  4: Category04 = value; break;
                    case  5: Category05 = value; break;
                    case  6: Category06 = value; break;
                    case  7: Category07 = value; break;
                    case  8: Category08 = value; break;
                    case  9: Category09 = value; break;
                    case 10: Category10 = value; break;
                    case 11: Category11 = value; break;
                    case 12: Category12 = value; break;
                    case 13: Category13 = value; break;
                    case 14: Category14 = value; break;
                    case 15: Category15 = value; break;
                    case 16: Category16 = value; break;
                    case 17: Category17 = value; break;
                    case 18: Category18 = value; break;
                    case 19: Category19 = value; break;
                    case 20: Category20 = value; break;
                    case 21: Category21 = value; break;
                    case 22: Category22 = value; break;
                    case 23: Category23 = value; break;
                    case 24: Category24 = value; break;
                    case 25: Category25 = value; break;
                    case 26: Category26 = value; break;
                    case 27: Category27 = value; break;
                    case 28: Category28 = value; break;
                    case 29: Category29 = value; break;
                    case 30: Category30 = value; break;
                    case 31: Category31 = value; break;
                }
            }
        }

        public uint Value
        {
            get
            {
                var result = 0;
                result |= (Category00 ? 1 : 0) << 0;
                result |= (Category01 ? 1 : 0) << 1;
                result |= (Category02 ? 1 : 0) << 2;
                result |= (Category03 ? 1 : 0) << 3;
                result |= (Category04 ? 1 : 0) << 4;
                result |= (Category05 ? 1 : 0) << 5;
                result |= (Category06 ? 1 : 0) << 6;
                result |= (Category07 ? 1 : 0) << 7;
                result |= (Category08 ? 1 : 0) << 8;
                result |= (Category09 ? 1 : 0) << 9;
                result |= (Category10 ? 1 : 0) << 10;
                result |= (Category11 ? 1 : 0) << 11;
                result |= (Category12 ? 1 : 0) << 12;
                result |= (Category13 ? 1 : 0) << 13;
                result |= (Category14 ? 1 : 0) << 14;
                result |= (Category15 ? 1 : 0) << 15;
                result |= (Category16 ? 1 : 0) << 16;
                result |= (Category17 ? 1 : 0) << 17;
                result |= (Category18 ? 1 : 0) << 18;
                result |= (Category19 ? 1 : 0) << 19;
                result |= (Category20 ? 1 : 0) << 20;
                result |= (Category21 ? 1 : 0) << 21;
                result |= (Category22 ? 1 : 0) << 22;
                result |= (Category23 ? 1 : 0) << 23;
                result |= (Category24 ? 1 : 0) << 24;
                result |= (Category25 ? 1 : 0) << 25;
                result |= (Category26 ? 1 : 0) << 26;
                result |= (Category27 ? 1 : 0) << 27;
                result |= (Category28 ? 1 : 0) << 28;
                result |= (Category29 ? 1 : 0) << 29;
                result |= (Category30 ? 1 : 0) << 30;
                result |= (Category31 ? 1 : 0) << 31;
                return unchecked((uint)result);
            }
            set
            {
                Category00 = (value & (1 << 0)) != 0;
                Category01 = (value & (1 << 1)) != 0;
                Category02 = (value & (1 << 2)) != 0;
                Category03 = (value & (1 << 3)) != 0;
                Category04 = (value & (1 << 4)) != 0;
                Category05 = (value & (1 << 5)) != 0;
                Category06 = (value & (1 << 6)) != 0;
                Category07 = (value & (1 << 7)) != 0;
                Category08 = (value & (1 << 8)) != 0;
                Category09 = (value & (1 << 9)) != 0;
                Category10 = (value & (1 << 10)) != 0;
                Category11 = (value & (1 << 11)) != 0;
                Category12 = (value & (1 << 12)) != 0;
                Category13 = (value & (1 << 13)) != 0;
                Category14 = (value & (1 << 14)) != 0;
                Category15 = (value & (1 << 15)) != 0;
                Category16 = (value & (1 << 16)) != 0;
                Category17 = (value & (1 << 17)) != 0;
                Category18 = (value & (1 << 18)) != 0;
                Category19 = (value & (1 << 19)) != 0;
                Category20 = (value & (1 << 20)) != 0;
                Category21 = (value & (1 << 21)) != 0;
                Category22 = (value & (1 << 22)) != 0;
                Category23 = (value & (1 << 23)) != 0;
                Category24 = (value & (1 << 24)) != 0;
                Category25 = (value & (1 << 25)) != 0;
                Category26 = (value & (1 << 26)) != 0;
                Category27 = (value & (1 << 27)) != 0;
                Category28 = (value & (1 << 28)) != 0;
                Category29 = (value & (1 << 29)) != 0;
                Category30 = (value & (1 << 30)) != 0;
                Category31 = (value & (1 << 31)) != 0;
            }
        }

        public bool Equals(PhysicsCategoryTags other) => Value == other.Value;

        public override bool Equals(object obj) => obj is PhysicsCategoryTags other && Equals(other);

        public override int GetHashCode() => unchecked((int)Value);
    }
}
