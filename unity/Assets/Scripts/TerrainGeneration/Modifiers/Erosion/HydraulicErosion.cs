using System;
using System.Collections.Generic;
using System.Linq;
using TerrainGeneration.Core;
using UnityEngine;
using Random = UnityEngine.Random;

namespace TerrainGeneration.Modifiers.Erosion
{
    /// <summary>
    /// Simulates simplified hydraulic erosion on terrain while respecting prohibited areas.
    /// Creates realistic water flow patterns such as valleys and channels by simulating water droplets
    /// flowing over and eroding the terrain.
    /// </summary>
    [Serializable]
    public class HydraulicErosion : ITerrainModifier
    {
        #region Properties & Fields
        
        /// <summary>
        /// Number of water droplets to simulate in the erosion process.
        /// Higher values create more erosion but increase computation time.
        /// </summary>
        [SerializeField] private int dropletCount = 1000;
        
        /// <summary>
        /// Determines how strongly each droplet erodes the terrain.
        /// Higher values create more pronounced erosion features.
        /// </summary>
        [SerializeField] private float erosionStrength = 0.2f;
        
        /// <summary>
        /// Number of water flow paths (springs/rivers) each droplet creates.
        /// Higher values create more branching erosion patterns.
        /// </summary>
        [SerializeField] private int springsPerDroplet = 3;
        
        /// <summary>
        /// Amount of sediment a droplet removes per step during erosion.
        /// Controls the rate at which terrain erodes along water flow paths.
        /// </summary>
        [SerializeField] private float solubility = 0.005f;
        
        private const int DETERMINISTIC_SEED = 42;
        
        private static System.Random _rng = new();
        
        /// <summary>
        /// Gets or sets the number of droplets to simulate in the erosion process.
        /// Value is clamped to a minimum of 1.
        /// </summary>
        public int DropletCount
        {
            get => dropletCount;
            set => dropletCount = Mathf.Max(1, value);
        }
        
        /// <summary>
        /// Gets or sets the erosion strength of each droplet.
        /// Value is clamped between 0.01 and 1.0.
        /// </summary>
        public float ErosionStrength
        {
            get => erosionStrength;
            set => erosionStrength = Mathf.Clamp(value, 0.01f, 1f);
        }
        
        /// <summary>
        /// Gets or sets the number of springs each droplet creates.
        /// Value is clamped to a minimum of 1.
        /// </summary>
        public int SpringsPerDroplet
        {
            get => springsPerDroplet;
            set => springsPerDroplet = Mathf.Max(1, value);
        }
        
        /// <summary>
        /// Gets or sets the solubility rate of the terrain.
        /// Controls how much sediment a droplet removes per step.
        /// Value is clamped between 0.001 and 0.1.
        /// </summary>
        public float Solubility
        {
            get => solubility;
            set => solubility = Mathf.Clamp(value, 0.001f, 0.1f);
        }
        
        /// <summary>
        /// When true, uses a fixed seed for deterministic generation. When false, uses random seed.
        /// </summary>
        public bool UseDeterministicSeed { get; set; } = false;
        
        #endregion
        
        #region Interface Implementation
        
        /// <summary>
        /// Gets the display name of this terrain operation.
        /// </summary>
        public string Name => "Hydraulic Erosion";
        
        /// <summary>
        /// Gets the type of terrain operation this component performs.
        /// </summary>
        public TerrainOperationType OperationType => TerrainOperationType.Modifier;
        
        /// <summary>
        /// Gets whether this operation requires the distance grid to function.
        /// Hydraulic erosion does not require distance information.
        /// </summary>
        public bool RequiresDistanceGrid => false;
        
        /// <summary>
        /// Applies the hydraulic erosion operation to the terrain heightmap.
        /// </summary>
        /// <param name="heightMap">The terrain heightmap to modify</param>
        /// <param name="width">Width of the heightmap</param>
        /// <param name="height">Height of the heightmap</param>
        /// <param name="shouldModify">Function that determines if a specific point can be modified</param>
        /// <param name="distanceGrid">Optional distance grid (not used by this operation)</param>
        public void ApplyOperation(float[,] heightMap, int width, int height, Func<int, int, bool> shouldModify, float[,] distanceGrid = null)
        {
            Modify(heightMap, width, height, shouldModify, distanceGrid);
        }
        
