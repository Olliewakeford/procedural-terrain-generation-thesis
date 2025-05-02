using System;
using TerrainGeneration.Core;
using UnityEngine;

namespace TerrainGeneration.Modifiers.HeightAdjusters
{
    /// <summary>
    /// Height adjustment method options for the uniform height modifier.
    /// </summary>
    public enum HeightAdjustmentMethod
    {
        /// <summary>
        /// Normalize terrain so the lowest modifiable point is at height 0.
        /// </summary>
        NormalizeHeight,
        
        /// <summary>
        /// Use a reference height from fixed points.
        /// </summary>
        ReferenceHeight,
        
        /// <summary>
        /// Adjust heights by a relative amount.
        /// </summary>
        RelativeAdjustment
    }

    /// <summary>
    /// Modifier that applies a uniform height change to the terrain.
    /// </summary>
    [Serializable]
    public class UniformHeightModifier : ITerrainModifier
    {
        #region Properties & Fields
        
        /// <summary>
        /// Amount to adjust terrain height, in Unity terrain height units.
        /// </summary>
        [SerializeField] private float uniformStep = 0.1f;
        
        /// <summary>
        /// Method used to determine how height adjustment is applied.
        /// </summary>
        [SerializeField] private HeightAdjustmentMethod adjustmentMethod = HeightAdjustmentMethod.RelativeAdjustment;
        
        /// <summary>
        /// Method used to calculate reference height when using ReferenceHeight adjustment method.
        /// </summary>
        [SerializeField] private ReferenceHeightMethod referenceMethod = ReferenceHeightMethod.AverageOfFixedPoints;
        
        /// <summary>
        /// Gets or sets the height adjustment amount.
        /// </summary>
        public float UniformStep
        {
            get => uniformStep;
            set => uniformStep = value;
        }
        
        /// <summary>
        /// Gets or sets the method used to adjust heights.
        /// </summary>
        public HeightAdjustmentMethod AdjustmentMethod
        {
            get => adjustmentMethod;
            set => adjustmentMethod = value;
        }
        
        /// <summary>
        /// Gets or sets the method used to calculate reference height.
        /// </summary>
        public ReferenceHeightMethod ReferenceMethod
        {
            get => referenceMethod;
            set => referenceMethod = value;
        }
        
        #endregion
        
        #region Interface Implementation
        
        /// <summary>
        /// The display name of this modifier.
        /// </summary>
        public string Name => "Uniform Height";
        
        /// <summary>
        /// The type of terrain operation.
        /// </summary>
        public TerrainOperationType OperationType => TerrainOperationType.Modifier;
        
        /// <summary>
        /// Indicates if this operation requires a distance grid.
        /// </summary>
        public bool RequiresDistanceGrid => false;
        
        /// <summary>
        /// Applies the uniform height modification operation to the terrain.
        /// </summary>
        /// <param name="heightMap">The heightmap to modify</param>
        /// <param name="width">Width of the heightmap</param>
        /// <param name="height">Height of the heightmap</param>
        /// <param name="shouldModify">Function that determines if a point should be modified</param>
        /// <param name="distanceGrid">Optional distance grid (not used by this modifier)</param>
        public void ApplyOperation(float[,] heightMap, int width, int height, Func<int, int, bool> shouldModify, float[,] distanceGrid = null)
        {
            Modify(heightMap, width, height, shouldModify, distanceGrid);
        }
        
        /// <summary>
        /// Applies uniform height adjustments on the terrain.
        /// </summary>
        /// <param name="heightMap">The heightmap to modify</param>
        /// <param name="width">Width of the heightmap</param>
        /// <param name="height">Height of the heightmap</param>
        /// <param name="shouldModify">Function that determines if a point should be modified</param>
        /// <param name="distanceGrid">Optional distance grid (not used by this modifier)</param>
        public void Modify(float[,] heightMap, int width, int height, Func<int, int, bool> shouldModify, float[,] distanceGrid = null)
        {
            // Handle NormalizeHeight method
            if (adjustmentMethod == HeightAdjustmentMethod.NormalizeHeight)
            {
                // Find minimum height in all modifiable areas
                float minHeight = float.MaxValue;
                bool foundModifiablePoints = false;
                
                for (int x = 0; x < width; x++)
                {
                    for (int z = 0; z < height; z++)
                    {
                        if (shouldModify(x, z))
                        {
                            foundModifiablePoints = true;
                            if (heightMap[x, z] < minHeight)
                            {
                                minHeight = heightMap[x, z];
                            }
                        }
                    }
                }
                
                // If we found any modifiable points, normalize all modifiable points
                if (foundModifiablePoints)
                {
                    // Subtract the minimum height from all modifiable points
                    for (int x = 0; x < width; x++)
                    {
                        for (int z = 0; z < height; z++)
                        {
                            if (shouldModify(x, z))
                            {
                                heightMap[x, z] -= minHeight;
                            }
                        }
                    }
                }
                
                return; // Early return after normalization is complete
            }
            
            // Default adjustment value is the uniform step
            float adjustmentValue = uniformStep;
    
            // Try to find TerrainManager for reference height calculation
            var terrainManager = UnityEngine.Object.FindFirstObjectByType<TerrainManager>();
    
            // For ReferenceHeight method, replace adjustment value with calculated reference
            if (adjustmentMethod == HeightAdjustmentMethod.ReferenceHeight && terrainManager != null)
            {
                adjustmentValue = terrainManager.CalculateReferenceHeight(referenceMethod);
            }
    
            // Apply the height adjustment
            for (int x = 0; x < width; x++)
            {
                for (int z = 0; z < height; z++)
                {
                    if (shouldModify(x, z))
                    {
                        if (adjustmentMethod == HeightAdjustmentMethod.RelativeAdjustment)
                        {
                            // Add the adjustment value
                            heightMap[x, z] += adjustmentValue;
                        }
                        else // subtract the ReferenceHeight 
                        {
                            heightMap[x, z] -= adjustmentValue;
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// Creates a deep copy of this terrain operation.
        /// </summary>
        /// <returns>A new instance of UniformHeightModifier with the same settings</returns>
        ITerrainOperation ITerrainOperation.Clone()
        {
            return Clone();
        }
        
        /// <summary>
        /// Creates a deep copy of this modifier.
        /// </summary>
        /// <returns>A new instance of UniformHeightModifier with the same settings</returns>
        public ITerrainModifier Clone()
        {
            return new UniformHeightModifier
            {
                uniformStep = this.uniformStep,
                adjustmentMethod = this.adjustmentMethod,
                referenceMethod = this.referenceMethod,
            };
        }
        #endregion
    }
}