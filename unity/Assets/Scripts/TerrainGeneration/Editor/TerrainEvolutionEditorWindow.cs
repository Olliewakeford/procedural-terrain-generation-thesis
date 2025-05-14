using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using TerrainGeneration.Core;
using TerrainGeneration.InteractiveGeneticAlgorithm;

namespace TerrainGeneration.Editor
{
    /// <summary>
    /// Editor window for interactive evolution of terrain using genetic algorithms.
    /// Allows users to view, select, and evolve terrain variants to find optimal terrain generation parameters.
    /// </summary>
    public class TerrainEvolutionEditorWindow : EditorWindow
    {
        #region Fields
        private TerrainManager _terrainManager;
        private TerrainEvolutionManager _evolutionManager;
        private TerrainPreviewGenerator _previewGenerator;
        private Camera _previewCamera;
        
        // Preview state
        private List<Texture2D> _previewTextures = new();
        private List<bool> _selectedTerrains = new();
        private bool _previewsGenerated;
        
        // UI layout
        private Vector2 _scrollPosition;
        private const int PreviewSize = 275;

        // UI state
        private bool _isGeneratingPreviews;
        private bool _hasInitializedPopulation;
        
        // Evolution settings
        private int _populationSize = 8;
        private int _selectionCount = 3;
        private float _mutationRate = 0.1f;
        private float _crossoverRate = 0.7f;
        
        // Track the favourite terrain for elitism
        private int _favouriteTerrainIndex = -1; // -1 means no favourite selected
        #endregion
        
        #region Public Methods
        /// <summary>
        /// Opens the Terrain Evolution window in the Unity Editor.
        /// Creates and configures the window with proper size and title.
        /// </summary>
        [MenuItem("Tools/Terrain/Interactive Evolution")]
        public static void ShowWindow()
        {
            TerrainEvolutionEditorWindow window = GetWindow<TerrainEvolutionEditorWindow>();
            window.titleContent = new GUIContent("Terrain Evolution");
            window.minSize = new Vector2(450, 600);
            window.Show();
        }
        #endregion
        
        #region Unity Methods
        private void OnEnable()
        {
            _terrainManager = FindAnyObjectByType<TerrainManager>(); // Find the TerrainManager in the scene
            
            if (_terrainManager == null)
            {
                Debug.LogError("No TerrainManager found, please add one as a component to the terrain object");
                return;
            }
            
            _evolutionManager = FindAnyObjectByType<TerrainEvolutionManager>(); // Find the TerrainEvolutionManager
            
            // Read current values from the manager
            if (_evolutionManager != null) 
            {
                _populationSize = _evolutionManager.PopulationSize;
                _mutationRate = _evolutionManager.MutationProbability;
                _crossoverRate = _evolutionManager.CrossoverProbability;
            }
            
            // Create a temporary camera for previews
            if (_previewCamera == null)
            {
                GameObject cameraObject = new GameObject("Preview Camera")
                {
                    hideFlags = HideFlags.HideAndDontSave
                };
                _previewCamera = cameraObject.AddComponent<Camera>();
                _previewCamera.enabled = false;
            }
            
            _previewGenerator = new TerrainPreviewGenerator(_terrainManager, _previewCamera);
            
            _previewGenerator.CaptureOriginalHeights(); // Capture the initial terrain state
            
            _previewTextures = new List<Texture2D>();
            _selectedTerrains = new List<bool>();
        }
        
        private void OnDisable()
        {
            // Clean up preview textures
            if (_previewTextures != null)
            {
                foreach (Texture2D texture in _previewTextures)
                {
                    if (texture != null)
                    {
                        DestroyImmediate(texture);
                    }
                }
                _previewTextures.Clear();
            }
        }
        
        private void OnGUI()
        {
            if (_terrainManager == null || _evolutionManager == null)
            {
                EditorGUILayout.HelpBox("TerrainManager not found in the scene", MessageType.Error);
                return;
            }
            
            EditorGUILayout.BeginVertical();
            
            DrawHeader();
            
            if (!_hasInitializedPopulation)
            {
                DrawEvolutionSettings();
            }
            
            EditorGUILayout.Space(10);
            DrawPreviewGrid();
            
            EditorGUILayout.Space(10);
            DrawActionButtons(); 
            
            EditorGUILayout.EndVertical();
        }
        #endregion
        
