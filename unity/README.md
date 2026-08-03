# Constrained Procedural Terrain Generation

This repository contains a Unity-based system for procedural terrain generation with fixed height constraints. The system is designed to generate natural-looking terrain that respects areas where heights cannot be modified, creating seamless transitions between fixed and procedurally generated areas.

## Table of Contents
- [Features](#features)
- [Project Structure](#project-structure)
- [Installation](#installation)
  - [Option 1: Full Project](#option-1-full-project)
  - [Option 2: Import into Existing Project](#option-2-import-into-existing-project)
- [Setting Up a Constrained Terrain](#setting-up-a-constrained-terrain)
- [Using the Terrain Manager](#using-the-terrain-manager)
- [Using the Interactive Genetic Algorithm](#using-the-interactive-genetic-algorithm)
- [Operator and Parameter Details](#Operator-and-Parameter-Details)

## Features

- **Custom Terrain Generation** Use the Unity Editor to run various terrain generating algorithms and modifiers, where parameters can be adjusted as desired
- **Interactive Evolution**: Use a genetic algorithm to evolve terrain parameters and terrain operations based on user preferences
- **GPU Acceleration**: Long operations are GPU-accelerated for better performance, with CPU fallbacks
- **Constraint Handling**: Respects fixed height constraints while generating natural-looking terrain

## Project Structure
The system is organized into several key components:

- **Core**: Core classes and interfaces for terrain management
  - `TerrainManager.cs`: Central component for terrain operations
  - `DistanceGridManager.cs`: Manages distance calculations from fixed points
  - `Interfaces.cs`: Defines interfaces for terrain operations

- **Generators**: Terrain generation algorithms
  - `PerlinNoiseGenerator.cs`: Generates terrain using Perlin noise
  - `VoronoiGenerator.cs`: Creates mountainous terrain using Voronoi diagrams
  - `MidpointDisplacementGenerator.cs`: Implements diamond-square terrain generation

- **Modifiers**: Terrain modification algorithms
  - **Smoothers**: Algorithms for smoothing terrain
  - **Erosion**: Simulates natural erosion processes
  - **HeightAdjusters**: Adjusts terrain heights based on various criteria, including
    `UniformHeightModifier.cs` (uniform height changes) and `DistanceBasedHeightScaler.cs`

- **InteractiveGeneticAlgorithm**: Components for interactive evolution
  - `TerrainEvolutionManager.cs`: Manages evolution of terrain parameters
  - `TerrainGenome.cs`: Represents a set of terrain operations as a genome
  - `TerrainPreviewGenerator.cs`: Generates preview images for terrain variants

- **Editor**: Custom Unity editor components
  - `TerrainManagerEditor.cs`: Custom inspector for TerrainManager
  - `TerrainEvolutionEditorWindow.cs`: Editor window for the interactive genetic algorithm

- **Resources/Shaders**: Compute shaders for GPU-accelerated operations
  - `BasicSmoother.compute`: GPU implementation of basic smoothing
  - `EnhancedDistanceSmoother.compute`: GPU implementation of distance-based smoothing
  - `ThermalErosion.compute`: GPU implementation of thermal erosion


## Installation

### Requirements

- Unity 6 or newer

### Clone the Repository

### Option 1: Full Project

1. Clone this repository or download a .zip file of it:
   
   HTTPs:
   ```
   git clone https://github.com/Olliewakeford/Procedural-Terrain-Generation-with-fixed-points.git
   ```
   SSH:
   ```
   git git@github.com:Olliewakeford/Procedural-Terrain-Generation-with-fixed-points.git
   ```

2. Open the project in Unity:
   - Launch Unity Hub
   - Click "Add" -> "Add project from Disk", find the clone directory
   - Select the folder of this repository
   - Open the project

The project will be set up with various test data, terrains, masks, and everything ready to go.

### Option 2: Import into Existing Project

1. Download this repository or clone it to a temporary location.

2. Create the following folders in your Unity project if they don't already exist:
   - `Assets/Scripts/TerrainGeneration`
   - `Assets/Resources/Shaders`

3. Copy the following directories from the downloaded repository to your project:
   - Copy `Assets/Scripts/TerrainGeneration/Core` to your project's `Assets/Scripts/TerrainGeneration/Core`
   - Copy `Assets/Scripts/TerrainGeneration/Generators` to your project's `Assets/Scripts/TerrainGeneration/Generators`
   - Copy `Assets/Scripts/TerrainGeneration/Modifiers` to your project's `Assets/Scripts/TerrainGeneration/Modifiers`
   - Copy `Assets/Scripts/TerrainGeneration/InteractiveGeneticAlgorithm` to your project's `Assets/Scripts/TerrainGeneration/InteractiveGeneticAlgorithm`
   - Copy `Assets/Scripts/TerrainGeneration/Editor` to your project's `Assets/Scripts/TerrainGeneration/Editor`
   - Copy `Assets/Resources/Shaders/*.compute` to your project's `Assets/Resources/Shaders/`

4. **IMPORTANT**: Make sure the compute shader files are placed correctly in the `Assets/Resources/Shaders` folder with the exact same names. The system looks for these shaders at runtime using `Resources.Load()`, so the path and names must match exactly.

5. After importing the files, Unity will compile the scripts and the system should be ready to use.

6. Optional: Import sample mask textures from the `Assets/TestData` directory

## Setting Up A Constrained Terrain

1. **Create a Terrain**:
   - In the Hierarchy, go to GameObject > 3D Object > Terrain

2. **Create a Constraint Mask**:
   - Create a 2D PNG image where red pixels represent areas where terrain heights should not change and black areas represent modifiable areas. 
   - This is a birds-eye view of the cells of the terrain you dont want to edit

3. **Set the heights of the constrained areas**
   - You can set the heights of the constrained areas to your preference, they won't be edited by the scripts
   - This can be done using Unitys terrain tools, or any external system.

4. **Add the TerrainManager Component**:
   - Select your terrain in the hierarchy
   - Click "Add Component" in the Inspector
   - Search for "Terrain Manager" and add it
   - Drag your constraint mask image into the "Mask" field.
   - Ensure your mask has "Read/Write" enabled

5. **Set up the Interactive Evolution Window**:
   - Click "Add Component" in the Inspector
   - Search for "Terrain Evolution Manager" and add it
   - Drag the Terrain Manager component of the terrain into the Terrain Manager field within this component


## Using the Terrain Manager

The Terrain Manager is the core component that handles terrain operations and constraint enforcement. This component can be used to run terrain operations directly, giving full control and customisation to how the generation. In general, these operations are run as follows:

1. In the TerrainManager inspector, expand the section you want to use
2. Configure the parameters for the operation
5. Click "Apply" to apply it to the terrain

### Available Operations

The system includes various operations that can be applied to the terrain:

#### Generators

- **Perlin Noise Generator**: Creates natural-looking terrain using Perlin noise with fractal Brownian Motion
- **Voronoi Generator**: Creates mountain-like formations using Voronoi diagrams
- **Midpoint Displacement Generator**: Creates fractal terrain using midpoint displacement

#### Modifiers

- **Basic Smoother**: Simple uniform smoothing
- **Enhanced Distance Smoother**: Smoothes based on nearest distance to fixed points
- **Distance-Based Height Scaler**: Adjusts heights based on distance to fixed points
- **Uniform Height Modifier**: Applies uniform height adjustments
- **Hydraulic Erosion**: Simulates water flow over terrain
- **Thermal Erosion**: Simulates material slumping

For more information on each parameter, please see [Operator and Parameter Details](#Operator-and-Parameter-Details)

## Using the Interactive Genetic Algorithm

The Interactive Genetic Algorithm allows you to evolve terrain operations and parameters based on your aesthetic preferences. This tool does the terrain generation and parameter tuning for the user. It works using the principles of Genetic Algorithms within the field of Evolutionary Algorithms. It allows the user to generate terrains with fixed heoghts without having to manually apply the generators, modifiers or do any parameter-tuning. It works by generating initial terrains, then allowing the user to select their favourites, then evolving these over generations to end up with a terrain the user is happy with.

### Setting Up the IGA

1. Go to Tools > Terrain > Interactive Evolution in the Unity Editor
2. Configure the evolution settings:
   - **Population Size**: Number of terrain variants per generation (8-50). For more exploration, increase this number, although it will increase the time it takes to generate the terrains between generations.
   - **Selection Count**: Number of terrains to select for breeding (3-6)
   - **Mutation Rate**: Probability of mutation (0.0-1.0)
   - **Crossover Rate**: Probability of crossover (0.0-1.0)
3. Click "Initialise Population" to generate the first generation

### Using the IGA

1. The window will display a grid of terrain previews
2. Select the variants that you prefer by toggling "Select". The number needed is based on what is set in the Selection Count window in the beginning.
3. Mark one of your selected variants as a "Favourite" (this will be preserved as elite)
4. Click "Evolve Next Generation" to create a new generation based on your selections
5. Repeat the process until you find a terrain variant you like
6. Click "Apply Selected" to apply the chosen variant to your terrain

### Tips for Effective Evolution

- **Select Diverse Terrains**: Choose variants with different characteristics to explore the parameter space
- **Use the Favourite Button**: Mark your most preferred terrain as favourite to ensure it's preserved
- **Evolve Multiple Generations**: The algorithm improves over multiple generations
- **Apply Intermediate Results**: You can apply a terrain at any point without stopping the evolution process to examine it in more detail, or preserve it.
- **Use Appropriate Parameters**: The larger the population size, the more diversity you will have. We also recommend making mutation rate > 0.4 to add more variety.

## Operator and Parameter Details

### Reset Before Generating
- Under Common Settings, there is a check box which will determine whether to run the terrain operation ontop of the current terrain, or to restore the original terrain (where modifiable areas are at height 0) before running the selected option.

### Use Deterministic Generation
- Under Common Settings, there is a check box which will determine whether to run the terrain operation deterministically, so repeated runs with the same parameters will yiled the exact same results. Alternatively, unchecking this box will make the terrain operations run non-deterministically.

### Perlin Noise Generator

Creates natural-looking terrain using Perlin noise with fractional Brownian motion (fBM).

- **Frequency** (0.001-0.05): Controls the scale of the noise pattern. Lower values create larger, smoother terrain features, while higher values create more detailed, smaller features.
- **Octaves** (0.1-1.0): Number of noise layers to combine. Higher values create more detailed terrain with finer variations at different scales.
- **Persistence** (0.1-10.0): Controls how much each successive octave contributes to the final shape. Higher values create more pronounced detail and rougher terrain with stronger variation at smaller scales.
- **Amplitude** (0.01-1.0): Controls the overall height of the noise effect. Higher values create more dramatic terrain with larger height differences.

### Voronoi Generator

Creates mountain-like features using Voronoi diagrams. 

- **Peak Count** (1-100): Number of mountain/hill peaks to generate on the terrain. 
- **Fall Rate** (0.5-10.0): Controls how quickly height decreases with distance from peaks (linear component). Higher values create steeper mountains with smaller bases.
- **Drop Off** (0.5-10.0): Exponential factor controlling height falloff with distance. Higher values create more abrupt transitions near peak bases, leading to sharper, more distinct mountains.
- **Min/Max Height** (0.0-1.0): Range of possible heights for generated peaks. Determines the overall scale of the mountains in the Unity terrain height system.
- **Avoid Constrained Points** (true/false): When enabled, peaks will not be placed near constrained (prohibited) areas.
- **Max Placement Attempts** (50-200): Maximum number of attempts to place each peak when avoiding constrained areas. Higher values increase the chance of successful placement. This is necessary because if there is no valid place to place a hill of this size, we want to avoid searching forever.

### Midpoint Displacement Generator

Creates fractal terrain using the Diamond-Square (midpoint displacement) algorithm.

- **Min/Max Height** (0.0-1.0): Range of heights for the generated terrain. Controls the vertical scale of terrain features.
- **Smoothness** (0.1-1.0): Controls the roughness of the terrain. Higher values (closer to 1) create smoother terrain features with less dramatic height variations between adjacent points.
- **Initial Random Range** (0.1-1.0): Initial random displacement range applied to terrain corners. Higher values create more dramatic variation in the terrain's initial state, affecting the overall character of the generated landscape.

### Uniform Height Modifier

Applies uniform height changes to the terrain.

- **Adjustment Method**: Controls how height adjustments are applied:
  - **NormalizeHeight**: Normalizes terrain so the lowest modifiable point is at height 0
  - **ReferenceHeight**: Uses a reference height from fixed points
  - **RelativeAdjustment**: Adjusts heights by a relative amount
- **Uniform Step** (0.0-1.0): Amount to adjust modifiable terrain heights. Positive values raise terrain, negative values lower it.
- **Reference Method**: Method used to calculate reference height when using ReferenceHeight adjustment method and subtracts this value from all modifable points in the terrain.
  - **AverageOfFixedPoints**: Uses the average height of all fixed points
  - **MinimumOfFixedPoints**: Uses the minimum height of all fixed points
  - **MaximumOfFixedPoints**: Uses the maximum height of all fixed points

### Basic Smoother

Simple uniform smoothing that averages neighbouring heights. GPU-accelerated with CPU fallback.

- **Iterations** (1-100): Number of smoothing passes to apply to the terrain. Higher values produce more smoothed terrain.

### Enhanced Distance Smoother

Smoothing factor decreases as nearest distacne to a fix point increases, resulting in blending the surrounding terrain at the edges of the fixed points, without taking away too much detail from the modifiable terrain.GPU-accelerated with CPU fallback.

- **Base Smoothing** (0.1-10.0): Overall smoothing strength applied to terrain. Higher values create more aggressive smoothing and produce stronger blending effects.
- **Iterations** (1-100): Number of smoothing passes to apply. More iterations create smoother terrain at the cost of performance and potential loss of detail.
- **Constrained Height Proximity Weight** (1.0-10.0): Weight multiplier for neighboring points that are closer to constrained heights. Higher values create stronger influence from constrained areas, pulling terrain heights more toward fixed point heights.

### Distance-Based Height Scaler

Adjusts heights based on nearest distance to fixed points

- **Max Scale Factor** (0.0-1.0): Maximum scaling factor to apply to heights. Lower values create more dramatic scaling effects, with 0 flattening terrain completely near boundaries and 1 having no effect. 
- **Reference Method**: Method used to determine the reference height for blending operations:
  - **AverageOfFixedPoints**: Uses the average height of all fixed points
  - **MinimumOfFixedPoints**: Uses the minimum height of all fixed points
  - **MaximumOfFixedPoints**: Uses the maximum height of all fixed points

### Hydraulic Erosion

Simulates water flow over terrain to create valleys and channels.

- **Droplet Count** (100-10000): Number of water droplets to simulate in the erosion process. Higher values create more erosion and more pronounced water flow patterns, but increase computation time.
- **Erosion Strength** (0.01-1.0): Determines how strongly each droplet erodes the terrain. Higher values create more pronounced erosion features and deeper channels.
- **Springs Per Droplet** (1-10): Number of water flow paths (springs) each droplet creates. Higher values create more branching erosion patterns, simulating water flowing in multiple directions.
- **Solubility** (0.001-0.1): Amount of sediment a droplet removes per step during erosion. Controls the rate at which terrain erodes along water flow paths. Higher values create deeper and more pronounced channels.

### Thermal Erosion

Simulates material slumping to reduce unrealistically steep slopes. GPU-accelerated with CPU fallback.

- **Iterations** (1-100): Number of erosion passes to perform. Higher values create more pronounced erosion effects and more natural sloping, but increase computation time.
- **Erosion Threshold** (0.001-0.1): Threshold difference in height required to trigger erosion. Controls the minimum slope steepness that will erode. Lower values affect more gradual slopes.
- **Erosion Rate** (0.01-1.0): Rate at which material is transferred during erosion (range 0-1). Higher values create faster, more dramatic erosion. Controls how much material moves in each iteration.
