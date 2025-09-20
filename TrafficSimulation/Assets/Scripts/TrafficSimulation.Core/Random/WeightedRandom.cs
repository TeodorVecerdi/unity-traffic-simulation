namespace TrafficSimulation.Core.Random {
    public class WeightedRandom<T> {
        private readonly List<T> m_Elements = [];
        private readonly List<float> m_Weights = [];

        public WeightedRandom(IEnumerable<T> elements, IEnumerable<float> weights) {
            m_Elements.AddRange(elements);
            m_Weights.AddRange(weights);
            if (m_Weights.Count is 0) {
                throw new ArgumentException("At least one weight must be provided.");
            }

            var totalWeight = m_Weights.Sum();
            var cumulativeWeight = 0.0f;
            if (totalWeight <= 0.0f) {
                var uniformWeight = 1.0f / m_Weights.Count;
                for (var i = 0; i < m_Weights.Count; i++) {
                    cumulativeWeight += uniformWeight;
                    m_Weights[i] = cumulativeWeight;
                }

                return;
            }

            for (var i = 0; i < m_Weights.Count; i++) {
                cumulativeWeight += m_Weights[i];
                m_Weights[i] = cumulativeWeight / totalWeight;
            }
        }

        public T Next() {
            if (m_Weights.Count is 0) {
                throw new InvalidOperationException("No weights were provided.");
            }

            var randomValue = Rand.Float;
            for (var i = 0; i < m_Weights.Count; i++) {
                if (randomValue <= m_Weights[i]) {
                    return m_Elements[i];
                }
            }

            return m_Elements[^1];
        }
    }
}