        #region Draw methods
        
        // Draws the header section with evolution info and controls.
        private void DrawHeader()
        {
            // Title and generation info
            EditorGUILayout.BeginHorizontal();
            
            EditorGUILayout.LabelField(
                $"Terrain Evolution - Generation {_evolutionManager.CurrentGeneration}",
                EditorStyles.boldLabel
            );
            
            GUILayout.FlexibleSpace();
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(5);
            
            // Instructions
            if (_hasInitializedPopulation)
            {
                int selectedCount = GetSelectedCount();
                bool hasFavourite = _favouriteTerrainIndex >= 0 && _selectedTerrains[_favouriteTerrainIndex];
    
                EditorGUILayout.LabelField(
                    $"Select your {_selectionCount} preferred terrains ({selectedCount}/{_selectionCount} selected):",
                    EditorStyles.boldLabel
                );

                EditorGUILayout.HelpBox(
                    hasFavourite
                        ? $"Terrain #{_favouriteTerrainIndex} is marked as favourite and will be preserved as elite in the next generation."
                        : "Click 'Favourite' on one of your selected terrains to mark it as elite for the next generation.",
                    MessageType.Info);
            }
            
            else
            {
                EditorGUILayout.HelpBox(
                    "Configure evolution settings and click 'Initialize Population' to start the evolution process.",
                    MessageType.Info
                );
            }
        }
        
        // Draws the evolution settings UI with sliders for population parameters.
        private void DrawEvolutionSettings()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            EditorGUILayout.LabelField("Evolution Settings", EditorStyles.boldLabel);
            
            EditorGUI.BeginChangeCheck();
            _populationSize = EditorGUILayout.IntSlider("Population Size", _populationSize, 8, 50);
            
            _selectionCount = EditorGUILayout.IntSlider("Selection Count", _selectionCount, 1, 6);
            
            _mutationRate = EditorGUILayout.Slider("Mutation Rate", _mutationRate, 0f, 1f);
            
            _crossoverRate = EditorGUILayout.Slider("Crossover Rate", _crossoverRate, 0f, 1f);
            
            EditorGUILayout.EndVertical();
        }
        
        // Draws the grid of terrain previews
        private void DrawPreviewGrid()
        {
            // If previews haven't been generated yet, show a message
            if (!_hasInitializedPopulation || !_previewsGenerated)
            {
                EditorGUILayout.HelpBox(
                    "No terrain variants have been generated yet. Click 'Initialize Population' to start.",
                    MessageType.Info
                );
                return;
            }
            
            // If we're currently generating previews, show a progress message
            if (_isGeneratingPreviews)
            {
                EditorGUILayout.HelpBox(
                    "Generating terrain previews...",
                    MessageType.Info
                );
                return;
            }
            
            // Draw the preview grid
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            
            int count = _previewTextures.Count;
            
            int gridColumns = CalculateGridColumns();

            for (int i = 0; i < count; i += gridColumns)
            {
                EditorGUILayout.BeginHorizontal();
    
                for (int j = 0; j < gridColumns && (i + j) < count; j++)
                {
                    int index = i + j;
                    DrawTerrainPreview(index);
                }
    
                EditorGUILayout.EndHorizontal();
            }
            
            EditorGUILayout.EndScrollView();
        }
        
