using System;
using UnityEngine;
using TerrainGeneration.Core;
using Random = UnityEngine.Random;

namespace TerrainGeneration.Generators
{
    /// <summary>
    /// Generator that creates mountains/hills using Voronoi tessellation.
    /// Places a specified number of peaks at random positions and applies
    /// height influence to surrounding terrain to create natural mountain formations.
    /// </summary>
    [Serializable]
    public class VoronoiGenerator : ITerrainGenerator
    {
        #region Properties & Fields
        /// <summary>
        /// Number of mountain/hill peaks to generate on the terrain.
        /// </summary>
        [SerializeField] private int peakCount = 6;
        
        /// <summary>
        /// Controls how quickly height decreases with distance from peaks (linear component).
        /// Higher values create steeper mountains with smaller bases.
        /// </summary>
        [SerializeField] private float fallRate = 1.5f;
        
        /// <summary>
        /// Exponential factor controlling height falloff with distance.
        /// Higher values create more abrupt transitions near peak bases.
        /// </summary>
        [SerializeField] private float dropOff = 7f;
        
        /// <summary>
        /// Minimum possible height for generated peaks.
        /// Values are normalized between 0-1.
        /// </summary>
        [SerializeField] private float minHeight = 0.1f;
        
        /// <summary>
        /// Maximum possible height for generated peaks.
        /// Values are normalized between 0-1.
        /// </summary>
        [SerializeField] private float maxHeight = 0.4f;
        
        /// <summary>
        /// When true, peaks will not be placed near constrained (prohibited) areas.
        /// Requires a distance grid to be calculated first.
        /// </summary>
        [SerializeField] private bool avoidConstrainedPoints = true;
        
        /// <summary>
        /// Maximum number of attempts to place each peak when avoiding constrained areas.
        /// Higher values increase chance of successful placement but may affect performance.
        /// </summary>
        [SerializeField] private int maxPlacementAttempts = 100;
        
        private const int DeterministicSeed = 42;
        
        /// <summary>
        /// Gets or sets the number of mountain/hill peaks to generate.
        /// </summary>
        public int PeakCount
        {
            get => peakCount;
            set => peakCount = value;
        }
        
        /// <summary>
        /// Gets or sets the linear falloff rate for height influence.
        /// Higher values create steeper mountains with smaller bases.
        /// </summary>
        public float FallRate
        {
            get => fallRate;
            set => fallRate = value;
        }
        
        /// <summary>
        /// Gets or sets the exponential factor controlling height falloff.
        /// Higher values create more abrupt transitions near peak bases.
        /// </summary>
        public float DropOff
        {
            get => dropOff;
            set => dropOff = value;
        }
        
        /// <summary>
        /// Gets or sets the minimum possible height for generated peaks (0-1 range).
        /// </summary>
        public float MinHeight
        {
            get => minHeight;
            set => minHeight = value;
        }
        
        /// <summary>
        /// Gets or sets the maximum possible height for generated peaks (0-1 range).
        /// </summary>
        public float MaxHeight
        {
            get => maxHeight;
            set => maxHeight = value;
        }
        
        /// <summary>
        /// Gets or sets whether peaks should avoid placement near constrained areas.
        /// Requires distance grid to be calculated first when true.
        /// </summary>
        public bool AvoidConstrainedPoints
        {
            get => avoidConstrainedPoints;
            set => avoidConstrainedPoints = value;
        }

        /// <summary>
        /// Gets or sets the maximum number of attempts to place each peak when avoiding constrained areas.
        /// </summary>
        public int MaxPlacementAttempts
        {
            get => maxPlacementAttempts;
            set => maxPlacementAttempts = value;
        }
        
        /// <summary>
        /// When true, uses a fixed seed for deterministic generation. When false, uses random seed.
        /// </summary>
        public bool UseDeterministicSeed { get; set; } = false;
        
        #endregion
        
        #region Interface Implementation
        /// <summary>
        /// Gets the name of this terrain generator.
        /// </summary>
        public string Name => "Voronoi";

        /// <summary>
        /// Gets the type of terrain operation this component performs.
        /// </summary>
        public TerrainOperationType OperationType => TerrainOperationType.Generator;

        /// <summary>
        /// Gets whether this generator requires the distance grid to function properly.
        /// Only needed when avoiding constrained points.
        /// </summary>
        public bool RequiresDistanceGrid => avoidConstrainedPoints;
        
        /// <summary>
        /// Applies the Voronoi terrain generation operation to the height map.
        /// </summary>
        /// <param name="heightMap">The terrain height map to modify.</param>
        /// <param name="width">Width of the height map.</param>
        /// <param name="height">Height of the height map.</param>
        /// <param name="shouldModify">Function that determines if a point can be modified.</param>
        /// <param name="distanceGrid">Optional distance grid for constrained point avoidance.</param>
        public void ApplyOperation(float[,] heightMap, int width, int height, Func<int, int, bool> shouldModify, float[,] distanceGrid = null)
        {
            Generate(heightMap, width, height, shouldModify, distanceGrid);
        }
        
