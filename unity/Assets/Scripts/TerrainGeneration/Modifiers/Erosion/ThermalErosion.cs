using System;
using System.Collections.Generic;
using UnityEngine;
using TerrainGeneration.Core;

namespace TerrainGeneration.Modifiers.Erosion
{
    /// <summary>
    /// GPU-accelerated implementation of thermal erosion that simulates material slumping on terrain.
    /// Provides realistic slope behavior by gradually moving material from higher to lower areas.
    /// </summary>
    [Serializable]
    public class ThermalErosion : GpuTerrainOperation, ITerrainModifier
    {
        #region Properties & Fields
        private static readonly int Threshold = Shader.PropertyToID("ErosionThreshold");
        private static readonly int Rate = Shader.PropertyToID("ErosionRate");
        
        /// <summary>
        /// Number of erosion iterations to perform. Higher values create more pronounced erosion effects.
        /// </summary>
        [SerializeField] private int iterations = 50;
        
        /// <summary>
        /// Threshold difference in height required to trigger erosion. Controls the minimum slope steepness that will erode.
        /// </summary>
        [SerializeField] private float erosionThreshold = 0.001f;
        
        /// <summary>
        /// Rate at which material is transferred during erosion (range 0-1). Higher values create faster, more dramatic erosion.
        /// </summary>
        [SerializeField] private float erosionRate = 0.1f;
        
        /// <summary>
        /// Gets or sets the number of erosion iterations to perform.
        /// Minimum value is 1.
        /// </summary>
        public int Iterations
        {
            get => iterations;
            set => iterations = Mathf.Max(1, value);
        }
        
        /// <summary>
        /// Gets or sets the threshold difference in height required to trigger erosion.
        /// Minimum value is 0.0001.
        /// </summary>
        public float ErosionThreshold
        {
            get => erosionThreshold;
            set => erosionThreshold = Mathf.Max(0.0001f, value);
        }
        
        /// <summary>
        /// Gets or sets the rate at which material is transferred during erosion.
        /// Value is clamped between 0 and 1.
        /// </summary>
        public float ErosionRate
        {
            get => erosionRate;
            set => erosionRate = Mathf.Clamp01(value);
        }
        #endregion
        
        #region Interface Implementation
        /// <summary>
        /// Gets the display name of this terrain operation.
        /// </summary>
        public string Name => "GPU Thermal Erosion";

        /// <summary>
        /// Gets the type of this terrain operation.
        /// </summary>
        public TerrainOperationType OperationType => TerrainOperationType.Modifier;

        /// <summary>
        /// Gets whether this operation requires distance grid information.
        /// </summary>
        public bool RequiresDistanceGrid => false;
        
        /// <summary>
        /// Gets the name of the compute shader to use for GPU-accelerated operation.
        /// </summary>
        protected override string ShaderName => "Shaders/ThermalErosion";

        /// <summary>
        /// Gets the name of the kernel in the compute shader to use.
        /// </summary>
        protected override string KernelName => "CSThermalErosion";
        
        /// <summary>
        /// Applies the thermal erosion operation to the given height map.
        /// </summary>
        /// <param name="heightMap">The height map to modify.</param>
        /// <param name="width">Width of the height map.</param>
        /// <param name="height">Height of the height map.</param>
        /// <param name="shouldModify">Function that determines if a given point can be modified.</param>
        /// <param name="distanceGrid">Optional distance grid (not used by this operation).</param>
        public void ApplyOperation(float[,] heightMap, int width, int height, Func<int, int, bool> shouldModify, float[,] distanceGrid = null)
        {
            Modify(heightMap, width, height, shouldModify, distanceGrid);
        }
        
        /// <summary>
        /// Applies thermal erosion to smooth the terrain by simulating material slumping.
        /// Uses GPU acceleration when available, with fallback to CPU implementation.
        /// </summary>
        /// <param name="heightMap">The height map to modify.</param>
        /// <param name="width">Width of the height map.</param>
        /// <param name="height">Height of the height map.</param>
        /// <param name="shouldModify">Function that determines if a given point can be modified.</param>
        /// <param name="distanceGrid">Optional distance grid (not used by this operation).</param>
        public void Modify(float[,] heightMap, int width, int height, Func<int, int, bool> shouldModify, float[,] distanceGrid = null)
        {
            bool useGpu = ExecuteGpuOperation(heightMap, width, height, shouldModify, 
                ExecuteErosion);
            
            if (!useGpu)
            {
                FallbackToCPU(heightMap, width, height, shouldModify);
            }
        }
        
