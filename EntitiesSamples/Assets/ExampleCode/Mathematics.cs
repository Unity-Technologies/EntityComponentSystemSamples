using Unity.Collections;
using Unity.Mathematics;

namespace ExampleCode.Math
{
    public class Mathematics
    {
        public static void vectorCreation()
        {
            int4 i4 = new int4(1, 2, 3, 4); // x, y, z, w
            int2 i2 = int2.zero; // new int2(0, 0);

            // Index the components like an array.
            int i = i4[2]; // int i = i4.z
            i4[0] = 9; // i4.x = 9

            // Creating a vector by copying combinations
            // of values from another vector (swizzling).
            i4 = i4.xwyy; // new int4(i4.x, i4.w, i4.y, i4.y);
            i2 = i4.yz; // new int2(i4.y, i4.z);
            i4 = i2.yxyx; // new int4(i2.y, i2.x, i2.y, i2.x);

            // Creating a vector from combinations of
            // lower-dimension vectors and scalars.
            i4 = new int4(1, i2, 3); // new int4(1, i2.x, i2.y, 3);
            i4 = new int4(i2, i2); // new int4(i2.x, i2.y, i2.x, i2.y);
            i4 = new int4(7); // new int4(7, 7, 7, 7);
            i2 = new int2(7.5f); // new int2((int) 7.5f, (int) 7.5f);

            // Creating a vector by casting.
            i4 = (int4)7; // new int4(7, 7, 7, 7);
            i2 = (int2)7.5f; // new int2((int) 7.5f, (int) 7.5f);
        }

        public static void matrixCreation()
        {
            // Values in row-major order.
            int2x3 m = new int2x3(1, 2, 3, 4, 5, 6); // first row: 1, 2, 3
            // second row: 4, 5, 6

            // First column: new int2(1, 4)
            int2 i2 = m.c0;

            // Third column: new int2(3, 6)
            i2 = m.c2;

            // new int2x3(100, 100, 100, 100, 100, 100)
            m = new int2x3(100);

            m = new int2x3(
                new int2(1, 2), // column 0
                new int2(3, 4), // column 1
                new int2(5, 6)); // column 2

            // Converts each int component to a float.
            float2x3 m2 = new float2x3(m);
        }

        public static void vectorMatrixOperators()
        {
            int2 a = new int2(1, 2);
            int2 b = new int2(3, 4);

            // Addition.
            int2 c = a + b; // new int2(a.x + b.x, a.y + b.y)

            // Negation.
            c = -a; // new int2(-a.x, -a.y)

            // Equality.
            bool myBool = a.Equals(b); // a.x == b.x && a.y == b.y
            bool2 myBool2 = a == b; // new int2(a.x == b.x, a.y == b.y)

            // Greater than.
            myBool2 = a > b; // new bool2(a.x > b.x, a.y > b.y)
        }

        public static void random()
        {
            Random rand = new Random(123); // seed of 123

            // [-2147483647, 2147483647]
            int integer = rand.NextInt();

            // [25, 100)
            integer = rand.NextInt(25, 100);

            // x is [0, 1), y is [0, 1)
            float2 f2 = rand.NextFloat2();

            // x is [0, 7.5), y is [0, 11)
            f2 = rand.NextFloat2(new float2(7.5f, 11f));

            // x is [2, 7.5), y is [-4.6, 11)
            f2 = rand.NextFloat2(new float2(2f, -4.6f), new float2(7.5f, 11f));

            // Uniformly random unit-length direction vector.
            double3 d3 = rand.NextDouble3Direction();

            // Uniformly random unit-length quaternion.
            quaternion q = rand.NextQuaternionRotation();

            // Create multiple Random number generators using an incremented seed.
            NativeArray<Random> rngs = new NativeArray<Random>(10, Allocator.Temp);
            for (int i = 0; i < 10; i++)
            {
                // Unlike the Random constructor, CreateFromIndex hashes the seed.
                // If we were to pass incremented seeds to the constructor,
                // each RNG would produce a similar stream of random numbers
                // as each other. Because we instead here use CreateFromIndex,
                // the RNG's will each produce streams of random numbers that
                // are properly distinct and unrelated to the others.
                rngs[i] = Random.CreateFromIndex((uint)(i + 123));
            }
        }
    }
}
