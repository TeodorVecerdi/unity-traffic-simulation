namespace TrafficSimulation.Core.Random {
    public interface IRandomProvider {
        /// <summary>
        /// Seed used to generate values
        /// </summary>
        public uint Seed { get; set; }

        /// <summary>
        /// Returns a random integer based on the seed and <paramref name="iterations"/>.
        /// </summary>
        public int GetInt(uint iterations);

        /// <summary>
        /// Returns a random float between 0 and 1 based on the seed and <paramref name="iterations"/>.
        /// </summary>
        /// <param name="iterations"></param>
        /// <returns></returns>
        public float GetFloat(uint iterations);
    }
}
