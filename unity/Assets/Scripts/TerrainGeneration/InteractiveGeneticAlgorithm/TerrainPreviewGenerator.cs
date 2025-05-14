using UnityEngine;
using TerrainGeneration.Core;

namespace TerrainGeneration.InteractiveGeneticAlgorithm
{
    /// <summary>
    /// Generates preview images of terrain variants for the evolution UI
    /// </summary>
    public class TerrainPreviewGenerator
    {
        #region Fields
        private readonly TerrainManager _terrainManager;
        private readonly Camera _previewCamera;
        private readonly int _previewResolution;
        private float[,] _originalHeights; // Store the original heights to restore after preview generation
        #endregion
        
        #region Constructor
        /// <summary>
        /// Creates a new terrain preview generator
        /// </summary>
        /// <param name="terrainManager">The terrain manager to use for previews</param>
        /// <param name="previewCamera">The camera to use for rendering previews</param>
        /// <param name="previewResolution">Resolution of preview images</param>
        public TerrainPreviewGenerator(TerrainManager terrainManager, Camera previewCamera, int previewResolution = 1024)
        {
            _terrainManager = terrainManager;
            _previewCamera = previewCamera;
            _previewResolution = previewResolution;
            
            SetupPreviewCamera(); // Configure the preview camera
        }
        #endregion
        
        #region Public Methods
        /// <summary>
        /// Generates a preview image of a terrain genome
        /// </summary>
        /// <param name="genome">The genome to preview</param>
        /// <returns>A texture containing the preview image</returns>
        public Texture2D GeneratePreview(TerrainGenome genome)
        {
            if (_terrainManager == null || _terrainManager.terrain == null || _previewCamera == null)
            {
                Debug.LogError("Cannot generate preview: missing terrain manager or preview camera");
                return null;
            }
            
            Terrain terrain = _terrainManager.terrain;
            TerrainData terrainData = terrain.terrainData;
            
            // Backup current terrain heights if not already done
            if (_originalHeights == null)
            {
                int resolution = terrainData.heightmapResolution;
                _originalHeights = terrainData.GetHeights(0, 0, resolution, resolution);
            }
            
            // Create render texture for preview
            RenderTexture renderTexture = new RenderTexture(
                _previewResolution,
                _previewResolution,
                24
            );
            
            _terrainManager.ApplyOperations(genome.Operations, $"Preview {genome.name}"); // Apply genome to terrain
            
            // Render preview
            _previewCamera.targetTexture = renderTexture;
            _previewCamera.Render();
            
            // Convert RenderTexture to Texture2D
            RenderTexture.active = renderTexture;
            Texture2D previewTexture = new Texture2D(
                _previewResolution,
                _previewResolution,
                TextureFormat.RGB24,
                false
            );
            
            previewTexture.ReadPixels(
                new Rect(0, 0, _previewResolution, _previewResolution),
                0, 0
            );
            previewTexture.Apply();
            
            terrainData.SetHeights(0, 0, _originalHeights); // Restore original terrain heights
            
            // Clean up
            RenderTexture.active = null;
            _previewCamera.targetTexture = null;
            renderTexture.Release();
            
            return previewTexture;
        }
        
        /// <summary>
        /// Captures the original terrain heights to restore later
        /// </summary>
        public void CaptureOriginalHeights()
        {
            if (_terrainManager == null || _terrainManager.terrain == null)
                return;
            
            TerrainData terrainData = _terrainManager.terrain.terrainData;
            int resolution = terrainData.heightmapResolution;
            _originalHeights = terrainData.GetHeights(0, 0, resolution, resolution);
        }
        
        /// <summary>
        /// Restores the original terrain heights
        /// </summary>
        public void RestoreOriginalHeights()
        {
            if (_terrainManager == null || _terrainManager.terrain == null || _originalHeights == null)
                return;
            
            TerrainData terrainData = _terrainManager.terrain.terrainData;
            terrainData.SetHeights(0, 0, _originalHeights);
        }
        #endregion
        
        #region Private Methods
        /// <summary>
        /// Sets up the preview camera position and orientation
        /// </summary>
        private void SetupPreviewCamera()
        {
            if (_previewCamera == null || _terrainManager == null || _terrainManager.terrain == null)
                return;
            
            Terrain terrain = _terrainManager.terrain;
            TerrainData terrainData = terrain.terrainData;
            
            // Get terrain dimensions
            Vector3 terrainSize = terrainData.size;
            float maxDimension = Mathf.Max(terrainSize.x, terrainSize.z);
            
            // Position camera at 45-degree angle to show height variations clearly
            _previewCamera.transform.position = new Vector3(
                terrainSize.x * 0.5f,
                terrainSize.y * 1.2f,
                -maxDimension * 0.5f
            );
            
            // Look at the center of the terrain
            _previewCamera.transform.LookAt(new Vector3(
                terrainSize.x * 0.5f,
                0,
                terrainSize.z * 0.5f
            ));
            
            // Set camera properties
            _previewCamera.fieldOfView = 60f;
            _previewCamera.nearClipPlane = 0.1f;
            _previewCamera.farClipPlane = maxDimension * 3f;
        }
        #endregion
    }
}