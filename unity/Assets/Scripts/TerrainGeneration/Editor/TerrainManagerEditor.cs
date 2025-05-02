using UnityEngine;
using UnityEditor;
using TerrainGeneration.Generators;
using TerrainGeneration.Modifiers.Smoothers;
using TerrainGeneration.Modifiers.Erosion;
using TerrainGeneration.Modifiers.HeightAdjusters;
using TerrainGeneration.Core;

namespace TerrainGeneration.Editor
{
    /// <summary>
    /// Custom editor for TerrainManager that provides a user interface to control terrain operation algorithms.
    /// </summary>
    [CustomEditor(typeof(TerrainManager))]
    public class TerrainManagerEditor : UnityEditor.Editor
    {
        #region Properties & Fields
        // Common settings properties
        private SerializedProperty _terrainProp;
        private SerializedProperty _maskProp;
        private SerializedProperty _restoreTerrainProp;
        private SerializedProperty _useDeterministicGenerationProp;
        
        // Generator instances
        private readonly PerlinNoiseGenerator _perlinGenerator = new();
        private readonly VoronoiGenerator _voronoiGenerator = new();
        private readonly MidpointDisplacementGenerator _midpointDisplacementGenerator = new();
        
        // Modifier instances
        private readonly BasicSmoother _basicSmoother = new();
        private readonly EnhancedDistanceSmoother _enhancedDistanceSmoother = new();
        private readonly DistanceBasedHeightScaler _distanceBasedHeightScaler = new();
        private readonly HydraulicErosion _hydraulicErosion = new();
        private readonly ThermalErosion _thermalErosion = new();
        private readonly UniformHeightModifier _uniformModifier = new();
        
        #endregion

        #region Editor State
        
        private TerrainManager _terrainManager;
        
        // Foldout states
        private bool _showCommonSettings = true;
        private bool _showPerlinGenerator;
        private bool _showVoronoiGenerator;
        private bool _showMidpointDisplacementGenerator;
        private bool _showHeightAdjusters;
        private bool _showSmoothing;
        private bool _showErosion;
        
        #endregion

        #region Unity Methods
        
        /// <summary>
        /// Initializes the editor when it becomes enabled.
        /// Retrieves a reference to the TerrainManager and its serialized properties.
        /// </summary>
        private void OnEnable()
        {
            // Get a reference to the TerrainManager
            _terrainManager = (TerrainManager)target;
            
            // Find serialized properties for common settings
            _terrainProp = serializedObject.FindProperty("terrain");
            _maskProp = serializedObject.FindProperty("mask");
            _restoreTerrainProp = serializedObject.FindProperty("restoreTerrain");
            _useDeterministicGenerationProp = serializedObject.FindProperty("useDeterministicGeneration");
        }
        
        /// <summary>
        /// Draws the custom inspector GUI for the TerrainManager.
        /// </summary>
        public override void OnInspectorGUI()
        {
            // Update the serializedObject representation
            serializedObject.Update();
            
            DrawCommonSettings();
            
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            
            // Individual generators
            DrawPerlinGenerator();
            DrawVoronoiGenerator();
            DrawMidpointDisplacementGenerator();
            
            //Modifiers
            DrawHeightAdjusters(); 
            DrawSmoothing(); 
            DrawErosion(); 
            
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            
            // Restore terrain button
            if (GUILayout.Button("Restore Terrain"))
            {
                _terrainManager.RestoreTerrain();
            }
            
            // Apply modifications to the serializedObject
            serializedObject.ApplyModifiedProperties();
        }
        
        #endregion

        #region Drawing Methods
        
        private void DrawCommonSettings()
        {
            _showCommonSettings = EditorGUILayout.Foldout(_showCommonSettings, "Common Settings");

            if (!_showCommonSettings) return;
            EditorGUILayout.PropertyField(_terrainProp, new GUIContent("Terrain"));
            EditorGUILayout.PropertyField(_maskProp, new GUIContent("Mask Texture"));
            EditorGUILayout.PropertyField(_restoreTerrainProp, new GUIContent("Reset Before Generating"));
            EditorGUILayout.PropertyField(_useDeterministicGenerationProp, new GUIContent("Use Deterministic Generation"));
        }
        
