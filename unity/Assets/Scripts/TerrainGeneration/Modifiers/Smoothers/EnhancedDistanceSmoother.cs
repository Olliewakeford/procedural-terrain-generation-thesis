using System;
using System.Collections.Generic;
using UnityEngine;
using TerrainGeneration.Core;

namespace TerrainGeneration.Modifiers.Smoothers
{
    /// <summary>
    /// GPU-accelerated smoother that creates natural transitions based on distance from fixed points.
    /// Applies stronger smoothing near constrained areas and gradually reduces effect with distance.
    /// </summary>
    [Serializable]
    public class EnhancedDistanceSmoother : GpuTerrainOperation, ITerrainModifier
    {
        #region Properties & Fields
        private static readonly int DistanceMap = Shader.PropertyToID("DistanceMap");
        private static readonly int Smoothing = Shader.PropertyToID("BaseSmoothing");
        private static readonly int HeightProximityWeight = Shader.PropertyToID("ConstrainedHeightProximityWeight");
        private static readonly int MaxDistance = Shader.PropertyToID("MaxDistance");
        
        /// <summary>
        /// Base smoothing strength applied to terrain. Higher values create more aggressive smoothing.
        /// </summary>
        [SerializeField] private float baseSmoothing = 1f;
        
        /// <summary>
        /// Number of smoothing passes to apply. More iterations create smoother terrain at the cost of performance.
        /// </summary>
        [SerializeField] private int iterations = 10;
        
        /// <summary>
        /// Weight multiplier for neighboring points that are closer to constrained heights.
        /// Higher values create stronger influence from constrained areas.
        /// </summary>
        [SerializeField] private float constrainedHeightProximityWeight = 3f;
        
        // Distance-specific render texture
        private RenderTexture _distanceMapRT;
        
        /// <summary>
        /// Gets or sets the base smoothing strength.
        /// </summary>
        public float BaseSmoothing
        {
            get => baseSmoothing;
            set => baseSmoothing = value;
        }
        
        /// <summary>
        /// Gets or sets the number of smoothing iterations. Value is clamped to a minimum of 1.
        /// </summary>
        public int Iterations
        {
            get => iterations;
            set => iterations = Mathf.Max(1, value);
        }
        
        /// <summary>
        /// Gets or sets the weight multiplier for neighbors closer to constrained heights.
        /// Value is clamped to a minimum of 1.
        /// </summary>
        public float ConstrainedHeightProximityWeight
        {
            get => constrainedHeightProximityWeight;
            set => constrainedHeightProximityWeight = Mathf.Max(1f, value);
        }
        #endregion
        
        #region Interface Implementation
        /// <summary>
        /// Gets the name of this terrain operation.
        /// </summary>
        public string Name => "Enhanced Distance Modifier";
        
        /// <summary>
        /// Gets the operation type of this terrain operation.
        /// </summary>
        public TerrainOperationType OperationType => TerrainOperationType.Modifier;
        
        /// <summary>
        /// Indicates whether this operation requires distance grid information.
        /// Always returns true as this smoother depends on distance calculations.
        /// </summary>
        public bool RequiresDistanceGrid => true;
        
        /// <summary>
        /// Gets the name of the compute shader used for GPU acceleration.
        /// </summary>
        protected override string ShaderName => "Shaders/EnhancedDistanceSmoother";
        
        /// <summary>
        /// Gets the kernel name in the compute shader used for this operation.
        /// </summary>
        protected override string KernelName => "CSEnhancedDistanceSmooth";
        
        /// <summary>
        /// Applies smoothing operation to the specified height map with distance-based falloff.
        /// </summary>
        /// <param name="heightMap">The height map to modify.</param>
        /// <param name="width">Width of the height map.</param>
        /// <param name="height">Height of the height map.</param>
        /// <param name="shouldModify">Function that determines if a point should be modified.</param>
        /// <param name="distanceGrid">Grid of distances from each point to nearest constrained point.</param>
        public void ApplyOperation(float[,] heightMap, int width, int height, Func<int, int, bool> shouldModify, float[,] distanceGrid = null)
        {
            Modify(heightMap, width, height, shouldModify, distanceGrid);
        }
        
        /// <summary>
        /// Performs the distance-based smoothing operation on the terrain height map.
        /// Applies stronger smoothing near constrained areas and gradually reduces effect with distance.
        /// </summary>
        /// <param name="heightMap">The height map to modify.</param>
        /// <param name="width">Width of the height map.</param>
        /// <param name="height">Height of the height map.</param>
        /// <param name="shouldModify">Function that determines if a point should be modified.</param>
        /// <param name="distanceGrid">Grid of distances from each point to nearest constrained point.</param>
        /// <exception cref="System.ArgumentNullException">Thrown when distanceGrid is null.</exception>
        public void Modify(float[,] heightMap, int width, int height, Func<int, int, bool> shouldModify, float[,] distanceGrid = null)
        {
            // Find the maximum distance value for normalization
            float maxDistanceValue = TerrainManager.CalculateMaxDistance(distanceGrid, width, height);
            
            bool useGpu = ExecuteGpuOperation(heightMap, width, height, shouldModify, 
                (w, h, maskTexture) => ExecuteSmoothing(w, h, maskTexture, distanceGrid, maxDistanceValue));
            if (!useGpu)
            {
                FallbackToCPU(heightMap, width, height, shouldModify, distanceGrid, maxDistanceValue);
            }
        }
        
