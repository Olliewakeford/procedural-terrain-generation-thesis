using System;
using UnityEngine;

namespace TerrainGeneration.Core
{
    /// <summary>
    /// Base class for GPU-accelerated terrain operations.
    /// Provides common functionality for shader management, texture processing, and 
    /// efficient terrain manipulation using compute shaders.
    /// </summary>
    [Serializable]
    public abstract class GpuTerrainOperation : IDisposable
    {
        #region Properties & Fields
        private static readonly int HeightMap = Shader.PropertyToID("HeightMap");
        private static readonly int OriginalHeightMap = Shader.PropertyToID("OriginalHeightMap");
        private static readonly int MaskTexture = Shader.PropertyToID("MaskTexture");
        private static readonly int Width = Shader.PropertyToID("Width");
        private static readonly int Height = Shader.PropertyToID("Height");

        /// <summary>
        /// RenderTexture for the heightmap that's currently being processed.
        /// </summary>
        protected RenderTexture HeightMapRT;
        
        /// <summary>
        /// RenderTexture containing the original heightmap data for reference.
        /// </summary>
        protected RenderTexture OriginalHeightMapRT;
        
        /// <summary>
        /// Reference to the compute shader that performs the terrain operation.
        /// </summary>
        protected ComputeShader ComputeShader;
        
        /// <summary>
        /// Handle to the specific kernel in the compute shader that will be executed.
        /// </summary>
        protected int KernelHandle;
        
        /// <summary>
        /// Flag indicating whether the compute shader has been initialized successfully.
        /// </summary>
        protected bool Initialized;
        
        /// <summary>
        /// The name of the compute shader resource to load.
        /// </summary>
        protected abstract string ShaderName { get; }
        
        /// <summary>
        /// The name of the specific kernel within the compute shader to use.
        /// </summary>
        protected abstract string KernelName { get; }
        #endregion
        
        #region Protected Methods
        /// <summary>
        /// Initializes the compute shader and finds the appropriate kernel.
        /// </summary>
        /// <returns>True if initialization was successful, false otherwise.</returns>
        protected bool InitializeShader()
        {
            if (Initialized) return true;
            
            // Load the compute shader
            ComputeShader = Resources.Load<ComputeShader>(ShaderName);
            if (ComputeShader != null)
            {
                try {
                    KernelHandle = ComputeShader.FindKernel(KernelName);
                    Initialized = true;
                    return true;
                }
                catch (Exception e) {
                    Debug.LogError($"Found shader {ShaderName} but couldn't find kernel {KernelName}: {e.Message}");
                }
            }
            else
            {
                Debug.LogError($"Failed to load compute shader: {ShaderName}");
            }
            
            return false;
        }
        
        /// <summary>
        /// Creates or resizes the RenderTextures used for heightmap processing.
        /// </summary>
        /// <param name="width">Width of the heightmap</param>
        /// <param name="height">Height of the heightmap</param>
        protected void EnsureRenderTextures(int width, int height)
        {
            // Create or resize heightmap texture
            if (HeightMapRT == null || HeightMapRT.width != width || HeightMapRT.height != height)
            {
                if (HeightMapRT != null)
                    HeightMapRT.Release();
                
                HeightMapRT = new RenderTexture(width, height, 0, RenderTextureFormat.RFloat)
                    {
                        enableRandomWrite = true
                    };
                HeightMapRT.Create();
            }
            
            // Create or resize original heightmap texture
            if (OriginalHeightMapRT == null || OriginalHeightMapRT.width != width || OriginalHeightMapRT.height != height)
            {
                if (OriginalHeightMapRT != null)
                    OriginalHeightMapRT.Release();
                
                OriginalHeightMapRT = new RenderTexture(width, height, 0, RenderTextureFormat.RFloat)
                    {
                        enableRandomWrite = true
                    };
                OriginalHeightMapRT.Create();
            }
        }
        
        /// <summary>
        /// Creates a mask texture from a shouldModify function to define protected areas.
        /// </summary>
        /// <param name="shouldModify">Function that returns true for areas that can be modified</param>
        /// <param name="width">Width of the mask texture</param>
        /// <param name="height">Height of the mask texture</param>
        /// <returns>A texture where black pixels indicate modifiable areas and red pixels indicate protected areas</returns>
        protected Texture2D CreateMaskTexture(Func<int, int, bool> shouldModify, int width, int height)
        {
            // Create a new mask texture
            Texture2D maskTexture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            
            // Fill with data based on shouldModify function
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    bool canModify = shouldModify(y, x);
                    
                    // Black for areas that can be modified
                    // and red for areas that cant be modified
                    Color pixel = canModify ? Color.black : Color.red;
                    maskTexture.SetPixel(x, y, pixel);
                }
            }
            
