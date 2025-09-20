using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using UnityEngine;
using Vecerdi.Extensions.Logging;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace TrafficSimulation.Core.Random {
    public static class Rand {
        private const float TwoPI = 6.2831853071795864f;

        private static IRandomProvider s_RandomProvider = new DefaultRandomProvider();
        private static readonly Stack<ulong> s_StateStack = new();
        private static uint s_Iterations;

        [field: MaybeNull]
        private static ILogger Logger => field ??= UnityLoggerFactory.CreateLogger(typeof(Rand).FullName ?? nameof(Rand));

        static Rand() {
            s_RandomProvider.Seed = (uint)DateTime.Now.GetHashCode();
        }

        private static ulong StateCompressed {
            get => s_RandomProvider.Seed | ((ulong)s_Iterations << 32);
            set {
                s_RandomProvider.Seed = (uint)(value & uint.MaxValue);
                s_Iterations = (uint)((value >> 32) & uint.MaxValue);
            }
        }

        public static IRandomProvider RandomProvider {
            get => s_RandomProvider;
            set {
                var currentSeed = s_RandomProvider.Seed;
                s_RandomProvider = value;
                s_RandomProvider.Seed = currentSeed;
                s_Iterations = 0;
            }
        }

        public static void PushState() {
            s_StateStack.Push(StateCompressed);
        }

        /// <summary>
        /// Replaces the current seed with <paramref name="replacementSeed"/>. Use <see cref="PopState"/> to undo this operation and retrieve the original state
        /// </summary>
        public static void PushState(int replacementSeed) {
            PushState();

            s_RandomProvider.Seed = (uint)replacementSeed;
            s_Iterations = 0U;
        }

        public static void PopState() {
            if (s_StateStack.Count == 0) {
                Logger.LogError("State stack is empty when trying to call PopState");
                return;
            }

            StateCompressed = s_StateStack.Pop();
        }

        /// <summary>
        /// Replaces the current seed with <paramref name="replacementSeed"/>.<br/>
        /// Invoking <see cref="IDisposable.Dispose"/> on the returned object will undo this operation and
        /// revert to the previous state.
        /// </summary>
        /// <returns>An instance of <see cref="IDisposable"/> that will revert the state when disposed</returns>
        public static IDisposable StateScope(int replacementSeed) {
            PushState(replacementSeed);
            return new PopStateScope();
        }

        /// <summary>
        /// Returns a random float from 0 to 1 (both inclusive)
        /// </summary>
        public static float Float => s_RandomProvider.GetFloat(s_Iterations++);

        /// <summary>
        /// Returns a random integer
        /// </summary>
        public static int Int => s_RandomProvider.GetInt(s_Iterations++);

        /// <summary>
        /// Returns a random long
        /// </summary>
        public static long Long => BitConverter.ToInt64(Bytes(8), 0);

        /// <summary>
        /// Returns a random bool
        /// </summary>
        public static bool Bool => Float < 0.5;

        /// <summary>
        /// Returns a random sign. (Either 1 or -1)
        /// </summary>
        public static int Sign => Bool ? 1 : -1;

        /// <summary>
        /// Returns true if a random chance occurs, false otherwise
        /// </summary>
        /// <param name="chance">The chance (between 0 and 1)</param>
        /// <returns>true if a random chance occurs, false otherwise</returns>
        public static bool Chance(float chance) {
            if (chance <= 0.0)
                return false;
            if (chance >= 1.0)
                return true;
            return Float < (double)chance;
        }

        /// <summary>
        /// Returns an array of <paramref name="count"/> random bytes
        /// </summary>
        /// <param name="count">The number of bytes to generate</param>
        /// <returns>An array of <paramref name="count"/> random bytes</returns>
        public static byte[] Bytes(int count) {
            var buffer = new byte[count];
            for (var i = 0; i < buffer.Length; i++) {
                buffer[i] = (byte)(Int % 256);
            }

            return buffer;
        }

        public static void Shuffle<T>(IList<T> list) {
            var n = list.Count;
            while (n > 1) {
                n--;
                var k = Range(n + 1);
                (list[k], list[n]) = (list[n], list[k]);
            }
        }

        /// <summary>
        /// Returns a random element from the given <see cref="IList{T}"/>
        /// </summary>
        public static T ListItem<T>(IList<T> list) {
            return list[Range(0, list.Count)];
        }

        /// <summary>
        /// Returns either <paramref name="a"/> or <paramref name="b"/> randomly.
        /// </summary>
        public static T Element<T>(T a, T b) {
            if (Bool)
                return a;
            return b;
        }

        /// <summary>
        /// Returns either <paramref name="a"/>, <paramref name="b"/>, or <paramref name="c"/> randomly.
        /// </summary>
        public static T Element<T>(T a, T b, T c) {
            var num = Float;
            if (num < 0.333330005407333)
                return a;
            if (num < 0.666660010814667)
                return b;
            return c;
        }

        /// <summary>
        /// Returns either <paramref name="a"/>, <paramref name="b"/>, <paramref name="c"/>, or <paramref name="d"/> randomly.
        /// </summary>
        public static T Element<T>(T a, T b, T c, T d) {
            var num = Float;
            if (num < 0.25)
                return a;
            if (num < 0.5)
                return b;
            if (num < 0.75)
                return c;
            return d;
        }

        /// <summary>
        /// Returns either <paramref name="a"/>, <paramref name="b"/>, <paramref name="c"/>, <paramref name="d"/>, or <paramref name="e"/> randomly.
        /// </summary>
        public static T Element<T>(T a, T b, T c, T d, T e) {
            var num = Float;
            if (num < 0.2f)
                return a;
            if (num < 0.4f)
                return b;
            if (num < 0.6f)
                return c;
            if (num < 0.8f)
                return d;
            return e;
        }

        /// <summary>
        /// Returns either <paramref name="a"/>, <paramref name="b"/>, <paramref name="c"/>, <paramref name="d"/>, <paramref name="e"/>, or <paramref name="f"/> randomly.
        /// </summary>
        public static T Element<T>(T a, T b, T c, T d, T e, T f) {
            var num = Float;
            if (num < 0.166659995913506)
                return a;
            if (num < 0.333330005407333)
                return b;
            if (num < 0.5)
                return c;
            if (num < 0.666660010814667)
                return d;
            if (num < 0.833329975605011)
                return e;
            return f;
        }

        /// <summary>
        /// Returns a random element from <paramref name="items"/>
        /// </summary>
        public static T Element<T>(params T[] items) {
            return items[Range(0, items.Length)];
        }

        /// <summary>
        /// Returns a random integer from <paramref name="min"/> (inclusive) to <paramref name="max"/> (exclusive)
        /// </summary>
        /// <param name="min">Minimum range (inclusive)</param>
        /// <param name="max">Maximum range (exclusive)</param>
        /// <returns>A random integer from <paramref name="min"/> (inclusive) to <paramref name="max"/> (exclusive)</returns>
        public static int Range(int min, int max) {
            if (max <= min)
                return min;
            return min + Mathf.Abs(Int % (max - min));
        }

        /// <summary>
        /// Returns a random integer from 0 (inclusive) to <paramref name="max"/> (exclusive)
        /// </summary>
        /// <param name="max">Maximum range (exclusive)</param>
        /// <returns>A random integer from 0 (inclusive) to <paramref name="max"/> (exclusive)</returns>
        public static int Range(int max) {
            return Range(0, max);
        }

        /// <summary>
        /// Returns a random float from <paramref name="min"/> (inclusive) to <paramref name="max"/> (inclusive)
        /// </summary>
        /// <param name="min">Minimum range (inclusive)</param>
        /// <param name="max">Maximum range (inclusive)</param>
        /// <returns>A random float from <paramref name="min"/> (inclusive) to <paramref name="max"/> (inclusive)</returns>
        public static float Range(float min, float max) {
            if (max <= (double)min)
                return min;
            return Float * (max - min) + min;
        }

        /// <summary>
        /// Returns a random float from 0 (inclusive) to <paramref name="max"/> (inclusive)
        /// </summary>
        /// <param name="max">Maximum range (inclusive)</param>
        /// <returns>A random float from 0 (inclusive) to <paramref name="max"/> (inclusive)</returns>
        public static float Range(float max) {
            return Range(0f, max);
        }

        /// <summary>
        /// Returns a random float from <paramref name="range"/>.x to <paramref name="range"/>.y
        /// </summary>
        public static float Range(Vector2 range) {
            return Range(range.x, range.y);
        }

        /// <summary>
        /// Returns a random integer from <paramref name="range"/>.x (inclusive) to <paramref name="range"/>.y (exclusive)
        /// </summary>
        public static int Range(Vector2Int range) {
            return Range(range.x, range.y);
        }

        /// <summary>
        /// Returns a random integer from <paramref name="min"/> (inclusive) to <paramref name="max"/> (inclusive)
        /// </summary>
        /// <param name="min">Minimum range (inclusive)</param>
        /// <param name="max">Maximum range (inclusive)</param>
        /// <returns>A random integer from <paramref name="min"/> (inclusive) to <paramref name="max"/> (inclusive)</returns>
        public static int RangeInclusive(int min, int max) {
            if (max <= min)
                return min;
            return Range(min, max + 1);
        }

        /// <summary>
        /// Returns a random integer from <paramref name="range"/>.x (inclusive) to <paramref name="range"/>.y (inclusive)
        /// </summary>
        public static int RangeInclusive(Vector2Int range) {
            return RangeInclusive(range.x, range.y);
        }

        public static float Gaussian(float centerX = 0.0f, float widthFactor = 1f) {
            return Mathf.Sqrt(-2f * Mathf.Log(Float)) * Mathf.Sin(TwoPI * Float) * widthFactor + centerX;
        }

        public static float GaussianAsymmetric(float centerX = 0.0f, float lowerWidthFactor = 1f, float upperWidthFactor = 1f) {
            var num = Mathf.Sqrt(-2f * Mathf.Log(Float)) * Mathf.Sin(TwoPI * Float);
            if (num <= 0.0)
                return num * lowerWidthFactor + centerX;
            return num * upperWidthFactor + centerX;
        }

        /// <summary>
        /// Returns a random unit vector
        /// </summary>
        public static Vector3 UnitVector3 => new Vector3(Gaussian(), Gaussian(), Gaussian()).normalized;

        /// <summary>
        /// Returns a random unit vector
        /// </summary>
        public static Vector2 UnitVector2 => new Vector2(Gaussian(), Gaussian()).normalized;

        /// <summary>
        /// Returns a random point inside the unit circle
        /// </summary>
        public static Vector2 InsideUnitCircle {
            get {
                Vector2 vector;
                do {
                    vector = new Vector2(Float - 0.5f, Float - 0.5f) * 2f;
                } while (vector.sqrMagnitude > 1.0f);

                return vector;
            }
        }

        /// <summary>
        /// Returns a random point inside the unit circle
        /// </summary>
        public static Vector3 InsideUnitCircleVec3 {
            get {
                var insideUnitCircle = InsideUnitCircle;
                return new Vector3(insideUnitCircle.x, 0.0f, insideUnitCircle.y);
            }
        }

        /// <summary>
        /// Returns a random point inside the unit sphere
        /// </summary>
        public static Vector3 InsideUnitSphere {
            get {
                Vector3 vector;
                do {
                    vector = new Vector3(Float - 0.5f, Float - 0.5f, Float - 0.5f) * 2f;
                } while (vector.sqrMagnitude > 1.0f);

                return vector;
            }
        }

        /// <summary>
        /// Returns a random rotation
        /// </summary>
        public static Quaternion Rotation => Quaternion.Euler(360.0f * new Vector3(Float, Float, Float));

        /// <summary>
        /// Returns a random rotation with uniform distribution
        /// </summary>
        public static Quaternion RotationUniform => Quaternion.Euler(360.0f * new Vector3(Gaussian(), Gaussian(), Gaussian()));

        /// <summary>
        /// Generates a random color using an HSV range and alpha range.
        /// </summary>
        /// <param name="hueMin">Minimum hue [range 0..1]</param>
        /// <param name="hueMax">Maximum hue [range 0..1]</param>
        /// <param name="saturationMin">Minimum saturation [range 0..1]</param>
        /// <param name="saturationMax">Maximum saturation [range 0..1]</param>
        /// <param name="valueMin">Minimum value [range 0..1]</param>
        /// <param name="valueMax">Maximum value [range 0..1]</param>
        /// <param name="alphaMin">Minimum alpha [range 0..1]</param>
        /// <param name="alphaMax">Maximum alpha [range 0..1]</param>
        /// <returns>A random color with HSV and alpha values in input range</returns>
        /// <footer>This is the same implementation as <see cref="UnityEngine.Random.ColorHSV()">UnityEngine.Random.ColorHSV()</see></footer>
        public static Color ColorHSV(
            float hueMin = 0.0f, float hueMax = 1.0f, float saturationMin = 0.0f, float saturationMax = 1.0f,
            float valueMin = 0.0f, float valueMax = 1.0f, float alphaMin = 1.0f, float alphaMax = 1.0f
        ) {
            var color = Color.HSVToRGB(
                Range(Mathf.Clamp01(hueMin), Mathf.Clamp01(hueMax)),
                Range(Mathf.Clamp01(saturationMin), Mathf.Clamp01(saturationMax)),
                Range(Mathf.Clamp01(valueMin), Mathf.Clamp01(valueMax))
            );
            color.a = Range(Mathf.Clamp01(alphaMin), Mathf.Clamp01(alphaMax));
            return color;
        }

        /// <summary>
        /// Returns a random integer in from <paramref name="min"/> (inclusive) to <paramref name="max"/> (exclusive) using <paramref name="seed"/> as a seed
        /// </summary>
        /// <param name="min">Minimum range (inclusive)</param>
        /// <param name="max">Maximum range (exclusive)</param>
        /// <param name="seed">Custom seed</param>
        /// <returns>A random integer in from <paramref name="min"/> (inclusive) to <paramref name="max"/> (exclusive) using <paramref name="seed"/> as a seed</returns>
        public static int RangeSeeded(int min, int max, int seed) {
            PushState(seed);
            var num = Range(min, max);
            PopState();
            return num;
        }

        /// <summary>
        /// Returns a random float in from <paramref name="min"/> (inclusive) to <paramref name="max"/> (inclusive) using <paramref name="seed"/> as a seed
        /// </summary>
        /// <param name="min">Minimum range (inclusive)</param>
        /// <param name="max">Maximum range (inclusive)</param>
        /// <param name="seed">Custom seed</param>
        /// <returns>A random float in from <paramref name="min"/> (inclusive) to <paramref name="max"/> (inclusive) using <paramref name="seed"/> as a seed</returns>
        public static float RangeSeeded(float min, float max, int seed) {
            PushState(seed);
            var num = Range(min, max);
            PopState();
            return num;
        }

        /// <summary>
        /// Returns a random integer in from <paramref name="min"/> (inclusive) to <paramref name="max"/> (inclusive) using <paramref name="seed"/> as a seed
        /// </summary>
        /// <param name="min">Minimum range (inclusive)</param>
        /// <param name="max">Maximum range (inclusive)</param>
        /// <param name="seed">Custom seed</param>
        /// <returns>A random integer in from <paramref name="min"/> (inclusive) to <paramref name="max"/> (inclusive) using <paramref name="seed"/> as a seed</returns>
        public static int RangeInclusiveSeeded(int min, int max, int seed) {
            PushState(seed);
            var num = RangeInclusive(min, max);
            PopState();
            return num;
        }

        /// <summary>
        /// Returns a random float from 0 to 1 (both inclusive) using <paramref name="seed"/> as a seed
        /// </summary>
        /// <param name="seed">Custom seed</param>
        /// <returns>A random float from 0 to 1 (both inclusive) using <paramref name="seed"/> as a seed</returns>
        public static float FloatSeeded(int seed) {
            PushState(seed);
            var num = Float;
            PopState();
            return num;
        }

        /// <summary>
        /// Returns true if a random chance occurs using <paramref name="seed"/> as a seed, false otherwise
        /// </summary>
        /// <param name="chance">The chance (between 0 and 1)</param>
        /// <param name="seed">Custom seed</param>
        /// <returns>true if a random chance occurs using <paramref name="seed"/> as a seed, false otherwise</returns>
        public static bool ChanceSeeded(float chance, int seed) {
            PushState(seed);
            var flag = Chance(chance);
            PopState();
            return flag;
        }

        /// <summary>
        /// Returns an array of <paramref name="count"/> random bytes using <paramref name="seed"/> as a seed
        /// </summary>
        /// <param name="count">The number of bytes to generate</param>
        /// <param name="seed">Custom seed</param>
        /// <returns>An array of <paramref name="count"/> random bytes</returns>
        public static byte[] BytesSeeded(int count, int seed) {
            PushState(seed);
            var bytes = Bytes(count);
            PopState();
            return bytes;
        }

        private sealed class DefaultRandomProvider : IRandomProvider {
            // ReSharper disable once MemberHidesStaticFromOuterClass
            public uint Seed { get; set; } = (uint)DateTime.Now.Ticks;

            public int GetInt(uint iterations) {
                return (int)GetHash((int)iterations);
            }

            public float GetFloat(uint iterations) {
                return (float)((GetInt(iterations) - (double)int.MinValue) / uint.MaxValue);
            }

            private uint GetHash(int value) {
                var num1 = Rotate(Seed + 409645513U + 4U + (uint)(value * -1028477379), 17) * 658815097U;
                var num2 = (num1 ^ (num1 >> 15)) * 562027853U;
                var num3 = (num2 ^ (num2 >> 13)) * 3770947547U;
                return num3 ^ (num3 >> 16);
            }

            private static uint Rotate(uint value, int count) {
                return (value << count) | (value >> (32 - count));
            }
        }

        private sealed class PopStateScope : IDisposable {
            private bool m_Disposed;

            public void Dispose() {
                if (m_Disposed) {
                    return;
                }

                m_Disposed = true;
                PopState();
            }
        }
    }
}
