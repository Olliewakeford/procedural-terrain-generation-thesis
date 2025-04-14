using System;

namespace TerrainGeneration.Core
{
    /// <summary>
    /// Common interface for all terrain operations (generators and smoothers)
    /// </summary>
    public interface ITerrainOperation
    {
        /// <summary>
        /// The Name of the terrain operation
        /// </summary>
        string Name { get; }

        /// <summary>
        /// The type of operation (Generator or Modifier)
        /// </summary>
        TerrainOperationType OperationType { get; }

        /// <summary>
        /// Indicates whether this operation requires a distance grid
        /// </summary>
        bool RequiresDistanceGrid { get; }

        /// <summary>
        /// Applies the operation to the terrain
        /// </summary>
        /// <param name="heightMap">The heightmap to modify</param>
        /// <param name="width">Width of the heightmap</param>
        /// <param name="height">Height of the heightmap</param>
        /// <param name="shouldModify">Function that determines if a point should be modified</param>
        /// <param name="distanceGrid">Optional distance grid for distance-based operations</param>
        void ApplyOperation(float[,] heightMap, int width, int height, Func<int, int, bool> shouldModify, float[,] distanceGrid = null);

        /// <summary>
        /// Creates a copy of the operation with its current configuration
        /// </summary>
        ITerrainOperation Clone();
    }

    /// <summary>
    /// Defines the type of terrain operation
    /// </summary>
    public enum TerrainOperationType
    {
        Generator,
        Modifier
    }
    
    
    /// <summary>
    /// Interface for all terrain generation algorithms
    /// </summary>
    public interface ITerrainGenerator : ITerrainOperation
    {
        /// <summary>
        /// Applies the generation algorithm to the provided heightmap
        /// </summary>
        /// <param name="heightMap">The heightmap to modify</param>
        /// <param name="width">Width of the heightmap</param>
        /// <param name="height">Height of the heightmap</param>
        /// <param name="shouldModify">Function that determines if a point should be modified</param>
        /// <param name="distanceGrid">Optional distance grid for distance-based generation (null by default)</param>
        void Generate(float[,] heightMap, int width, int height, Func<int, int, bool> shouldModify, float[,] distanceGrid = null);
        
        /// <summary>
        /// Creates a copy of the generator with its current configuration
        /// </summary>
        new ITerrainGenerator Clone();
    }

    /// <summary>
    /// Interface for terrain smoothing algorithms
    /// </summary>
    public interface ITerrainModifier : ITerrainOperation
    {
        /// <summary>
        /// Applies the smoothing algorithm to the provided heightmap
        /// </summary>
        /// <param name="heightMap">The heightmap to smooth</param>
        /// <param name="width">Width of the heightmap</param>
        /// <param name="height">Height of the heightmap</param>
        /// <param name="shouldModify">Function that determines if a point should be modified</param>
        /// <param name="distanceGrid">Optional distance grid for distance-based smoothing</param>
        void Modify(float[,] heightMap, int width, int height, Func<int, int, bool> shouldModify, float[,] distanceGrid = null);
    
        /// <summary>
        /// Creates a copy of the smoother with its current configuration
        /// </summary>
        new ITerrainModifier Clone();
    }
    
    /// <summary>
    /// Reference height method options
    /// </summary>
    public enum ReferenceHeightMethod
    {
        AverageOfFixedPoints, // Use average height of all fixed points
        MinimumOfFixedPoints, // Use minimum height of all fixed points
        MaximumOfFixedPoints, // Use maximum height of all fixed points
    }
}