        /// <summary>
        /// Releases all GPU resources used by this operation.
        /// </summary>
        public override void Dispose()
        {
            base.Dispose();
            
            if (_distanceMapRT != null)
            {
                _distanceMapRT.Release();
                _distanceMapRT = null;
            }
        }
        
        /// <summary>
        /// Creates a deep copy of this terrain operation.
        /// </summary>
        /// <returns>A new instance of EnhancedDistanceSmoother with copied properties.</returns>
        ITerrainOperation ITerrainOperation.Clone()
        {
            return Clone();
        }
        
        /// <summary>
        /// Creates a deep copy of this smoother with identical parameters.
        /// </summary>
        /// <returns>A new instance of EnhancedDistanceSmoother with copied properties.</returns>
        public ITerrainModifier Clone()
        {
            return new EnhancedDistanceSmoother
            {
                baseSmoothing = this.baseSmoothing,
                iterations = this.iterations,
                constrainedHeightProximityWeight = this.constrainedHeightProximityWeight
            };
        }
        #endregion
        
        #region Private Methods
        private void ExecuteSmoothing(int width, int height, Texture2D maskTexture, float[,] distanceGrid, float maxDistanceValue)
        {
            EnsureDistanceRenderTexture(width, height); // Create or resize distance map texture
            
            UpdateDistanceMapTexture(distanceGrid, width, height, maxDistanceValue); // Convert distanceGrid to RenderTexture
            
            // Run multiple iterations
            for (int iteration = 0; iteration < iterations; iteration++)
            {
                // Copy current heightmap to original heightmap for reference
                Graphics.CopyTexture(HeightMapRT, OriginalHeightMapRT);
                
                SetCommonShaderParams(width, height, maskTexture); // Set common shader parameters
                
                // Set operation-specific parameters
                ComputeShader.SetTexture(KernelHandle, DistanceMap, _distanceMapRT);
                ComputeShader.SetFloat(Smoothing, baseSmoothing);
                ComputeShader.SetFloat(HeightProximityWeight, constrainedHeightProximityWeight);
                ComputeShader.SetFloat(MaxDistance, maxDistanceValue);
                
                DispatchComputeShader(width, height); // Dispatch the compute shader
            }
        }
        
        private void EnsureDistanceRenderTexture(int width, int height)
        {
            // Create or resize distance map texture
            if (_distanceMapRT == null || _distanceMapRT.width != width || _distanceMapRT.height != height)
            {
                if (_distanceMapRT != null)
                    _distanceMapRT.Release();
                
                _distanceMapRT = new RenderTexture(width, height, 0, RenderTextureFormat.RFloat)
                    {
                        enableRandomWrite = true
                    };
                _distanceMapRT.Create();
            }
        }
        
        private void UpdateDistanceMapTexture(float[,] distanceGrid, int width, int height, float maxDistance)
        {
            // Create temporary Texture to hold distance data
            Texture2D distanceMapTex = new Texture2D(width, height, TextureFormat.RFloat, false);
    
            // Fill with distance data
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float distanceValue = Mathf.Approximately(distanceGrid[y, x], float.MaxValue) ? maxDistance : distanceGrid[y, x];
                    distanceMapTex.SetPixel(x, y, new Color(distanceValue, 0, 0, 0));
                }
            }
            distanceMapTex.Apply();
    
            // Copy to RenderTexture
            RenderTexture.active = _distanceMapRT;
            Graphics.Blit(distanceMapTex, _distanceMapRT);
            RenderTexture.active = null;
    
            UnityEngine.Object.DestroyImmediate(distanceMapTex); // Clean up temporary texture
        }
        
        private void FallbackToCPU(float[,] heightMap, int width, int height, Func<int, int, bool> shouldModify,
                          float[,] distanceGrid, float maxDistanceValue)
        {
            for (int iteration = 0; iteration < iterations; iteration++)
            {
                // Create a copy of the height map to reference original heights
                float[,] originalHeightMap = new float[width, height];
                Array.Copy(heightMap, originalHeightMap, heightMap.Length);

                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        if (!shouldModify(x, y)) continue;

                        // Normalize the distance to [0,1] range
                        float normalizedDistance = distanceGrid[x, y] / maxDistanceValue;

                        // Calculate smoothing factor based on distance
                        float smoothingFactor = baseSmoothing * (1.0f - normalizedDistance);

                        // Fixed weight for the center point to ensure stability
                        float centerWeight = 1.0f;
                        float totalWeight = centerWeight;
                        float smoothedHeight = originalHeightMap[x, y] * centerWeight;

                        // Get neighboring heights and add their contribution
                        List<Vector2> neighbours = TerrainManager.GenerateNeighbours(new Vector2(x, y), width, height);

                        foreach (Vector2 n in neighbours)
                        {
                            int nx = (int)n.x;
                            int ny = (int)n.y;

                            // Calculate neighbor weight
                            float neighborWeight = smoothingFactor;

                            // Add road proximity weighting if enabled
                            if (distanceGrid[nx, ny] < distanceGrid[x, y])
                            {
                                // This neighbor is closer to road, give it more weight
                                neighborWeight *= constrainedHeightProximityWeight;
                            }

                            totalWeight += neighborWeight;
                            smoothedHeight += originalHeightMap[nx, ny] * neighborWeight;
                        }

                        heightMap[x, y] = smoothedHeight / totalWeight;
                    }
                }
            }
        }
        #endregion
    }
}