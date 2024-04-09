using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;

namespace Graphical.Splines
{
    public static class BlobArrayExtensions
    {
        public static int LowerBound<T>(ref this BlobArray<T> array, T value)
            where T : struct, IComparable<T>
        {
            return LowerBound(ref array, value, new NativeSortExtension.DefaultComparer<T>());
        }

        public static int LowerBound<T, U>(ref this BlobArray<T> array, T value, U comparer)
            where T : struct
            where U : IComparer<T>
        {
            int l = 0;
            int r = array.Length;

            while (r > l)
            {
                var m = (l + r) / 2;
                var c = comparer.Compare(array[m], value);
                if (c == 0)
                {
                    return l;
                }

                if (c < 0)
                {
                    l = m + 1;
                }
                else
                {
                    r = m;
                }
            }

            return l - 1;
        }
    }
}