        /// <summary>
        /// Generates Voronoi-based mountains and hills on the terrain.
        /// Places peaks at random positions and blends them into the surrounding terrain.
        /// </summary>
        /// <param name="heightMap">The terrain height map to modify.</param>
        /// <param name="width">Width of the height map.</param>
        /// <param name="height">Height of the height map.</param>
        /// <param name="shouldModify">Function that determines if a point can be modified.</param>
        /// <param name="distanceGrid">Optional distance grid for constrained point avoidance.</param>
        public void Generate(float[,] heightMap, int width, int height, Func<int, int, bool> shouldModify, float[,] distanceGrid = null)
        {
            if (UseDeterministicSeed)
            {
                UnityEngine.Random.InitState(DeterministicSeed);
            }
            
            // Only calculate distance requirements if we're avoiding constrained points
            int requiredDistanceFromConstraints = 0;
            
            if (avoidConstrainedPoints)
            {
                // Calculate the maximum radius a hill could influence
                float maxHillRadius = CalculateMaxHillRadius();
                
                // Convert to distance grid units (the distance grid stores integer distances)
                requiredDistanceFromConstraints = Mathf.CeilToInt(maxHillRadius * width);
            }
            
            for (int i = 0; i < peakCount; ++i)
            {
                bool validPeakFound = false;
                int attempts = 0;
                Vector3 peak = Vector3.zero;
                
                while (!validPeakFound && attempts < maxPlacementAttempts)
                {
                    // Choose a random point for peak
                    peak = new Vector3(
                        Random.Range(0, width),
                        Random.Range(minHeight, maxHeight),
                        Random.Range(0, height)
                    );
                    
                    // Check if this peak can be placed
                    bool canPlace = true;
                    
                    // If avoiding constrained points, check the distance grid
                    if (avoidConstrainedPoints && distanceGrid != null)
                    {
                        // Check if the peak is far enough from constrained areas based on the distance grid
                        if (distanceGrid[(int)peak.x, (int)peak.z] < requiredDistanceFromConstraints)
                        {
                            canPlace = false;
                        }
                    }
                    
                    // If the height is lower than existing terrain, we can't place here (avoid divots)
                    if (canPlace && heightMap[(int)peak.x, (int)peak.z] >= peak.y)
                    {
                        canPlace = false;
                    }
                    
                    validPeakFound = canPlace;
                    attempts++;
                }
                
                // If we couldn't find a valid placement after max attempts, skip this peak
                if (!validPeakFound)
                {
                    continue;
                }
                
                // Set the peak height
                if (shouldModify((int)peak.x, (int)peak.z))
                {
                    heightMap[(int)peak.x, (int)peak.z] = peak.y;
                }
                else // try another peak if we tried to place it on a constrained point
                {
                    peakCount--;
                    continue;
                }

                Vector2 peakLocation = new Vector2(peak.x, peak.z);
                float maxDistance = Vector2.Distance(new Vector2(0, 0), new Vector2(width, height));
                
                // Apply height influence to surrounding terrain
                for (int z = 0; z < height; z++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        // Skip the peak point itself
                        if (Mathf.Approximately(peak.x, x) && Mathf.Approximately(peak.z, z)) continue;
                        
                        // Calculate normalized distance to peak
                        float distanceToPeak = Vector2.Distance(peakLocation, new Vector2(x, z)) / maxDistance;
                        
                        // Calculate height influence using combined linear and power falloff
                        float distanceFactor = distanceToPeak * fallRate;
                        float dropOffFactor = Mathf.Pow(distanceToPeak, dropOff);
                        float h = peak.y - distanceFactor - dropOffFactor;
                        
                        // Only update if the new height is higher and the point is modifiable
                        if (heightMap[x, z] < h && shouldModify(x, z))
                        {
                            heightMap[x, z] = h;
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// Creates a deep copy of this terrain operation.
        /// </summary>
        /// <returns>A new instance of VoronoiGenerator with the same parameters.</returns>
        ITerrainOperation ITerrainOperation.Clone()
        {
            return Clone();
        }
        
        /// <summary>
        /// Creates a deep copy of this terrain generator.
        /// </summary>
        /// <returns>A new instance of VoronoiGenerator with the same parameters.</returns>
        public ITerrainGenerator Clone()
        {
            return new VoronoiGenerator
            {
                peakCount = this.peakCount,
                fallRate = this.fallRate,
                dropOff = this.dropOff,
                minHeight = this.minHeight,
                maxHeight = this.maxHeight,
                avoidConstrainedPoints = this.avoidConstrainedPoints,
                maxPlacementAttempts = this.maxPlacementAttempts,
                UseDeterministicSeed = this.UseDeterministicSeed
            };
        }
        #endregion

        #region Private Methods
        
        // Calculates how far a hill's influence extends from its peak.
        // Used to determine safe placement distance from constrained areas.
        private float CalculateMaxHillRadius()
        {
            const float minHeightContribution = 0.01f; // Hill's minimum noticeable height influence
            
            // Find radius where height contribution drops below minimum threshold
            // The height function is: peak.y - (distance*fallRate) - (distance^dropOff)
            
            // Calculate radius from linear term (distance*fallRate)
            float linearRadius = (maxHeight - minHeightContribution) / fallRate;
            
            // Calculate radius from power term (distance^dropOff)
            float powerRadius = Mathf.Pow(maxHeight - minHeightContribution, 1f / dropOff);
            
            // Use smaller radius as conservative estimate and add a margin
            return Mathf.Min(linearRadius, powerRadius) * 1.1f;
        }
        #endregion
    }
}