        private void DrawPerlinGenerator()
        {
            _showPerlinGenerator = EditorGUILayout.Foldout(_showPerlinGenerator, "Perlin Noise");

            if (!_showPerlinGenerator) return;
            EditorGUILayout.HelpBox("Runs Perlin Noise with Fractional Brownian Motion", MessageType.Info);
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
                
            // Perlin noise parameters
            _perlinGenerator.Frequency = EditorGUILayout.Slider(
                "Frequency",
                _perlinGenerator.Frequency,
                0f, 0.1f
            );
            
            _perlinGenerator.Octaves = EditorGUILayout.IntSlider(
                "Octaves",
                _perlinGenerator.Octaves,
                1, 10
            );
                
            _perlinGenerator.Persistence = EditorGUILayout.Slider(
                "Persistence",
                _perlinGenerator.Persistence,
                0.01f, 1f
            );
                
            _perlinGenerator.Amplitude = EditorGUILayout.Slider(
                "Amplitude",
                _perlinGenerator.Amplitude,
                0f, 1f
            );
                
            if (GUILayout.Button("Apply Perlin Noise"))
            {
                _perlinGenerator.UseDeterministicSeed = _terrainManager.UseDeterministicGeneration;
                Undo.RegisterCompleteObjectUndo(_terrainManager.terrain.terrainData, "Apply Perlin Noise");
                _terrainManager.ApplyOperation(_perlinGenerator);
            }
        }
        
        private void DrawVoronoiGenerator()
        {
            _showVoronoiGenerator = EditorGUILayout.Foldout(_showVoronoiGenerator, "Voronoi Tessellation");

            if (!_showVoronoiGenerator) return;
            EditorGUILayout.HelpBox("Creates peaks using Voronoi Diagrams", MessageType.Info);
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
                
            // Voronoi parameters
            _voronoiGenerator.PeakCount = EditorGUILayout.IntSlider(
                "Peak Count",
                _voronoiGenerator.PeakCount,
                1, 30
            );
                
            _voronoiGenerator.FallRate = EditorGUILayout.Slider(
                "Fall Rate",
                _voronoiGenerator.FallRate,
                0f, 10f
            );
                
            _voronoiGenerator.DropOff = EditorGUILayout.Slider(
                "Drop Off",
                _voronoiGenerator.DropOff,
                0f, 10f
            );
                
            _voronoiGenerator.MinHeight = EditorGUILayout.Slider(
                "Min Height",
                _voronoiGenerator.MinHeight,
                0f, 1f
            );
                
            _voronoiGenerator.MaxHeight = EditorGUILayout.Slider(
                "Max Height",
                _voronoiGenerator.MaxHeight,
                0f, 1f
            );
            
            _voronoiGenerator.AvoidConstrainedPoints = EditorGUILayout.Toggle(
                "Avoid Constrained Points",
                _voronoiGenerator.AvoidConstrainedPoints
            );

            if (_voronoiGenerator.AvoidConstrainedPoints)
            {
                EditorGUILayout.HelpBox("Hills will be generated only in areas that won't affect constrained points. This may result in fewer hills than requested if suitable locations cannot be found.", MessageType.Info);
    
                _voronoiGenerator.MaxPlacementAttempts = EditorGUILayout.IntSlider(
                    "Max Placement Attempts",
                    _voronoiGenerator.MaxPlacementAttempts,
                    10, 200
                );
            }
            
            if (GUILayout.Button("Apply Voronoi"))
            {
                _voronoiGenerator.UseDeterministicSeed = _terrainManager.UseDeterministicGeneration;
                Undo.RegisterCompleteObjectUndo(_terrainManager.terrain.terrainData, "Apply Voronoi");
                _terrainManager.ApplyOperation(_voronoiGenerator);
            }
        }
        
