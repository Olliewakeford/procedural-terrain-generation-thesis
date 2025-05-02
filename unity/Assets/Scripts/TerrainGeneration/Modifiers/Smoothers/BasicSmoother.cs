using System;
using System.Collections.Generic;
using UnityEngine;
using TerrainGeneration.Core;

namespace TerrainGeneration.Modifiers.Smoothers
{
    /// <summary>
    /// GPU-accelerated implementation of a basic terrain smoothing algorithm.
    /// Provides automatic CPU fallback when GPU processing is unavailable.
    /// </summary>
    [Serializable]
    public class BasicSmoother : GpuTerrainOperation, ITerrainModifier
    {
        #region Properties & Fields
        /// <summary>
        /// Number of smoothing passes to apply to the terrain.
        /// Higher values produce more smoothed terrain.
        /// </summary>
        [SerializeField] private int iterations = 10;
        
        /// <summary>
        /// Gets or sets the number of smoothing iterations.
        /// Value is clamped to a minimum of 1.
        /// </summary>
        public int Iterations
        {
            get => iterations;
            set => iterations = Mathf.Max(1, value);
        }
        #endregion
        
        #region Interface Implementation
        /// <summary>
        /// Gets the display name of this terrain operation.
        /// </summary>
        public string Name => "GPU Basic Modifier";

        /// <summary>
        /// Gets the operation type classification.
        /// </summary>
        public TerrainOperationType OperationType => TerrainOperationType.Modifier;

        /// <summary>
        /// Gets whether this operation requires distance grid information.
        /// </summary>
        public bool RequiresDistanceGrid => false;
        
        /// <summary>
        /// Gets the compute shader resource path.
        /// </summary>
        protected override string ShaderName => "Shaders/BasicSmoother";

        /// <summary>
        /// Gets the compute shader kernel name to execute.
        /// </summary>
        protected override string KernelName => "CSBasicSmooth";
        
        /// <summary>
        /// Applies the smoothing operation to the provided height map.
        /// </summary>
        /// <param name="heightMap">The terrain height map to modify.</param>
        /// <param name="width">Width of the height map.</param>
        /// <param name="height">Height of the height map.</param>
        /// <param name="shouldModify">Function determining which areas can be modified.</param>
        /// <param name="distanceGrid">Optional distance grid (not used in this operation).</param>
        public void ApplyOperation(float[,] heightMap, int width, int height, Func<int, int, bool> shouldModify, float[,] distanceGrid = null)
        {
            Modify(heightMap, width, height, shouldModify, distanceGrid);
        }
        
        /// <summary>
        /// Smooths the terrain by averaging heights with neighboring points.
        /// Attempts GPU implementation first with CPU fallback if needed.
        /// </summary>
        /// <param name="heightMap">The terrain height map to modify.</param>
        /// <param name="width">Width of the height map.</param>
        /// <param name="height">Height of the height map.</param>
        /// <param name="shouldModify">Function determining which areas can be modified.</param>
        /// <param name="distanceGrid">Optional distance grid (not used in this operation).</param>
        public void Modify(float[,] heightMap, int width, int height, Func<int, int, bool> shouldModify, float[,] distanceGrid = null)
        {
            bool useGpu = ExecuteGpuOperation(heightMap, width, height, shouldModify, 
                ExecuteSmoothing);
            if (!useGpu)
            {
                FallbackToCPU(heightMap, width, height, shouldModify);
            }
        }
        
        /// <summary>
        /// Explicit interface implementation of Clone for ITerrainOperation.
        /// </summary>
        /// <returns>A new instance of this operation with copied properties.</returns>
        ITerrainOperation ITerrainOperation.Clone()
        {
            return Clone();
        }
        
        /// <summary>
        /// Creates a deep copy of this smoother operation.
        /// </summary>
        /// <returns>A new BasicSmoother instance with the same properties.</returns>
        public ITerrainModifier Clone()
        {
            return new BasicSmoother
            {
                iterations = this.iterations
            };
        }
        #endregion

        #region Private Methods
        private void ExecuteSmoothing(int width, int height, Texture2D maskTexture)
        {
            // Run multiple iterations
            for (int iteration = 0; iteration < iterations; iteration++)
            {
                Graphics.CopyTexture(HeightMapRT, OriginalHeightMapRT); // Copy current heightmap to original heightmap for reference
                
                SetCommonShaderParams(width, height, maskTexture); // Set common and operation-specific shader parameters
                
                DispatchComputeShader(width, height); // Dispatch the compute shader
            }
        }
        
        private void FallbackToCPU(float[,] heightMap, int width, int height, Func<int, int, bool> shouldModify)
        {
            float smoothProgress = 0;
            UnityEditor.EditorUtility.DisplayProgressBar("Basic Smoothing (CPU Fallback)", "Progress", smoothProgress);
            
            for (int i = 0; i < iterations; i++)
            {
                // Create a copy of the height map to reference original heights
                float[,] originalHeightMap = new float[width, height];
                Array.Copy(heightMap, originalHeightMap, heightMap.Length);
                
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        if (!shouldModify(x, y)) continue;
                        
                        float avgHeight = originalHeightMap[x, y];
                        List<Vector2> neighbours = TerrainManager.GenerateNeighbours(new Vector2(x, y), width, height);
                        
                        foreach (Vector2 n in neighbours)
                        {
                            avgHeight += originalHeightMap[(int)n.x, (int)n.y];
                        }
                        
                        // Set the height of the current point to the average height
                        heightMap[x, y] = avgHeight / ((float)neighbours.Count + 1);
                    }
                }
                
                smoothProgress++;
                UnityEditor.EditorUtility.DisplayProgressBar("Basic Smoothing (CPU Fallback)", "Progress", smoothProgress / iterations);
            }
            
            UnityEditor.EditorUtility.ClearProgressBar();
        }
        #endregion
    }
}