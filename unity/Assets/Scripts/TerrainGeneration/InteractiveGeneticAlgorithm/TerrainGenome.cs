using System;
using System.Collections.Generic;
using TerrainGeneration.Core;
using TerrainGeneration.Generators;
using TerrainGeneration.Modifiers.Smoothers;
using TerrainGeneration.Modifiers.Erosion;
using TerrainGeneration.Modifiers.HeightAdjusters;
using UnityEngine;
using HeightAdjustmentMethod = TerrainGeneration.Modifiers.HeightAdjusters.HeightAdjustmentMethod;
using Random = UnityEngine.Random;

namespace TerrainGeneration.InteractiveGeneticAlgorithm
{
    /// <summary>
    /// Represents a genome for terrain evolution, containing a list of operations and genetic algorithm functionality.
    /// Used for the interactive genetic algorithm to evolve terrain parameters.
    /// </summary>
    [Serializable]
    public class TerrainGenome
    {
        #region Fields
        /// <summary>
        /// Name of the genome for display purposes in the UI.
        /// </summary>
        public string name;

        /// <summary>
        /// Ordered list of terrain operations (both generators and smoothers) that define this genome.
        /// </summary>
        public List<ITerrainOperation> Operations = new();
        #endregion
        
        #region Constructor
        /// <summary>
        /// Creates a new TerrainGenome with the specified name.
        /// </summary>
        /// <param name="name">The display name for this genome.</param>
        public TerrainGenome(string name)
        {
            this.name = name;
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Creates a deep copy of this genome with all operations cloned.
        /// </summary>
        /// <returns>A new TerrainGenome instance with the same name and cloned operations.</returns>
        public TerrainGenome Clone()
        {
            TerrainGenome clone = new TerrainGenome(name);
            
            // Clone each operation
            foreach (var operation in Operations)
            {
                clone.Operations.Add(operation.Clone());
            }
            
            return clone;
        }
        
        /// <summary>
        /// Performs crossover between this genome and another genome to create a child genome.
        /// </summary>
        /// <param name="other">The other parent genome to crossover with.</param>
        /// <returns>A new genome resulting from crossover of the two parents.</returns>
        public TerrainGenome Crossover(TerrainGenome other)
        {
            TerrainGenome child = new TerrainGenome($"Child of {name} and {other.name}");
            
            // Number of terrain operations in each genome
            int thisCount = Operations.Count;
            int otherCount = other.Operations.Count;
            
            // Choose a crossover point for operations
            int operationsCrossPoint = Random.Range(0, Mathf.Min(thisCount, otherCount) + 1);
            
            // Take operations from this genome up to crossover point
            for (int i = 0; i < operationsCrossPoint && i < thisCount; i++)
            {
                child.Operations.Add(Operations[i].Clone());
            }
            
            // Take remaining operations from other genome
            for (int i = operationsCrossPoint; i < otherCount; i++)
            {
                child.Operations.Add(other.Operations[i].Clone());
            }
            
            return child;
        }
        
        /// <summary>
        /// Mutates the genome by randomly adding various terrain operations.
        /// </summary>
        /// <param name="mutationCount">Number of mutations to apply (default: 1).</param>
        public void Mutate(int mutationCount = 1)
        {
            for (int i = 0; i < mutationCount; i++)
            {
                float mutationType = Random.value;
                if (mutationType < 0.25f)
                {
                    RemoveRandomOperation(Random.Range(3, 6));
                }
                else if (mutationType < 0.5f)
                {
                    HeightScalingMutation();
                }
                else if (mutationType < 0.75f)
                {
                    AddExtremeSmoothing(false);
                }
                else if (mutationType < 1.0f)
                {
                    AddTexture();
                }
            }
        }
        
        /// <summary>
        /// Creates a genome with variation but smart heuristics for good initialization.
        /// </summary>
        /// <param name="name">Optional custom name for the generated genome (default: "Random Genome").</param>
        /// <returns>A new generated TerrainGenome which has operations applied in a smart order.</returns>
        public static TerrainGenome CreateSmartTerrainGenome(string name = "Random Genome")
        {
            TerrainGenome genome = new TerrainGenome(name);
            genome.AddSmartOperations(Random.Range(2, 7));
            return genome;
        }
        #endregion
        
        #region Private Methods
        
        // Add a series of operations to the genome in a smart order
        private void AddSmartOperations(int loopCount = 1)
        {
            for (int i = 0; i < loopCount; i++)
            {
                AddGenerator(); // always start with a generator
                if (Random.value < 0.2f) //Add extreme smoothing occasionally
                {
                    AddExtremeSmoothing(true);
                }
                
                if (Random.value < 0.5f) // Add erosion occasionally
                {
                    AddRandomErosion();   
                }
                
                AddHeightAdjustment();
                
                AddSmoother();
                
            }
            AddSmartEndingOperations();
            
            if (Random.value < 0.5f) // Occasionally add texture
            {
                AddTexture();
            }
        }
        
        private void AddGenerator()
        {
            // Add voronoi before occasionally:
            if (Random.value < 0.3f)
            {
                Operations.Add(new VoronoiGenerator
                {
                    PeakCount = Random.Range(1, 20),
                    FallRate = Random.Range(0.5f, 3f),
                    DropOff = Random.Range(0.5f, 10f),
                    MinHeight = Random.Range(0f, 0.3f),
                    MaxHeight = Random.Range(0.3f, 0.7f),
                    AvoidConstrainedPoints = Random.value > 0.7f,
                    MaxPlacementAttempts = Random.Range(100, 200),
                    UseDeterministicSeed = true
                });
            }
            
            // Add Perlin or Midpoint displacement 
            int generatorType = Random.Range(0, 2);
            switch (generatorType)
            {
                case 0:
                    Operations.Add(new PerlinNoiseGenerator
                    {
                        Frequency = Random.Range(0.001f, 0.01f),
                        Octaves = Random.Range(3, 10),
                        Persistence = Random.Range(0.1f, 0.7f),
                        Amplitude = Random.Range(0.1f, 0.7f),
                        UseDeterministicSeed = true
                    });
                    break;
                case 1:
                    Operations.Add(new MidpointDisplacementGenerator
                    {
                        MinHeight = Random.Range(0f, 0.3f),
                        MaxHeight = Random.Range(0.5f, 0.7f),
                        Smoothness = Random.Range(0.7f, 0.99f),
                        InitialRandomRange = Random.Range(0.8f, 1f),
                        UseDeterministicSeed = true
                    });
                    break;
            }
        }

        private void AddHeightAdjustment()
        {
            if (Random.value < 0.6) // Occasionally add uniform height adjustment
            {
                Operations.Add(new UniformHeightModifier
                {
                    AdjustmentMethod = (HeightAdjustmentMethod)Random.Range(0, 3),
                    UniformStep = Random.Range(-0.3f, 0.2f),
                    ReferenceMethod = (ReferenceHeightMethod)Random.Range(0, 3)
                });
            }
            // Add distance-based height adjustment always
            Operations.Add(new DistanceBasedHeightScaler
            {
                MaxScaleFactor = Random.Range(0.3f, 0.7f),
                ReferenceMethod = (ReferenceHeightMethod)Random.Range(0, 3)
            });
        }
        
        private void AddSmoother()
        {
            // Add distance smoothing more often
            if (Random.value < 0.25) 
            {
                Operations.Add(new BasicSmoother
                {
                    Iterations = Random.Range(5, 30)
                });
            }
            else
            {
                Operations.Add(new EnhancedDistanceSmoother
                {
                    BaseSmoothing = Random.Range(1f,5f),
                    Iterations = Random.Range(10, 60),
                    ConstrainedHeightProximityWeight = Random.Range(1f, 4f),
                });
            }
        }

        private void AddRandomErosion()
        {
            // Choose a random erosion
            int erosionType = Random.Range(0, 2);
            switch (erosionType)
            {
                case 0:
                    Operations.Add(new HydraulicErosion
                    {
                        DropletCount = Random.Range(200, 800),
                        ErosionStrength = Random.Range(0.05f, 0.2f),
                        SpringsPerDroplet = Random.Range(7, 10),
                        Solubility = Random.Range(0.001f, 0.002f),
                        UseDeterministicSeed = false
                    });
                    break;
                case 1:
                    Operations.Add(new ThermalErosion
                    {
                        Iterations = Random.Range(30, 100),
                        ErosionThreshold = Random.Range(0.001f, 0.002f),
                        ErosionRate = Random.Range(0.1f, 0.15f)
                    });
                    break;
            }
            
            // always apply basic smoothing after erosion:
            Operations.Add(new BasicSmoother
            {
                Iterations = Random.Range(15, 25)
            });
        }
        
        // Always end initial terrain genome with this post-processing
        private void AddSmartEndingOperations()
        {
            if (Random.value < 0.8f)
            {
                Operations.Add(new DistanceBasedHeightScaler
                {
                    MaxScaleFactor = Random.Range(0.3f, 0.8f),
                    ReferenceMethod = (ReferenceHeightMethod)Random.Range(0, 3)
                });
            }
            
            if (Random.value < 0.8f)
            {
                Operations.Add(new EnhancedDistanceSmoother
                {
                    BaseSmoothing = Random.Range(1f, 3f),
                    Iterations = Random.Range(5, 25),
                    ConstrainedHeightProximityWeight = 1f
                });
            }
            
            Operations.Add(new BasicSmoother
                {
                    Iterations = Random.Range(5, 15)
                }
            );
        }
        
        private void AddTexture()
        {
            int textureType = Random.Range(0, 2);
            switch (textureType)
            {
                case 0: 
                    Operations.Add(new MidpointDisplacementGenerator
                    {
                        MinHeight = 0.001f,
                        MaxHeight = 0.03f,
                        Smoothness = Random.Range(0.2f, 0.5f),
                        InitialRandomRange = 1f,
                        UseDeterministicSeed = true
                    });
                    break;
                case 1:
                    Operations.Add(new PerlinNoiseGenerator
                    {
                        Frequency = Random.Range(0.05f, 0.08f),
                        Octaves = Random.Range(5, 8),
                        Persistence = Random.Range(0.6f, 0.8f),
                        Amplitude = Random.Range(0.01f, 0.015f),
                        UseDeterministicSeed = true
                    });
                    break;
                    
            }
            Operations.Add(new EnhancedDistanceSmoother
            {
                BaseSmoothing = 1f,
                Iterations = Random.Range(15, 25),
                ConstrainedHeightProximityWeight = 1
            });
        }

        // Add a height scaling mutation at some random point in the operations
        private void HeightScalingMutation()
        {
            int indexToMutate = Random.Range(1, Operations.Count);
            Operations.Insert(indexToMutate, new DistanceBasedHeightScaler
            {
                MaxScaleFactor = 0.5f,
                ReferenceMethod = (ReferenceHeightMethod)Random.Range(0, 3)
            });
            Operations.Insert(++indexToMutate , new DistanceBasedHeightScaler
            {
                MaxScaleFactor = 0.7f,
                ReferenceMethod = (ReferenceHeightMethod)Random.Range(0, 3)
            });
            
            Operations.Insert(++indexToMutate, new DistanceBasedHeightScaler
            {
                MaxScaleFactor = 0.9f,
                ReferenceMethod = (ReferenceHeightMethod)Random.Range(0, 3)
            });
            
        }

        private void AddExtremeSmoothing(bool atEnd = true)
        {
            int index = Random.Range(0, Operations.Count);
            if (atEnd)
            {
                index = Operations.Count - 1;
            }
            Operations.Insert(index, new EnhancedDistanceSmoother
            {
                Iterations =  Random.Range(600, 1000),
                BaseSmoothing = Random.Range(1f, 10f),
                ConstrainedHeightProximityWeight = Random.Range(2f, 5f),
            });
            Operations.Insert(++index, new EnhancedDistanceSmoother
            {
                Iterations =  Random.Range(200, 300),
                BaseSmoothing = 1,
                ConstrainedHeightProximityWeight = 1,
            });
            Operations.Insert(++index, new BasicSmoother
            {
                Iterations = Random.Range(200, 300),
            });
            
        }
        
        
        private void RemoveRandomOperation(int numberOfOperations)
        {
            for (int i = 0; i < numberOfOperations; i++)
            {
                if (Operations.Count > 0)
                {
                    int indexToRemove = Random.Range(0, Operations.Count);
                    Operations.RemoveAt(indexToRemove);
                }
            }
        }
        #endregion
    }
}