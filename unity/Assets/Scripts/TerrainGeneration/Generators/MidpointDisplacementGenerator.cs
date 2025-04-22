using System;
using UnityEngine;
using TerrainGeneration.Core;

namespace TerrainGeneration.Generators
{
    /// <summary>
    /// Implements terrain generation using the Diamond-Square algorithm (midpoint displacement)
    /// </summary>
    [Serializable]
    public class MidpointDisplacementGenerator : ITerrainGenerator
    {
        #region Properties & Fields
        /// <summary>
        /// Minimum height value for the generated terrain
        /// </summary>
        [SerializeField] private float minHeight = 0.01f;
        
        /// <summary>
        /// Maximum height value for the generated terrain
        /// </summary>
        [SerializeField] private float maxHeight = 0.6f;
        
        /// <summary>
        /// Controls the roughness/smoothness of the terrain. Higher values (closer to 1) create smoother terrain.
        /// </summary>
        [SerializeField] private float smoothness = 0.8f;
        
        /// <summary>
        /// Initial random displacement range (between height range) applied to terrain corners. 
        /// </summary>
        [SerializeField] private float initialRandomRange = 0.5f;
        
        private const int DETERMINISTIC_SEED = 42;
        /// <summary>
        /// Gets or sets the minimum height value for generated terrain.
        /// </summary>
        public float MinHeight
        {
            get => minHeight;
            set => minHeight = value;
        }

        /// <summary>
        /// Gets or sets the maximum height value for generated terrain.
        /// </summary>
        public float MaxHeight
        {
            get => maxHeight;
            set => maxHeight = value;
        }

        /// <summary>
        /// Gets or sets the smoothness factor of the terrain.
        /// Higher values (closer to 1) create smoother terrain features.
        /// Value is clamped between 0.1 and 1.0.
        /// </summary>
        public float Smoothness
        {
            get => smoothness;
            set => smoothness = Mathf.Clamp(value, 0.1f, 1.0f);
        }
        
        /// <summary>
        /// Gets or sets the initial random displacement range applied to terrain corners.
        /// Value is clamped between 0.0 and 1.0.
        /// </summary>
        public float InitialRandomRange
        {
            get => initialRandomRange;
            set => initialRandomRange = Mathf.Clamp(value, 0.0f, 1.0f);
        }
        
        /// <summary>
        /// When true, uses a fixed seed for deterministic generation. When false, uses random seed.
        /// </summary>
        public bool UseDeterministicSeed { get; set; } = false;
        #endregion
        
        #region Interface Implementation
        /// <summary>
        /// Gets the display name of this terrain generator.
        /// </summary>
        public string Name => "Midpoint Displacement";

        /// <summary>
        /// Gets the operation type (Generator or Modifier).
        /// </summary>
        public TerrainOperationType OperationType => TerrainOperationType.Generator;

        /// <summary>
        /// Gets whether this generator requires a distance grid.
        /// </summary>
        public bool RequiresDistanceGrid => false;

        /// <summary>
        /// Applies the midpoint displacement operation to the terrain heightmap.
        /// </summary>
        /// <param name="heightMap">The terrain heightmap to modify.</param>
        /// <param name="width">Width of the heightmap.</param>
        /// <param name="height">Height of the heightmap.</param>
        /// <param name="shouldModify">Function that determines if a specific point can be modified.</param>
        /// <param name="distanceGrid">Optional distance grid (not used by this generator).</param>
        public void ApplyOperation(float[,] heightMap, int width, int height, Func<int, int, bool> shouldModify, float[,] distanceGrid = null)
        {
            Generate(heightMap, width, height, shouldModify, distanceGrid);
        }
        
