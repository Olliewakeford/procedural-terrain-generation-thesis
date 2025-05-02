using System;
using TerrainGeneration.Core;
using UnityEngine;

namespace TerrainGeneration.Modifiers.HeightAdjusters
{
    /// <summary>
    /// Scales terrain heights based on distance from prohibited areas to create natural transitions.
    /// Uses distance information to blend heights between modifiable and fixed points.
    /// </summary>
    [Serializable]
    public class DistanceBasedHeightScaler : ITerrainModifier
    {
        #region Properties & Fields
        /// <summary>
        /// Maximum scaling factor to apply to heights. Lower values create more dramatic scaling effects.
        /// Values range from 0 to 1, where 0 flattens terrain completely near boundaries and 1 has no effect.
        /// </summary>
        [SerializeField] private float maxScaleFactor = 0.5f;
        
        /// <summary>
        /// Method used to determine the reference height for blending operations.
        /// Controls how the algorithm calculates the target height to blend towards.
        /// </summary>
        [SerializeField] private ReferenceHeightMethod referenceMethod = ReferenceHeightMethod.AverageOfFixedPoints;
        
        /// <summary>
        /// Gets or sets the maximum scaling factor applied to terrain heights.
        /// Automatically clamps values between 0 and 1.
        /// </summary>
        public float MaxScaleFactor
        {
            get => maxScaleFactor;
            set => maxScaleFactor = Mathf.Clamp(value, 0f, 1f);
        }
        
        /// <summary>
        /// Gets or sets the method used to determine reference heights for blending.
        /// </summary>
        public ReferenceHeightMethod ReferenceMethod
        {
            get => referenceMethod;
            set => referenceMethod = value;
        }
        #endregion
        
        #region Interface Implementation
        /// <summary>
        /// Gets the display name of this terrain operation.
        /// </summary>
        public string Name => "Distance-Based Height Scaler";

        /// <summary>
        /// Gets the type of this terrain operation.
        /// </summary>
        public TerrainOperationType OperationType => TerrainOperationType.Modifier;

        /// <summary>
        /// Indicates that this operation requires a distance grid to function.
        /// </summary>
        public bool RequiresDistanceGrid => true;

        /// <summary>
        /// Applies the height scaling operation to the terrain heightmap.
        /// </summary>
        /// <param name="heightMap">The heightmap to modify</param>
        /// <param name="width">Width of the heightmap</param>
        /// <param name="height">Height of the heightmap</param>
        /// <param name="shouldModify">Function determining if a point should be modified</param>
        /// <param name="distanceGrid">Grid containing distances to fixed points</param>
        public void ApplyOperation(float[,] heightMap, int width, int height, Func<int, int, bool> shouldModify, float[,] distanceGrid = null)
        {
            Modify(heightMap, width, height, shouldModify, distanceGrid);
        }
        
        /// <summary>
        /// Smooths the terrain by scaling heights based on distance from fixed points.
        /// Points closer to fixed areas are scaled more aggressively toward reference heights.
        /// </summary>
        /// <param name="heightMap">The heightmap to modify</param>
        /// <param name="width">Width of the heightmap</param>
        /// <param name="height">Height of the heightmap</param>
        /// <param name="shouldModify">Function determining if a point should be modified</param>
        /// <param name="distanceGrid">Grid containing distances to fixed points</param>
        public void Modify(float[,] heightMap, int width, int height, Func<int, int, bool> shouldModify,
            float[,] distanceGrid = null)
        {
            if (distanceGrid == null)
            {
                return;
            }

            float maxDistanceValue = TerrainManager.CalculateMaxDistance(distanceGrid, width, height);

            float referenceHeight = 0f; // 0 if reference height doesnt work
            var terrainManager = UnityEngine.Object.FindFirstObjectByType<TerrainManager>();
            if (terrainManager)
            {
                referenceHeight = terrainManager.CalculateReferenceHeight(referenceMethod);
            }
            
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (!shouldModify(x, y)) continue;

                    // Normalize the distance to [0,1] range
                    float normalizedDistance = distanceGrid[x, y] / maxDistanceValue;
                    
                    // Calculate scaling factor based on distance
                    // Linear interpolation between maxScaleFactor (near fixed points) and 1.0 (far from fixed points)
                    float scalingRange = 1.0f - maxScaleFactor;
                    float scalingFactor = maxScaleFactor + (scalingRange * normalizedDistance);
                    
                    // Apply scaling
                    float originalHeight = heightMap[x, y];
                    float scaledHeight = originalHeight * scalingFactor;
                    
                    // Blend between reference height and scaled height based on distance
                    float offsetBlend = 1.0f - normalizedDistance;
                    scaledHeight = Mathf.Lerp(scaledHeight, referenceHeight, offsetBlend * (1 - scalingFactor));
                    
                    heightMap[x, y] = scaledHeight;
                }
            }
        }
        
        /// <summary>
        /// Creates a deep copy of this terrain operation.
        /// </summary>
        /// <returns>A new instance of DistanceBasedHeightScaler with the same parameters</returns>
        ITerrainOperation ITerrainOperation.Clone()
        {
            return Clone();
        }

        /// <summary>
        /// Creates a deep copy of this smoother.
        /// </summary>
        /// <returns>A new instance of DistanceBasedHeightScaler with the same parameters</returns>
        public ITerrainModifier Clone()
        {
            return new DistanceBasedHeightScaler
            {
                maxScaleFactor = this.maxScaleFactor,
                referenceMethod = this.referenceMethod,
            };
        }
        #endregion
    }
}