            maskTexture.Apply();
            return maskTexture;
        }
        
        /// <summary>
        /// Converts a float heightmap array to a RenderTexture for GPU processing.
        /// </summary>
        /// <param name="heightMap">The 2D array containing height values</param>
        /// <param name="width">Width of the heightmap</param>
        /// <param name="height">Height of the heightmap</param>
        protected void UpdateHeightMapTexture(float[,] heightMap, int width, int height)
        {
            Texture2D heightmapTex = new Texture2D(width, height, TextureFormat.RFloat, false);
            
            // Fill with heightmap data
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    // Unity's terrain data is stored with [y,x] indexing
                    heightmapTex.SetPixel(x, y, new Color(heightMap[y, x], 0, 0, 0));
                }
            }
            heightmapTex.Apply();
            
            // Copy to RenderTexture
            RenderTexture.active = HeightMapRT;
            Graphics.Blit(heightmapTex, HeightMapRT);
            RenderTexture.active = null;
            
            UnityEngine.Object.DestroyImmediate(heightmapTex); // Clean up temporary texture
        }
        
        /// <summary>
        /// Reads heightmap data from the GPU back into the provided float array.
        /// </summary>
        /// <param name="heightMap">The 2D array to write height values into</param>
        /// <param name="width">Width of the heightmap</param>
        /// <param name="height">Height of the heightmap</param>
        protected void ReadHeightMapFromGPU(float[,] heightMap, int width, int height)
        {
            Texture2D resultTex = new Texture2D(width, height, TextureFormat.RFloat, false);
    
            // Read from RenderTexture
            RenderTexture.active = HeightMapRT;
            resultTex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            resultTex.Apply();
            RenderTexture.active = null;
    
            // Copy data back to heightMap array
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    heightMap[y, x] = resultTex.GetPixel(x, y).r;
                }
            }
    
            UnityEngine.Object.DestroyImmediate(resultTex); // clean up temporary texture
        } 
        
        /// <summary>
        /// Initializes shader and textures in preparation for GPU operation.
        /// </summary>
        /// <param name="width">Width of the heightmap</param>
        /// <param name="height">Height of the heightmap</param>
        /// <returns>True if preparation was successful, false if CPU fallback should be used</returns>
        protected bool PrepareForGpuOperation(int width, int height)
        {
            if (!InitializeShader())
                return false;
                
            EnsureRenderTextures(width, height);
            return true;
        }
        
        /// <summary>
        /// Sets the common parameters required by most compute shader operations.
        /// </summary>
        /// <param name="width">Width of the heightmap</param>
        /// <param name="height">Height of the heightmap</param>
        /// <param name="maskTexture">Mask texture defining protected areas</param>
        protected void SetCommonShaderParams(int width, int height, Texture2D maskTexture)
        {
            ComputeShader.SetTexture(KernelHandle, HeightMap, HeightMapRT);
            ComputeShader.SetTexture(KernelHandle, OriginalHeightMap, OriginalHeightMapRT);
            ComputeShader.SetTexture(KernelHandle, MaskTexture, maskTexture);
            ComputeShader.SetInt(Width, width);
            ComputeShader.SetInt(Height, height);
        }
        
        /// <summary>
        /// Dispatches the compute shader with appropriate thread group dimensions.
        /// </summary>
        /// <param name="width">Width of the heightmap</param>
        /// <param name="height">Height of the heightmap</param>
        protected void DispatchComputeShader(int width, int height)
        {
            // Calculate dispatch dimensions (16x16 thread groups)
            int dispatchX = Mathf.CeilToInt(width / 16.0f);
            int dispatchY = Mathf.CeilToInt(height / 16.0f);
            
            // Dispatch the compute shader
            ComputeShader.Dispatch(KernelHandle, dispatchX, dispatchY, 1);
        }
        
        /// <summary>
        /// Executes a GPU operation with proper error handling and fallback options.
        /// </summary>
        /// <param name="heightMap">The 2D array containing height values to modify</param>
        /// <param name="width">Width of the heightmap</param>
        /// <param name="height">Height of the heightmap</param>
        /// <param name="shouldModify">Function determining which areas can be modified</param>
        /// <param name="gpuOperation">The GPU operation to execute</param>
        /// <returns>True if GPU operation completed successfully, false if CPU fallback should be used</returns>
        protected bool ExecuteGpuOperation(float[,] heightMap, int width, int height, 
                                      Func<int, int, bool> shouldModify, 
                                      Action<int, int, Texture2D> gpuOperation)
        {
            try
            {
                if (!PrepareForGpuOperation(width, height))
                    return false;
                
                UpdateHeightMapTexture(heightMap, width, height); // Convert heightMap to RenderTexture
                
                Texture2D maskTexture = CreateMaskTexture(shouldModify, width, height); // Create mask texture
                
                gpuOperation(width, height, maskTexture); // Execute the provided GPU operation
                
                ReadHeightMapFromGPU(heightMap, width, height); // Read back the heightmap data
                
                UnityEngine.Object.DestroyImmediate(maskTexture); // Clean up
                
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"Error in GPU operation: {e.Message}. Falling back to CPU implementation");
                return false;
            }
        }
        #endregion

        #region Disposing
        /// <summary>
        /// Releases all GPU resources used by the operation.
        /// </summary>
        public virtual void Dispose()
        {
            if (HeightMapRT != null)
            {
                HeightMapRT.Release();
                HeightMapRT = null;
            }
            
            if (OriginalHeightMapRT != null)
            {
                OriginalHeightMapRT.Release();
                OriginalHeightMapRT = null;
            }
            
            Initialized = false;
        }
        
        /// <summary>
        /// Finalizer that ensures GPU resources are properly released if Dispose isn't called.
        /// </summary>
        ~GpuTerrainOperation()
        {
            Dispose();
        }
        #endregion
    }
}