        /// <summary>
        /// Performs hydraulic erosion on the terrain heightmap by simulating water flow.
        /// </summary>
        /// <param name="heightMap">The terrain heightmap to modify</param>
        /// <param name="width">Width of the heightmap</param>
        /// <param name="height">Height of the heightmap</param>
        /// <param name="shouldModify">Function that determines if a specific point can be modified</param>
        /// <param name="distanceGrid">Optional distance grid (not used by this operation)</param>
        public void Modify(float[,] heightMap, int width, int height, Func<int, int, bool> shouldModify, float[,] distanceGrid = null)
        {
            if (UseDeterministicSeed)
            {
                Random.InitState(DETERMINISTIC_SEED);
                _rng = new System.Random(DETERMINISTIC_SEED);
            }
            
            float[,] erosionMap = new float[width, height]; // Create erosion map to store erosion values
            
            // Simulate water droplets
            for (int i = 0; i < dropletCount; i++)
            {
                // Choose a random position for the droplet (only on modifiable terrain)
                int posX, posY;
                int attempts = 0;
                do
                {
                    posX = Random.Range(0, width);
                    posY = Random.Range(0, height);
                    attempts++;
                    
                    if (attempts > 1000) // Prevent infinite loop if there are no valid positions after 1000 attempts
                    {
                        Debug.LogWarning("Failed to find valid droplet start position after 1000 attempts");
                        break;
                    }
                } while (!shouldModify(posX, posY));
                
                if (attempts > 1000) continue;
                
                // Set initial erosion value at droplet position
                Vector2 dropletPosition = new Vector2(posX, posY);
                erosionMap[posX, posY] = erosionStrength;
                
                // Run multiple springs from this droplet
                for (int j = 0; j < springsPerDroplet; j++)
                {
                    RunRiver(dropletPosition, heightMap, erosionMap, width, height, shouldModify);
                }
            }
            
            // Apply erosion to height map
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (erosionMap[x, y] > 0.0f && shouldModify(x, y))
                    {
                        heightMap[x, y] -= erosionMap[x, y];
                    }
                }
            }
        }
        
        /// <summary>
        /// Creates a clone of this terrain operation.
        /// </summary>
        /// <returns>A new instance of HydraulicErosion with the same parameters</returns>
        ITerrainOperation ITerrainOperation.Clone()
        {
            return Clone();
        }
        
        /// <summary>
        /// Creates a clone of this terrain smoother.
        /// </summary>
        /// <returns>A new instance of HydraulicErosion with the same parameters</returns>
        public ITerrainModifier Clone()
        {
            return new HydraulicErosion
            {
                dropletCount = this.dropletCount,
                erosionStrength = this.erosionStrength,
                springsPerDroplet = this.springsPerDroplet,
                solubility = this.solubility,
                UseDeterministicSeed = this.UseDeterministicSeed
            };
        }
        #endregion
        
        #region Private Methods
        /// <summary>
        /// Simulates water flowing downhill from the droplet position, eroding the terrain along the way
        /// </summary>
        private void RunRiver(Vector2 dropletPosition, float[,] heightMap, float[,] erosionMap, 
            int width, int height, Func<int, int, bool> shouldModify)
        {
            // Continue until erosion value at current position is depleted
            while (erosionMap[(int)dropletPosition.x, (int)dropletPosition.y] > 0)
            {
                // Get neighbors of current position
                List<Vector2> neighbors = TerrainManager.GenerateNeighbours(dropletPosition, width, height);
                
                // Randomly shuffle neighbors so its random which direction the droplet flows
                neighbors = neighbors.OrderBy(_ => _rng.Next()).ToList();
                
                bool foundLower = false;
                
                // Find a lower neighbor to flow to
                foreach (Vector2 neighbor in neighbors)
                {
                    int nx = (int)neighbor.x;
                    int ny = (int)neighbor.y;
                    
                    // If neighbor is lower height and modifiable, flow there
                    if (heightMap[nx, ny] < heightMap[(int)dropletPosition.x, (int)dropletPosition.y] 
                        && shouldModify(nx, ny))
                    {
                        // Erode the new position by transferring erosion value minus solubility
                        erosionMap[nx, ny] = erosionMap[(int)dropletPosition.x, (int)dropletPosition.y] - solubility;
                        
                        // Move to new position
                        dropletPosition = neighbor;
                        foundLower = true;
                        break;
                    }
                }
                
                // If no lower neighbor found, reduce erosion at current position
                if (!foundLower)
                {
                    erosionMap[(int)dropletPosition.x, (int)dropletPosition.y] -= solubility;
                }
                
                // If we've hit a prohibited area, stop the river
                if (!shouldModify((int)dropletPosition.x, (int)dropletPosition.y))
                {
                    break;
                }
            }
        }
        
        #endregion
    }
}