        /// <summary>
        /// Generates terrain using the Diamond-Square (midpoint displacement) algorithm.
        /// </summary>
        /// <param name="heightMap">The terrain heightmap to modify.</param>
        /// <param name="width">Width of the heightmap.</param>
        /// <param name="height">Height of the heightmap.</param>
        /// <param name="shouldModify">Function that determines if a specific point can be modified.</param>
        /// <param name="distanceGrid">Optional distance grid (not used by this generator).</param>
        public void Generate(float[,] heightMap, int width, int height, Func<int, int, bool> shouldModify, float[,] distanceGrid = null )
        {
            System.Random prng = UseDeterministicSeed ? 
                new System.Random(DETERMINISTIC_SEED) : 
                new System.Random();
            
            // Make sure we're working with a power of 2 plus 1 sized grid (necessary for diamond-square)
            int size = Mathf.NextPowerOfTwo(Mathf.Min(width, height) - 1);
            
            float heightRange = maxHeight - minHeight; // Store the original height range for later normalization
            
            // Create a displacement map to store our displacement values
            float[][] displacementMap = new float[width][];
            for (int index = 0; index < width; index++)
            {
                displacementMap[index] = new float[height];
            }

            // The corners of the terrain will receive random displacement
            float cornerRandom = initialRandomRange * heightRange;
            
            // Apply random displacement to the corners of the displacement map
            if (shouldModify(0, 0))
                displacementMap[0][0] = ((float)prng.NextDouble() * 2 - 1) * (cornerRandom / 2);
            if (shouldModify(0, size) && size < height)
                displacementMap[0][size] = ((float)prng.NextDouble() * 2 - 1) * (cornerRandom / 2);
            if (shouldModify(size, 0) && size < width)
                displacementMap[size][0] = ((float)prng.NextDouble() * 2 - 1) * (cornerRandom / 2);
            if (shouldModify(size, size) && size < width && size < height)
                displacementMap[size][size] = ((float)prng.NextDouble() * 2 - 1) * (cornerRandom / 2);
            
            // Diamond-Square algorithm to fill the displacement map
            int squareSize = size;
            float randomRange = cornerRandom;
            
            while (squareSize > 1)
            {
                int halfSize = squareSize / 2;
                
                // DIAMOND STEP
                for (int y = halfSize; y < height; y += squareSize)
                {
                    if (y >= height) continue; // Skip if out of bounds
                    
                    for (int x = halfSize; x < width; x += squareSize)
                    {
                        if (x >= width) continue; // Skip if out of bounds
                        
                        if (shouldModify(x, y))
                        {
                            // Get the four corners of the square
                            int x1 = Mathf.Max(x - halfSize, 0);
                            int y1 = Mathf.Max(y - halfSize, 0);
                            int x2 = Mathf.Min(x + halfSize, width - 1);
                            int y2 = Mathf.Min(y + halfSize, height - 1);
                            
                            // Calculate the average displacement of the four corners
                            float sum = 0f;
                            int count = 0;
                            
                            // Top-left corner
                            if (shouldModify(x1, y1))
                            {
                                sum += displacementMap[x1][y1];
                                count++;
                            }
                            
                            // Top-right corner
                            if (shouldModify(x2, y1))
                            {
                                sum += displacementMap[x2][y1];
                                count++;
                            }
                            
                            // Bottom-left corner
                            if (shouldModify(x1, y2))
                            {
                                sum += displacementMap[x1][y2];
                                count++;
                            }
                            
                            // Bottom-right corner
                            if (shouldModify(x2, y2))
                            {
                                sum += displacementMap[x2][y2];
                                count++;
                            }
                            
                            // Calculate average and add random displacement
                            float avg = count > 0 ? sum / count : 0;
                            displacementMap[x][y] = avg + ((float)prng.NextDouble() * 2 - 1) * (randomRange / 2);
                        }
                    }
                }
                
                // SQUARE STEP
                for (int y = 0; y < height; y += halfSize)
                {
                    for (int x = (y % squareSize == 0) ? halfSize : 0; x < width; x += squareSize)
                    {
                        if (x < width && y < height && shouldModify(x, y))
                        {
                            // Calculate average of the surrounding points
                            float sum = 0f;
                            int count = 0;
                            
                            // North neighbor
                            int northY = y - halfSize;
                            if (northY >= 0)
                            {
                                if (shouldModify(x, northY))
                                {
                                    sum += displacementMap[x][northY];
                                    count++;
                                }
                            }
                            
                            // South neighbor
                            int southY = y + halfSize;
                            if (southY < height)
                            {
                                if (shouldModify(x, southY))
                                {
                                    sum += displacementMap[x][southY];
                                    count++;
                                }
                            }
                            
                            // West neighbor
                            int westX = x - halfSize;
                            if (westX >= 0)
                            {
                                if (shouldModify(westX, y))
                                {
                                    sum += displacementMap[westX][y];
                                    count++;
                                }
                            }
                            
                            // East neighbor
                            int eastX = x + halfSize;
                            if (eastX < width)
                            {
                                if (shouldModify(eastX, y))
                                {
                                    sum += displacementMap[eastX][y];
                                    count++;
                                }
                            }
                            
                            if (count > 0)
                            {
                                float avg = sum / count;
                                // Add scaled random displacement to the average
                                displacementMap[x][y] = avg + ((float)prng.NextDouble() * 2 - 1) * (randomRange / 2);
                            }
                        }
                    }
                }
                
                // Reduce random range for next iteration - higher smoothness = less reduction
                randomRange *= Mathf.Pow(2, -smoothness);
                
                // Move to next smaller square size
                squareSize = halfSize;
            }
            
            // Find min/max values in the displacement map for normalization
            float dispMin = float.MaxValue;
            float dispMax = float.MinValue;
            
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (shouldModify(x, y))
                    {
                        dispMin = Mathf.Min(dispMin, displacementMap[x][y]);
                        dispMax = Mathf.Max(dispMax, displacementMap[x][y]);
                    }
                }
            }
            
            // Apply the displacement to the actual heightmap
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (shouldModify(x, y))
                    {
                        float displacement;
                        
                        // Normalize the displacement
                        if ( dispMin < dispMax) // This will be true almost certainly, but just in case 
                        {
                            float normalizedDisp = (displacementMap[x][y] - dispMin) / (dispMax - dispMin);
                            displacement = minHeight + normalizedDisp * heightRange;
                        }
                        else
                        {
                            // Just clamp to the min/max range
                            displacement = Mathf.Clamp(displacementMap[x][y], minHeight, maxHeight);
                        }
                        
                        // Add the displacement to the existing height
                        heightMap[x, y] += displacement;
                    }
                }
            }
        }
        
        /// <summary>
        /// Creates a deep copy of this terrain operation.
        /// </summary>
        /// <returns>A new instance of MidpointDisplacementGenerator with the same parameters.</returns>
        ITerrainOperation ITerrainOperation.Clone()
        {
            return Clone();
        }

        /// <summary>
        /// Creates a deep copy of this terrain generator.
        /// </summary>
        /// <returns>A new instance of MidpointDisplacementGenerator with the same parameters.</returns>
        public ITerrainGenerator Clone()
        {
            return new MidpointDisplacementGenerator
            {
                minHeight = this.minHeight,
                maxHeight = this.maxHeight,
                smoothness = this.smoothness,
                initialRandomRange = this.initialRandomRange,
                UseDeterministicSeed = this.UseDeterministicSeed
            };
        }
        #endregion
    }
}