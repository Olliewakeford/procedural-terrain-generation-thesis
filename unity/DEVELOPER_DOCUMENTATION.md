# Developer Documentation: Constrained Procedural Terrain Generation

---

## Table of Contents

1. [Architecture Overview](#1-architecture-overview)
2. [Core Components](#2-core-components)
3. [Terrain Generators](#3-terrain-generators)
4. [Terrain Modifiers](#4-terrain-modifiers)
5. [Interactive Genetic Algorithm](#5-interactive-genetic-algorithm)
6. [Extension Guidelines](#6-extension-guidelines)

---

## 1. Architecture Overview

### 1.1 System Design Principles

The constrained procedural terrain generation system is built on the following core principles:

- **Component-Based Architecture**: Each algorithm is implemented as a self-contained component implementing common interfaces
- **Distance-Based Processing**: Operations use distance from fixed points to create natural transitions
- **Pipeline Architecture**: Multi-stage processing with generators followed by modifiers
- **Interactive Evolution**: Genetic algorithm approach for parameter optimization
- **GPU Acceleration**: Compute shader support with CPU fallbacks for performance

### 1.2 Core Data Flow

```
1. Mask Analysis → Identify constrained/modifiable areas
2. Distance Grid → Calculate distances to constrained points
3. Generation → Apply terrain generators (Perlin, Voronoi, etc.)
4. Modification → Apply smoothers and erosion
5. Integration → Blend with existing constrained heights
```

### 1.3 Key Interfaces

- `ITerrainOperation`: Base interface for all terrain operations
- `ITerrainGenerator`: Interface for terrain generation algorithms
- `ITerrainModifier`: Interface for terrain modification algorithms

## 2. Core Components

### 2.1 TerrainManager

**Purpose**: Central coordinator for all terrain operations

**Key Responsibilities**:
- Manages Unity terrain integration
- Coordinates mask-based constraint enforcement
- Orchestrates operation pipeline execution
- Provides undo/redo functionality

**Core Methods**:
```csharp
// Apply single operation to terrain
public void ApplyOperation(ITerrainOperation operation, string operationName = "Apply Operation")

// Apply sequence of operations
public void ApplyOperations(List<ITerrainOperation> operations, string operationName = "Apply Operations")

// Reset modifiable areas to zero height
public void RestoreTerrain()
```

### 2.2 DistanceGridManager

**Purpose**: Calculates and manages distance grids for distance-based operations

**Algorithm**: Uses breadth-first search with Euclidean distances to calculate the minimum distance from each terrain point to the nearest constrained point.

**Key Features**:
- Efficient BFS-based distance calculation
- Support for 8-directional neighbors (including diagonals)
- Euclidean distance metrics for natural falloff

### 2.3 Core Interfaces

#### ITerrainOperation
Base interface for all terrain operations with common functionality:

```csharp
public interface ITerrainOperation
{
    string Name { get; }                    // Display name
    TerrainOperationType OperationType { get; } // Generator or Modifier
    bool RequiresDistanceGrid { get; }      // Distance dependency
    
    void ApplyOperation(float[,] heightMap, int width, int height, 
                       Func<int, int, bool> shouldModify, float[,] distanceGrid = null);
    
    ITerrainOperation Clone();              // Deep copy support
}
```

## 3. Terrain Generators

Generators create initial terrain features and implement the `ITerrainGenerator` interface.

### 3.1 PerlinNoiseGenerator

**Algorithm**: Generates terrain using Perlin noise with fractional Brownian motion (fBM)

**Parameters**:
- `Frequency` (0.001-0.05): Controls scale of noise pattern
- `Octaves` (3-10): Number of noise layers to combine
- `Persistence` (0.1-1.0): Contribution decay between octaves
- `Amplitude` (0.01-1.0): Overall height scaling

**Use Cases**: Natural-looking base terrain with organic variations

### 3.2 VoronoiGenerator

**Algorithm**: Creates mountain-like features using Voronoi diagrams

**Parameters**:
- `PeakCount` (1-100): Number of mountain peaks
- `FallRate` (0.5-10.0): Linear distance falloff rate
- `DropOff` (0.5-10.0): Exponential distance falloff
- `MinHeight`/`MaxHeight` (0.0-1.0): Height range for peaks
- `AvoidConstrainedPoints`: Prevents peaks near fixed areas

**Use Cases**: Mountainous terrain with distinct peaks and valleys

### 3.3 MidpointDisplacementGenerator

**Algorithm**: Diamond-Square fractal terrain generation

**Parameters**:
- `MinHeight`/`MaxHeight` (0.0-1.0): Height range
- `Smoothness` (0.1-1.0): Controls terrain roughness
- `InitialRandomRange` (0.1-1.0): Initial displacement variance

**Use Cases**: Fractal landscapes with self-similar detail at multiple scales

## 4. Terrain Modifiers

Modifiers alter existing terrain and implement the `ITerrainModifier` interface.

### 4.1 Smoothing Operations

#### BasicSmoother
- **Algorithm**: Simple neighborhood averaging
- **GPU Accelerated**: Yes
- **Parameters**: `Iterations` (1-100)
- **Use Cases**: General terrain smoothing

#### EnhancedDistanceSmoother
- **Algorithm**: Distance-weighted smoothing with exponential falloff
- **GPU Accelerated**: Yes
- **Distance Dependency**: Required
- **Parameters**:
  - `BaseSmoothing` (0.1-10.0): Overall smoothing strength
  - `Iterations` (1-100): Number of smoothing passes
  - `ConstrainedHeightProximityWeight` (1.0-10.0): Influence from fixed areas
- **Use Cases**: Creating natural transitions between fixed and generated terrain

### 4.2 Erosion Operations

#### HydraulicErosion
- **Algorithm**: Simulates water droplet flow and sediment transport
- **Parameters**:
  - `DropletCount` (100-10000): Number of water droplets
  - `ErosionStrength` (0.01-1.0): Erosion intensity
  - `SpringsPerDroplet` (1-10): Branching factor
  - `Solubility` (0.001-0.1): Sediment removal rate
- **Use Cases**: Creating realistic valleys and water channels

#### ThermalErosion
- **Algorithm**: Simulates material slumping on steep slopes
- **GPU Accelerated**: Yes
- **Parameters**:
  - `Iterations` (1-100): Number of erosion passes
  - `ErosionThreshold` (0.001-0.1): Minimum slope for erosion
  - `ErosionRate` (0.01-1.0): Material transfer rate
- **Use Cases**: Reducing unrealistic steep slopes

### 4.3 Height Adjustment Operations

#### DistanceBasedHeightScaler
- **Algorithm**: Scales heights based on distance from constrained points
- **Distance Dependency**: Required
- **Parameters**:
  - `MaxScaleFactor` (0.0-1.0): Maximum height scaling
  - `ReferenceMethod`: Height calculation method for blending
- **Use Cases**: Preventing cliff formations at constraint boundaries

## 5. Interactive Genetic Algorithm

The Interactive Genetic Algorithm (IGA) provides automated parameter optimization based on user preferences.

### 5.1 TerrainGenome

**Purpose**: Represents a complete terrain generation pipeline as a genetic genome

**Structure**:
- Ordered list of `ITerrainOperation` instances
- Parameter values for each operation
- Name for identification

**Genetic Operations**:
```csharp
// Create offspring from two parent genomes
public TerrainGenome Crossover(TerrainGenome other)

// Randomly modify genome parameters
public void Mutate(int mutationCount = 1)

// Create deep copy of genome
public TerrainGenome Clone()
```

### 5.2 Evolution Process

1. **Initialization**: Generate random population of terrain genomes
2. **Evaluation**: Generate terrain previews for user assessment
3. **Selection**: User selects preferred terrain variants
4. **Reproduction**: Apply crossover and mutation to create new generation
5. **Iteration**: Repeat until satisfactory results achieved

### 5.3 Smart Genome Generation

The system includes heuristics for generating sensible initial genomes:
- Generators applied before modifiers
- Height adjustments before smoothing
- Appropriate parameter ranges for each operation type
- Deterministic seeds for reproducible results

## 6. Extension Guidelines

### 6.1 Adding New Generators

To implement a new terrain generator:

1. **Implement ITerrainGenerator**:
```csharp
[Serializable]
public class CustomGenerator : ITerrainGenerator
{
    public string Name => "Custom Generator";
    public TerrainOperationType OperationType => TerrainOperationType.Generator;
    public bool RequiresDistanceGrid => false; // or true if needed
    
    public void Generate(float[,] heightMap, int width, int height, 
                        Func<int, int, bool> shouldModify, float[,] distanceGrid = null)
    {
        // Your generation algorithm here
    }
    
    // Implement other interface methods...
}
```

2. **Add Serializable Parameters**: Use `[SerializeField]` for Unity serialization
3. **Implement Clone()**: Ensure deep copying of all parameters
4. **Respect Constraints**: Always check `shouldModify(x, y)` before modifying heights

### 6.2 Adding New Modifiers

Similar process for modifiers, implementing `ITerrainModifier` instead.

### 6.3 GPU Acceleration

For GPU-accelerated operations:

1. **Inherit from GpuTerrainOperation**
2. **Create compute shader** in `Resources/Shaders/`
3. **Implement fallback CPU version** for compatibility
4. **Override required properties**:
   - `ShaderName`: Path to compute shader
   - `KernelName`: Compute shader kernel function name

### 6.4 Integration with Genetic Algorithm

To include new operations in the genetic algorithm:

1. **Add to TerrainGenome factory methods**
2. **Define parameter ranges** for mutation
3. **Implement proper serialization** for crossover operations
4. **Test with evolution system** to ensure stable behavior

---

*End of Developer Documentation*
