using System;
using UnityEngine;
using TerrainGeneration.Core;

namespace TerrainGeneration.Generators
{
    /// <summary>
    /// Generates terrain using Perlin noise with fractal Brownian motion.
    /// </summary>
    [Serializable]
    public class PerlinNoiseGenerator : ITerrainGenerator
    {
        #region Properties & Fields
        /// <summary>
        /// Controls the scale of the noise pattern. Lower values create larger, smoother features.
        /// </summary>
        [SerializeField] private float frequency = 0.002f;
        
        /// <summary>
        /// Number of noise layers to combine. Higher values create more detailed terrain.
        /// </summary>
        [SerializeField] private int octaves = 6;
        
        /// <summary>
        /// Controls how much each octave contributes to the final terrain. Higher values create more pronounced detail.
        /// </summary>
        [SerializeField] private float persistence = 0.5f;
        
        /// <summary>
        /// Controls the overall height of the noise effect.
        /// </summary>
        [SerializeField] private float amplitude = 0.6f;

        private Vector2 _randomOffset;
        
        private const int DETERMINISTIC_SEED = 42;
        
        /// <summary>
        /// Gets or sets the frequency of the Perlin noise, controlling the scale of terrain features.
        /// </summary>
        public float Frequency
        {
            get => frequency;
            set => frequency = value;
        }
        
        /// <summary>
        /// Gets or sets the number of noise layers (octaves) to stack. Higher values create more detailed terrain.
        /// </summary>
        public int Octaves
        {
            get => octaves;
            set => octaves = value;
        }
        
        /// <summary>
        /// Gets or sets how much each successive octave contributes to the final shape.
        /// Clamped to be between 0.0 and 1.0, as we want successive octaves to contribute less.
        /// </summary>
        public float Persistence
        {
            get => persistence;
            set => persistence = Mathf.Clamp(value, 0.0f, 1.0f);
        }
        
        /// <summary>
        /// Gets or sets the height of the generated noise.
        /// </summary>
        public float Amplitude
        {
            get => amplitude;
            set => amplitude = value;
        }
        
        /// <summary>
        /// When true, uses a fixed seed for deterministic generation. When false, uses random seed.
        /// </summary>
        public bool UseDeterministicSeed { get; set; } = false;
        
        #endregion
        
        #region Interface Implementation
        /// <summary>
        /// Gets the display name of this generator.
        /// </summary>
        public string Name => "Perlin Noise";
        
        /// <summary>
        /// Gets the operation type for this component.
        /// </summary>
        public TerrainOperationType OperationType => TerrainOperationType.Generator;
        
        /// <summary>
        /// Gets whether this generator requires a distance grid.
        /// </summary>
        public bool RequiresDistanceGrid => false;
        
        /// <summary>
        /// Applies Perlin noise generation to the provided heightmap.
        /// </summary>
        /// <param name="heightMap">The heightmap to modify.</param>
        /// <param name="width">Width of the heightmap.</param>
        /// <param name="height">Height of the heightmap.</param>
        /// <param name="shouldModify">Function that determines whether a point should be modified.</param>
        /// <param name="distanceGrid">Optional distance grid for distance-aware operations (not used).</param>
        public void ApplyOperation(float[,] heightMap, int width, int height, Func<int, int, bool> shouldModify, float[,] distanceGrid = null)
        {
            Generate(heightMap, width, height, shouldModify, distanceGrid);
        }
        
        /// <summary>
        /// Generates Perlin noise terrain on the provided heightmap.
        /// </summary>
        /// <param name="heightMap">The heightmap to modify.</param>
        /// <param name="width">Width of the heightmap.</param>
        /// <param name="height">Height of the heightmap.</param>
        /// <param name="shouldModify">Function that determines whether a point should be modified.</param>
        /// <param name="distanceGrid">Optional distance grid for distance-aware operations (not used).</param>
        public void Generate(float[,] heightMap, int width, int height, Func<int, int, bool> shouldModify, float[,] distanceGrid = null)
        {
            if (UseDeterministicSeed)
            {
                UnityEngine.Random.InitState(DETERMINISTIC_SEED);
            }

            _randomOffset = new Vector2(
                UnityEngine.Random.Range(0, 1000),
                UnityEngine.Random.Range(0, 1000)
            );
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (shouldModify(x, y))
                    {
                        // Use fractional Brownian motion for Perlin noise
                        heightMap[x, y] += FractalBrownianMotion(
                            (x + _randomOffset.x) * frequency,
                            (y + _randomOffset.y) * frequency,
                            octaves,
                            persistence
                        ) * amplitude;
                    }
                }
            }
        }
        
        
        /// <summary>
        /// Creates a deep copy of this operator.
        /// </summary>
        /// <returns>A new instance of the PerlinNoiseGenerator with the same parameter values.</returns>
        ITerrainOperation ITerrainOperation.Clone()
        {
            return Clone();
        }
    
        /// <summary>
        /// Creates a deep copy of this generator.
        /// </summary>
        /// <returns>A new instance of the PerlinNoiseGenerator with the same parameter values.</returns>
        public ITerrainGenerator Clone()
        {
            return new PerlinNoiseGenerator
            {
                frequency = this.frequency,
                octaves = this.octaves,
                persistence = this.persistence,
                amplitude = this.amplitude,
                UseDeterministicSeed = this.UseDeterministicSeed
            };
        }
        #endregion
        
        #region Private Methods
        // Function to generate Fractal Brownian Motion for Perlin Noise
        private static float FractalBrownianMotion(float x, float y, int oct, float persistence)
        {
            float total = 0.0f;    // Total accumulated value from all octaves
            float frequency = 1.0f; // Starting frequency for the first octave
            float amplitude = 1.0f; // Starting amplitude for the first octave
            float maxValue = 0.0f;  // Used to normalize the final value between 0 and 1

            for (int i = 0; i < oct; ++i)
            {
                // Add the current octave's Perlin noise value, scaled by amplitude
                total += Mathf.PerlinNoise(x * frequency, y * frequency) * amplitude; 
                
                maxValue += amplitude; // Keep track of the total possible amplitude for normalization later
                amplitude *= persistence; // Persistence controls how much the amplitude decreases with each octave
                frequency *= 2.0f; // Frequency change with each octave, (<1 adds detail)
            }

            // Return the normalized total value (0 to 1 range) after applying all octaves
            return total / maxValue;
        }
        #endregion
    }
}