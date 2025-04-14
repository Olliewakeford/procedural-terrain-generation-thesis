using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TerrainGeneration.Core
{
    /// <summary>
    /// Main class responsible for managing terrain generation and modifications with support for constrained areas.
    /// </summary>
    [ExecuteInEditMode]
    public class TerrainManager : MonoBehaviour
    {
        #region Properties and Fields
        
        /// <summary>
        /// Reference to the Unity Terrain component being managed.
        /// </summary>
        public Terrain terrain;

        /// <summary>
        /// Reference to the TerrainData asset containing heightmap and other terrain settings.
        /// </summary>
        public TerrainData terrainData;
        
        /// <summary>
        /// Mask texture defining prohibited areas where terrain heights cannot be modified.
        /// Red pixels (R > 0.1) represent fixed areas, while black pixels are modifiable.
        /// </summary>
        public Texture2D mask;

        /// <summary>
        /// Determines whether to reset modifiable areas to zero height before applying operations.
        /// </summary>
        public bool restoreTerrain;
        
        private DistanceGridManager _distanceGridManager;

        private bool DistanceGridCalculated => _distanceGridManager?.IsCalculated ?? false;
        
        private int HeightmapResolution => terrainData != null ? terrainData.heightmapResolution : 0;
        
        [SerializeField] private bool useDeterministicGeneration = false;
        
        public bool UseDeterministicGeneration 
        { 
            get => useDeterministicGeneration; 
            set => useDeterministicGeneration = value; 
        }
        
        #endregion

        #region Unity Lifecycle

        private void OnEnable()
        {
            if (terrain == null)
            {
                terrain = GetComponent<Terrain>();
            }
    
            if (terrain != null)
            {
                terrainData = terrain.terrainData;
            }
            else
            {
                Debug.LogError("Terrain not found. This script should be attached to a terrain game object.");
                return;
            }
    
            _distanceGridManager = new DistanceGridManager();
        }

        #endregion

        #region Public Core Methods
        
        /// <summary>
        /// Applies a single operation to the terrain.
        /// </summary>
        /// <param name="operation">The terrain operation to apply</param>
        /// <param name="operationName">Name of the operation for undo functionality</param>
        public void ApplyOperation(ITerrainOperation operation, string operationName = "Apply Operation")
        {
            // Create a one-element list and use apply operations method
            var operations = new List<ITerrainOperation> { operation };
            ApplyOperations(operations, operationName);
        }
        
        /// <summary>
        /// Applies a list of operations to the terrain in sequence.
        /// </summary>
        /// <param name="operations">The list of terrain operations to apply</param>
        /// <param name="operationName">Name of the operation for undo functionality</param>
        public void ApplyOperations(List<ITerrainOperation> operations, string operationName = "Apply Operations")
        {
            if (!terrainData) return;

            RecordUndo(operationName); // Record undo state before any modifications

            // Check if there are any generators in the operations list
            bool hasGenerators = operations.Any(op => op?.OperationType == TerrainOperationType.Generator);
    
            // Only restore terrain if restoreTerrain is true AND we have at least one generator, don't restore if we have only modifiers
            bool originalRestoreValue = restoreTerrain;
            if (restoreTerrain && hasGenerators)
            {
                InternalRestoreTerrain();
            }
            
            restoreTerrain = false; // Don't reset between operations

            // Apply operations in the specified order
            foreach (var operation in operations)
            {
                if (operation == null) continue;
        
                // Auto-calculate the distance grid if needed
                if (operation.RequiresDistanceGrid && !DistanceGridCalculated)
                {
                    CalculateDistanceGrid();
                }
                
                float[,] heightMap = GetHeightMap();

                operation.ApplyOperation(
                    heightMap,
                    HeightmapResolution,
                    HeightmapResolution,
                    ShouldModifyTerrain,
                    operation.RequiresDistanceGrid ? _distanceGridManager?.DistanceGrid : null
                );
                
                terrainData.SetHeights(0, 0, heightMap); // Update the terrain
            }

            restoreTerrain = originalRestoreValue;
        }
        
        
        /// <summary>
        /// Restores the terrain to its base state while preserving protected areas.
        /// </summary>
        public void RestoreTerrain()
        {
            RecordUndo("Restore Terrain");
            InternalRestoreTerrain();
        }
        #endregion
        
        #region Private Methods
        // This method allows the user to use Unity's Undo to revert changes made to the terrain
        private void RecordUndo(string operationName)
        {
            #if UNITY_EDITOR
            if (terrain && terrain.terrainData) // Record full Undo for terrain data 
            {
                UnityEditor.Undo.RegisterCompleteObjectUndo(terrain.terrainData, operationName);
            }
            #endif
        }
        
        /// <summary>
        /// Internal implementation of restore terrain without recording undo.
        /// </summary>
        private void InternalRestoreTerrain()
        {
            if (!terrainData) return;
            
            float[,] resetHeightMap = new float[HeightmapResolution, HeightmapResolution];
            
            float[,] currentHeightMap = terrainData.GetHeights(0, 0, HeightmapResolution, HeightmapResolution);
            
            for (int y = 0; y < HeightmapResolution; y++)
            {
                for (int x = 0; x < HeightmapResolution; x++)
                {
                    if (!ShouldModifyTerrain(x, y)) // If this point should not be modified, keep the current height
                    {
                        resetHeightMap[x, y] = currentHeightMap[x, y];
                    }
                    else // Otherwise, set the height to 0
                    {
                        resetHeightMap[x, y] = 0;
                    }
                }
            }
            
            terrainData.SetHeights(0, 0, resetHeightMap); // Apply the reset heightmap to the terrain
        }
        
        private void CalculateDistanceGrid()
        {
            _distanceGridManager ??= new DistanceGridManager();
            _distanceGridManager.CalculateDistanceGrid(HeightmapResolution, ShouldModifyTerrain);
        }

        /// <summary>
        /// Converts terrain heightmap coordinates to mask texture coordinates.
        /// </summary>
        /// <remarks>
        /// In Unity, terrain uses X,Z for the horizontal plane, while 2D textures use X,Y.
        /// This conversion ensures the mask properly aligns with the terrain:
        /// - Terrain X maps to Mask Y
        /// - Terrain Z maps to Mask X
        /// </remarks>
        /// <param name="terrainX">X coordinate in terrain space</param>
        /// <param name="terrainZ">Z coordinate in terrain space</param>
        /// <returns>Vector2 with mask texture coordinates (x,y)</returns>
        private Vector2 TerrainToMaskCoordinates(int terrainX, int terrainZ)
        {
            float maskX = terrainZ / (float)HeightmapResolution;
            float maskY = terrainX / (float)HeightmapResolution;
            return new Vector2(maskX, maskY);
        }

        /// <summary>
        /// Determines whether a specific terrain point can be modified based on the mask texture.
        /// </summary>
        /// <param name="terrainX">X coordinate in terrain space</param>
        /// <param name="terrainZ">Z coordinate in terrain space</param>
        /// <returns>True if the point can be modified, false if it's in a protected area</returns>
        private bool ShouldModifyTerrain(int terrainX, int terrainZ)
        {
            if (!mask) return true;
    
            // Convert terrain coordinates to mask texture coordinates
            Vector2 maskCoords = TerrainToMaskCoordinates(terrainX, terrainZ);
    
            // Sample the mask for its colour at this coordinate
            Color maskColor = mask.GetPixelBilinear(maskCoords.x, maskCoords.y);
    
            // Return true (allow modification) only for dark areas of the mask
            return maskColor is { r: < 0.1f, g: < 0.1f, b: < 0.1f };
        }
        
        /// <summary>
        /// Gets the current heightmap, optionally resetting modifiable areas to 0.
        /// </summary>
        /// <returns>A 2D array representing the heightmap</returns>
        private float[,] GetHeightMap()
        {
            if (!terrainData) return null;
            
            float[,] currentHeightMap = terrainData.GetHeights(0, 0, HeightmapResolution, HeightmapResolution);
            
            if (!restoreTerrain)
            {
                return currentHeightMap; // Return the current heightmap if not resetting
            }
            
            // Create a new heightmap to modify
            float[,] modifiedHeightMap = new float[HeightmapResolution, HeightmapResolution];
                
            for (int y = 0; y < HeightmapResolution; y++)
            {
                for (int x = 0; x < HeightmapResolution; x++)
                {
                    if (!ShouldModifyTerrain(x, y)) // If this point should not be modified, retain the original height
                    {
                        modifiedHeightMap[x, y] = currentHeightMap[x, y];
                    }
                    else // Otherwise, set the height to 0
                    {
                        modifiedHeightMap[x, y] = 0;
                    }
                }
            }
                
            return modifiedHeightMap;
        }
        #endregion
        
        #region Public Utility Methods
        /// <summary>
        /// Generates a list of neighboring points around a given position.
        /// </summary>
        /// <param name="pos">The central position to find neighbors for</param>
        /// <param name="width">Width of the grid</param>
        /// <param name="height">Height of the grid</param>
        /// <returns>A list of valid neighboring positions</returns>
        public static List<Vector2> GenerateNeighbours(Vector2 pos, int width, int height)
        {
            List<Vector2> neighbours = new List<Vector2>();
    
            // Loop through a 3x3 grid centered on the given position
            for (int y = -1; y < 2; y++)
            {
                for (int x = -1; x < 2; x++)
                {
                    if (x == 0 && y == 0) // Skip the center point (current position)
                        continue;

                    // Calculate the neighbor position
                    int neighbourX = (int)pos.x + x;
                    int neighbourY = (int)pos.y + y;
            
                    // Skip neighbors that are out of bounds
                    if (neighbourX < 0 || neighbourX >= width || neighbourY < 0 || neighbourY >= height)
                        continue;
                    
                    Vector2 neighbourPos = new Vector2(neighbourX, neighbourY);
                    neighbours.Add(neighbourPos); // Add valid neighbour
                }
            }
            return neighbours;
        }
        
        /// <summary>
        /// Calculates the maximum distance value in the distance grid.
        /// </summary>
        /// <param name="distanceGrid">The distance grid to analyze</param>
        /// <param name="width">Width of the grid</param>
        /// <param name="height">Height of the grid</param>
        /// <returns>The maximum distance value found</returns>
        public static float CalculateMaxDistance(float[,] distanceGrid, int width, int height)
        {
            if (distanceGrid == null) return 0;

            float maxDistanceValue = 0;
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (!Mathf.Approximately(distanceGrid[x, y], float.MaxValue) && distanceGrid[x, y] > maxDistanceValue)
                    {
                        maxDistanceValue = distanceGrid[x, y];
                    }
                }
            }
            return maxDistanceValue;
        }
        
        /// <summary>
        /// Calculates a global reference height based on all fixed points.
        /// </summary>
        /// <param name="method">The method to use for determining the reference height</param>
        /// <returns>The calculated reference height value</returns>
        public float CalculateReferenceHeight(ReferenceHeightMethod method)
        {
            if (!terrainData) return 0f;
    
            float[,] heightMap = terrainData.GetHeights(0, 0, HeightmapResolution, HeightmapResolution);
            int width = HeightmapResolution;
            int height = HeightmapResolution;
    
            float sum = 0;
            float min = float.MaxValue;
            float max = float.MinValue;
            int count = 0;
    
            // Collect heights from all fixed points
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (ShouldModifyTerrain(x, y)) continue;
            
                    float fixedHeight = heightMap[x, y];
                    sum += fixedHeight;
                    min = Mathf.Min(min, fixedHeight);
                    max = Mathf.Max(max, fixedHeight);
                    count++;
                }
            }
            
            return method switch 
            {
                ReferenceHeightMethod.AverageOfFixedPoints => count > 0 ? sum / count : 0,
                ReferenceHeightMethod.MinimumOfFixedPoints => count > 0 ? min : 0,
                ReferenceHeightMethod.MaximumOfFixedPoints => count > 0 ? max : 0,
                _ => 0,
            };
        }
        #endregion
    }
}