        private void DrawMidpointDisplacementGenerator()
        {
            _showMidpointDisplacementGenerator = EditorGUILayout.Foldout(_showMidpointDisplacementGenerator, "Midpoint Displacement");

            if (!_showMidpointDisplacementGenerator) return;
            EditorGUILayout.HelpBox("Runs the Diamond-Square procedural terrain generation algorithm", MessageType.Info);
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

            // Midpoint displacement parameters
            _midpointDisplacementGenerator.MinHeight = EditorGUILayout.Slider(
                "Min Height", 
                _midpointDisplacementGenerator.MinHeight,
                0.0f, 1.0f
            );

            _midpointDisplacementGenerator.MaxHeight = EditorGUILayout.Slider(
                "Max Height", 
                _midpointDisplacementGenerator.MaxHeight,
                0.0f, 1.0f
            );

            _midpointDisplacementGenerator.Smoothness = EditorGUILayout.Slider(
                "Smoothness", 
                _midpointDisplacementGenerator.Smoothness, 
                0.1f, 1.0f
            );

            _midpointDisplacementGenerator.InitialRandomRange = EditorGUILayout.Slider(
                "Initial Random Range", 
                _midpointDisplacementGenerator.InitialRandomRange, 
                0.0f, 1.0f
            );

            if (GUILayout.Button("Apply Midpoint Displacement"))
            {
                _midpointDisplacementGenerator.UseDeterministicSeed = _terrainManager.UseDeterministicGeneration;
                Undo.RegisterCompleteObjectUndo(_terrainManager.terrain.terrainData, "Apply Midpoint Displacement");
                _terrainManager.ApplyOperation(_midpointDisplacementGenerator);
            }
        }