        // Draws an individual terrain preview with selection controls.
        private void DrawTerrainPreview(int index)
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(PreviewSize + 10));
            
            // Preview box
            Rect previewRect = GUILayoutUtility.GetRect(PreviewSize, PreviewSize);
            
            if (_previewTextures[index] != null)
            {
                // Draw the preview
                GUI.DrawTexture(previewRect, _previewTextures[index]);
                
                // Draw selection border
                if (_selectedTerrains[index])
                {
                    Color originalColor = GUI.color;
                    GUI.color = Color.green;
                    GUI.Box(previewRect, GUIContent.none);
                    GUI.color = originalColor;
                }
                
                // Draw favourite indicator (colour preview red)
                if (_favouriteTerrainIndex == index)
                {
                    Color originalColor = GUI.color;
                    GUI.color = Color.red;
                    
                    // Also draw a thin red border
                    GUI.Box(previewRect, GUIContent.none);
                    GUI.color = originalColor;
                }
                
                // Draw the genome name
                TerrainGenome genome = _evolutionManager.GetIndividual(index);
                if (genome != null)
                {
                    Rect labelRect = new Rect(previewRect.x, previewRect.y, previewRect.width, 20);
                    GUI.color = new Color(0, 0, 0, 0.7f);
                    GUI.DrawTexture(labelRect, EditorGUIUtility.whiteTexture);
                    GUI.color = Color.white;
                    GUI.Label(labelRect, $" {genome.name}", EditorStyles.boldLabel);
                }
            }
            else
            {
                GUI.Box(previewRect, "No Preview");
            }
            
            // Selection controls
            EditorGUILayout.BeginHorizontal();
            
            // Selection toggle
            bool selected = _selectedTerrains[index];
            bool newSelected = EditorGUILayout.Toggle("Select", selected);
            
            if (newSelected != selected)
            {
                // Check if we already have right amount selected and we're trying to select another
                if (newSelected && GetSelectedCount() >= _selectionCount)
                {
                    EditorUtility.DisplayDialog(
                        "Selection Limit",
                        $"You can only select up to {_selectionCount} terrains.",
                        "OK"
                    );
                }
                else
                {
                    _selectedTerrains[index] = newSelected;
                }
            }
            
            // favourite button
            GUI.backgroundColor = (_favouriteTerrainIndex == index) ? Color.red : Color.white;
            if (GUILayout.Button("Favourite", GUILayout.Width(70)))
            {
                // Set this as the favourite, replacing any previous favourite
                _favouriteTerrainIndex = (_favouriteTerrainIndex == index) ? -1 : index;
            }
            GUI.backgroundColor = Color.white;
            
            // Apply button
            if (GUILayout.Button("Apply", GUILayout.Width(60)))
            {
                // Apply this terrain
                ApplyTerrain(index);
            }
            
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }
        
        
        // Draws the action buttons for evolution control based on current state.
        private void DrawActionButtons()
        {
            EditorGUILayout.BeginHorizontal();
    
            if (!_hasInitializedPopulation)
            {
                // Initialize button
                if (GUILayout.Button("Initialize Population", GUILayout.Height(30)))
                {
                    InitializePopulation();
                }
            }
            else
            {
                // Check if we have enough selections and a favourite is selected
                bool canEvolve = GetSelectedCount() == _selectionCount;
                bool hasFavourite = _favouriteTerrainIndex >= 0 && _selectedTerrains[_favouriteTerrainIndex];
        
                // Evolve button - enabled only if we have enough selections and a favourite is marked
                string evolveButtonText = hasFavourite 
                    ? "Evolve Next Generation" 
                    : "Select a Favourite Terrain First";
            
                EditorGUI.BeginDisabledGroup(!canEvolve || !hasFavourite);
                if (GUILayout.Button(evolveButtonText, GUILayout.Height(30)))
                {
                    EvolveNextGeneration();
                }
                EditorGUI.EndDisabledGroup();
        
                // Apply Selected button
                EditorGUI.BeginDisabledGroup(GetSelectedCount() != 1);
                if (GUILayout.Button("Apply Selected", GUILayout.Height(30)))
                {
                    ApplySelectedTerrain();
                }
                EditorGUI.EndDisabledGroup();
            }
    
            EditorGUILayout.EndHorizontal();
        }
        #endregion
        
        #region Private Methods
        /// <summary>
        /// Calculates the number of preview columns that can fit in the current window width.
        /// </summary>
        /// <returns>The number of columns that fit, minimum of 1</returns>
        private int CalculateGridColumns()
        {
            // Get the current window width
            float windowWidth = position.width;
    
            // Account for padding, scrollbar, and margins (roughly 40 pixels total)
            float availableWidth = windowWidth - 40f;
    
            // Each preview takes PreviewSize + 10 pixels for spacing
            float previewWidthWithSpacing = PreviewSize + 10f;
    
            // Calculate how many columns fit
            int columns = Mathf.FloorToInt(availableWidth / previewWidthWithSpacing);
    
            // Ensure at least 1 column
            return Mathf.Max(1, columns);
        }
        
        /// <summary>
        /// Counts the number of selected terrains in the current population.
        /// </summary>
        /// <returns>The count of selected terrains</returns>
        private int GetSelectedCount()
        {
            int count = 0;
            foreach (bool selected in _selectedTerrains)
            {
                if (selected) count++;
            }
            return count;
        }
        
        // Initializes the terrain population and generates previews.
        private void InitializePopulation()
        {
            // Apply settings to the evolution manager
            _evolutionManager.PopulationSize = _populationSize;
            _evolutionManager.MutationProbability = _mutationRate;
            _evolutionManager.CrossoverProbability = _crossoverRate;
    
            // Initialize the population
            _evolutionManager.InitializePopulation();
            _hasInitializedPopulation = true;
    
            // Generate previews
            RegenerateAllPreviews();
        }
        
        // Evolves the next generation based on selected terrains and updates previews.
        private void EvolveNextGeneration()
        {
            // Collect selected indices
            List<int> selectedIndices = new List<int>();
    
            // mMake the favourite terrain first in the list
            if (_favouriteTerrainIndex >= 0 && _selectedTerrains[_favouriteTerrainIndex])
            {
                selectedIndices.Add(_favouriteTerrainIndex);
            }
    
            // Add remaining selected terrains
            for (int i = 0; i < _selectedTerrains.Count; i++)
            {
                if (_selectedTerrains[i] && i != _favouriteTerrainIndex)
                {
                    selectedIndices.Add(i);
                }
            }
    
            // Set selected indices and evolve
            _evolutionManager.SetSelectedIndices(selectedIndices);
            _evolutionManager.EvolveNextGeneration();
    
            // Reset favourite for the next generation
            _favouriteTerrainIndex = -1;
    
            // Generate previews for the new generation
            RegenerateAllPreviews();
        }
        
        // Applies the currently selected terrain to the scene.
        private void ApplySelectedTerrain()
        {
            // Find the selected terrain
            for (int i = 0; i < _selectedTerrains.Count; i++)
            {
                if (_selectedTerrains[i])
                {
                    ApplyTerrain(i);
                    break;
                }
            }
        }
        
        private void ApplyTerrain(int index)
        {
            _evolutionManager.ApplyIndividual(index);
        }
        
        // Regenerates all preview images for the current population.
        private void RegenerateAllPreviews()
        {
            // Clear existing previews
            foreach (Texture2D texture in _previewTextures)
            {
                if (texture != null)
                {
                    DestroyImmediate(texture);
                }
            }
            
            _previewTextures.Clear();
            _selectedTerrains.Clear();
            
            // Start asynchronous preview generation
            _isGeneratingPreviews = true;
            _previewsGenerated = false;
            
            EditorApplication.delayCall += GeneratePreviewsAsync;
        }
        
        // Generates previews asynchronously (This is to avoid UI freezing problems)
        private void GeneratePreviewsAsync()
        {
            // Capture original heights before modifying the terrain
            _previewGenerator.CaptureOriginalHeights();
            
            int populationCount = _evolutionManager.PopulationCount;
            
            // Generate previews for each individual
            for (int i = 0; i < populationCount; i++)
            {
                TerrainGenome genome = _evolutionManager.GetIndividual(i);
                if (genome != null)
                {
                    Texture2D preview = _previewGenerator.GeneratePreview(genome);
                    _previewTextures.Add(preview);
                    _selectedTerrains.Add(false);
                }
                
                // Update the progress bar
                float progress = (i + 1) / (float)populationCount;
                if (EditorUtility.DisplayCancelableProgressBar(
                    "Generating Previews",
                    $"Generating preview {i + 1}/{populationCount}",
                    progress))
                {
                    // User cancelled
                    break;
                }
            }
            
            EditorUtility.ClearProgressBar();
            
            _previewGenerator.RestoreOriginalHeights();
            
            // Update state
            _isGeneratingPreviews = false;
            _previewsGenerated = true;
            
            // Repaint the window
            Repaint();
        }
        #endregion
    }
}