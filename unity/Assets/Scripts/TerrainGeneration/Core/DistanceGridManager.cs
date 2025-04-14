using System;
using System.Collections.Generic;
using UnityEngine;

namespace TerrainGeneration.Core
{
    /// <summary>
    /// Manages distance grid calculations which contain the distance from each pixel to the nearest protected area
    /// </summary>
    public class DistanceGridManager
    {
        #region Properties

        public float[,] DistanceGrid { get; private set; }

        public bool IsCalculated => DistanceGrid != null;
        
        #endregion

        #region Public Methods
        
        /// <summary>
        /// Calculates a grid where each cell contains the minimum distance to a protected area
        /// </summary>
       public void CalculateDistanceGrid(int resolution, Func<int, int, bool> shouldModify)
        {
        DistanceGrid = new float[resolution, resolution];
        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        
        // Initialize: 0 for fixed points, infinity for modifiable points
        for (int x = 0; x < resolution; x++)
        {
            for (int z = 0; z < resolution; z++)
            {
                if (!shouldModify(x, z))
                {
                    DistanceGrid[x, z] = 0f;
                    queue.Enqueue(new Vector2Int(x, z));
                }
                else
                {
                    DistanceGrid[x, z] = float.MaxValue;
                }
            }
        }
        
        // Modified BFS with Euclidean distances
        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();
            float currentDist = DistanceGrid[current.x, current.y];
            
            // Check all 8 neighbors (including diagonals)
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dz = -1; dz <= 1; dz++)
                {
                    if (dx == 0 && dz == 0) continue; // Skip center
                    
                    int newX = current.x + dx;
                    int newZ = current.y + dz;
                    
                    // Check bounds
                    if (newX < 0 || newX >= resolution || newZ < 0 || newZ >= resolution) continue;
                    
                    // Calculate Euclidean distance to this neighbor
                    float stepDistance = (dx != 0 && dz != 0) ? 1.414f : 1.0f; // √2 for diagonal, 1 for orthogonal
                    float newDistance = currentDist + stepDistance;
                    
                    // Update if we found a shorter path
                    if (newDistance < DistanceGrid[newX, newZ])
                    {
                        DistanceGrid[newX, newZ] = newDistance;
                        queue.Enqueue(new Vector2Int(newX, newZ));
                    }
                }
            }
        }
    }
        
        #endregion
    }
}