        private void DrawHeightAdjusters()
        {
            _showHeightAdjusters = EditorGUILayout.Foldout(_showHeightAdjusters, "Height Adjusters");

            if (!_showHeightAdjusters) return;
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
                
            // Distance-Based Height Scaler
            EditorGUILayout.LabelField("Distance-Based Height Scaler", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Scales terrain heights based on distance from prohibited areas.", MessageType.Info);

            _distanceBasedHeightScaler.MaxScaleFactor = EditorGUILayout.Slider(
                "Max Scale Factor",
                _distanceBasedHeightScaler.MaxScaleFactor,
                0f, 1f
            );

            _distanceBasedHeightScaler.ReferenceMethod = (ReferenceHeightMethod)EditorGUILayout.EnumPopup(
                "Reference Height Method",
                _distanceBasedHeightScaler.ReferenceMethod
            );

            if (GUILayout.Button("Apply Height Scaling"))
            {
                Undo.RegisterCompleteObjectUndo(_terrainManager.terrain.terrainData, "Apply Distance-Based Height Scaling");
                _terrainManager.ApplyOperation(_distanceBasedHeightScaler);
            }
            
            EditorGUILayout.Space(10);
            
            EditorGUILayout.HelpBox("Changes height of modifiable area uniformly", MessageType.Info);
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            
            _uniformModifier.AdjustmentMethod = (HeightAdjustmentMethod)EditorGUILayout.EnumPopup(
                "Height Adjustment Method",
                _uniformModifier.AdjustmentMethod
            );
            
            switch (_uniformModifier.AdjustmentMethod)
            {
                case HeightAdjustmentMethod.RelativeAdjustment:
                    EditorGUILayout.HelpBox("Adds uniform step across the terrain", MessageType.Info);
                    _uniformModifier.UniformStep = EditorGUILayout.Slider(
                        "Height Step",
                        _uniformModifier.UniformStep,
                        -1f, 1f
                    );
                    break;
                case HeightAdjustmentMethod.NormalizeHeight:
                    EditorGUILayout.HelpBox("Subtracts lowest modifiable points height from across the terrain", MessageType.Info);
                    break;
                case HeightAdjustmentMethod.ReferenceHeight:
                    EditorGUILayout.HelpBox("Subtracts the chosen reference height of the fixed points across the terrain", MessageType.Info);
                    _uniformModifier.ReferenceMethod = (ReferenceHeightMethod)EditorGUILayout.EnumPopup(
                        "Reference Height Method",
                        _uniformModifier.ReferenceMethod
                    );
                    break;
            }

            EditorGUILayout.BeginHorizontal();
        
            if (GUILayout.Button("Apply Uniform Height"))
            {
                // Register the operation for undo
                Undo.RegisterCompleteObjectUndo(_terrainManager.terrain.terrainData, "Apply Uniform Height");
                _terrainManager.ApplyOperation(_uniformModifier);
            }
            EditorGUILayout.EndHorizontal();
            
            
        }
        
        private void DrawSmoothing()
        {
            _showSmoothing = EditorGUILayout.Foldout(_showSmoothing, "Smoothing");

            if (!_showSmoothing) return;
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
                
            // Basic smoother & its parameter
            EditorGUILayout.LabelField("Basic Smoother", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Smoothes terrain uniformly.", MessageType.Info);
                
            _basicSmoother.Iterations = EditorGUILayout.IntSlider(
                "Iterations",
                _basicSmoother.Iterations,
                1, 100
            );
                
            if (GUILayout.Button("Apply Basic Smoothing"))
            {
                Undo.RegisterCompleteObjectUndo(_terrainManager.terrain.terrainData, "Apply Basic Smoothing");
                _terrainManager.ApplyOperation(_basicSmoother);
            }
            
            EditorGUILayout.Space(10);
            
            // Distance based smoother and its parameters
            EditorGUILayout.LabelField("Enhanced Distance Smoother", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Smoothes terrain based on distance from prohibited areas", MessageType.Info);
            
            _enhancedDistanceSmoother.Iterations = EditorGUILayout.IntSlider(
                "Iterations",
                _enhancedDistanceSmoother.Iterations,
                1, 100
            );
                
            _enhancedDistanceSmoother.BaseSmoothing = EditorGUILayout.Slider(
                "Base Smoothing",
                _enhancedDistanceSmoother.BaseSmoothing,
                0f, 10f
            );
                
            _enhancedDistanceSmoother.ConstrainedHeightProximityWeight = EditorGUILayout.Slider(
                "Constrained Height Proximity Weight",
                _enhancedDistanceSmoother.ConstrainedHeightProximityWeight,
                1f, 10f
            );
            
            if (GUILayout.Button("Apply Enhanced Distance Smoothing"))
            {
                Undo.RegisterCompleteObjectUndo(_terrainManager.terrain.terrainData, "Apply Enhanced Distance Smoothing");
                _terrainManager.ApplyOperation(_enhancedDistanceSmoother);
            }
        }
        
        private void DrawErosion()
        {
            _showErosion = EditorGUILayout.Foldout(_showErosion, "Erosion");

            if (!_showErosion) return;
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
                
            // Hydraulic erosion & its parameters
            EditorGUILayout.LabelField("Hydraulic Erosion", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Simulates rain over the terrain", MessageType.Info);

            EditorGUILayout.LabelField("Parameters", EditorStyles.boldLabel);

            _hydraulicErosion.DropletCount = EditorGUILayout.IntSlider(
                "Droplet Count",
                _hydraulicErosion.DropletCount,
                100, 20000
            );

            _hydraulicErosion.ErosionStrength = EditorGUILayout.Slider(
                "Erosion Strength",
                _hydraulicErosion.ErosionStrength,
                0.01f, 1.0f
            );

            _hydraulicErosion.SpringsPerDroplet = EditorGUILayout.IntSlider(
                "Springs Per Droplet",
                _hydraulicErosion.SpringsPerDroplet,
                1, 10
            );

            _hydraulicErosion.Solubility = EditorGUILayout.Slider(
                "Solubility",
                _hydraulicErosion.Solubility,
                0.001f, 0.1f
            );

            if (GUILayout.Button("Apply Hydraulic Erosion"))
            {
                _hydraulicErosion.UseDeterministicSeed = _terrainManager.UseDeterministicGeneration;
                Undo.RegisterCompleteObjectUndo(_terrainManager.terrain.terrainData, "Apply Hydraulic Erosion");
                _terrainManager.ApplyOperation(_hydraulicErosion);
            }

            EditorGUILayout.Space(10);
                
            // Thermal erosion & its parameters
            EditorGUILayout.LabelField("Thermal Erosion", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Simulates material slumping on steep slopes to create more natural terrain gradients.", MessageType.Info);
    
            _thermalErosion.Iterations = EditorGUILayout.IntSlider(
                "Iterations",
                _thermalErosion.Iterations,
                1, 100
            );
    
            _thermalErosion.ErosionThreshold = EditorGUILayout.Slider(
                "Erosion Threshold",
                _thermalErosion.ErosionThreshold,
                0.001f, 0.1f
            );
    
            EditorGUILayout.HelpBox("Making this value too high can cause problems, it is recommended to keep it below 0.2f", MessageType.Info);
            _thermalErosion.ErosionRate = EditorGUILayout.Slider(
                "Erosion Rate",
                _thermalErosion.ErosionRate,
                0.0f, 1.0f
            );
    
            if (GUILayout.Button("Apply Thermal Erosion"))
            {
                Undo.RegisterCompleteObjectUndo(_terrainManager.terrain.terrainData, "Apply Thermal Erosion");
                _terrainManager.ApplyOperation(_thermalErosion);
            }
        }
        
        #endregion
     }
}