        /// <summary>
        /// Creates a deep copy of this terrain operation.
        /// </summary>
        /// <returns>A new instance of ThermalErosion with the same parameters.</returns>
        ITerrainOperation ITerrainOperation.Clone()
        {
            return Clone();
        }
        
        /// <summary>
        /// Creates a deep copy of this terrain smoother.
        /// </summary>
        /// <returns>A new instance of ThermalErosion with the same parameters.</returns>
        public ITerrainModifier Clone()
        {
            return new ThermalErosion
            {
                iterations = this.iterations,
                erosionThreshold = this.erosionThreshold,
                erosionRate = this.erosionRate
            };
        }
        #endregion
        
        #region Private Methods
        private void ExecuteErosion(int width, int height, Texture2D maskTexture)
        {
            // Run multiple iterations
            for (int iteration = 0; iteration < iterations; iteration++)
            {
                Graphics.CopyTexture(HeightMapRT, OriginalHeightMapRT); // Copy current heightmap to original heightmap for reference
                
                SetCommonShaderParams(width, height, maskTexture); // Set common shader parameters
                
                // Set operation-specific parameters
                ComputeShader.SetFloat(Threshold, erosionThreshold);
                ComputeShader.SetFloat(Rate, erosionRate);
                
                DispatchComputeShader(width, height); // Dispatch the compute shader
            }
        }
        
        // Runs the thermal erosion operation on the CPU as a fallback
        private void FallbackToCPU(float[,] heightMap, int width, int height, Func<int, int, bool> shouldModify)
        {
            float erosionProgress = 0;
            UnityEditor.EditorUtility.DisplayProgressBar("Thermal Erosion (CPU Fallback)", "Progress", erosionProgress);
            
            for (int iteration = 0; iteration < iterations; iteration++)
            {
                // Create a copy of the height map to read from
                float[,] originalHeightMap = new float[width, height];
                Array.Copy(heightMap, originalHeightMap, heightMap.Length);
                
                // Process each cell in the heightmap
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        // Skip if we can't modify this cell
                        if (!shouldModify(x, y)) continue;
                        
                        // Get neighboring cells
                        List<Vector2> neighbors = TerrainManager.GenerateNeighbours(new Vector2(x, y), width, height);
                        
                        foreach (Vector2 neighbor in neighbors)
                        {
                            int nx = (int)neighbor.x;
                            int ny = (int)neighbor.y;
                            
                            // Skip if this neighbor can't be modified
                            if (!shouldModify(nx, ny)) continue;
                            
                            // Check if our height exceeds the neighbor's height by more than the erosion threshold
                            if (originalHeightMap[x, y] > originalHeightMap[nx, ny] + erosionThreshold)
                            {
                                // Calculate the amount to erode (percentage of height difference)
                                float heightDifference = originalHeightMap[x, y] - originalHeightMap[nx, ny];
                                float transferAmount = heightDifference * erosionRate;

                                // Clamp transfer amount to prevent negative heights
                                float maxTransferFromSource = heightMap[x, y];
                                float maxTransferToTarget = 1.0f - heightMap[nx, ny];
                                transferAmount = Mathf.Min(transferAmount, maxTransferFromSource, maxTransferToTarget);

                                // Apply the erosion with the clamped transfer amount
                                if (transferAmount > 0)
                                {
                                    heightMap[x, y] -= transferAmount;
                                    heightMap[nx, ny] += transferAmount;
                                }
                            }
                        }
                    }
                }
                
                erosionProgress = (float)(iteration + 1) / iterations;
                UnityEditor.EditorUtility.DisplayProgressBar("Thermal Erosion (CPU Fallback)", 
                    $"Iteration {iteration + 1}/{iterations}", erosionProgress);
            }
            
            UnityEditor.EditorUtility.ClearProgressBar();
        }
        #endregion
        
    }
}