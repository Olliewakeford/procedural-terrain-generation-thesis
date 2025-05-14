using System.Collections.Generic;
using UnityEngine;
using TerrainGeneration.Core;

namespace TerrainGeneration.InteractiveGeneticAlgorithm
{
    /// <summary>
    /// Manages an interactive genetic algorithm for terrain generation. 
    /// Handles population evolution, selection, and application of terrain genomes.
    /// </summary>
    public class TerrainEvolutionManager : MonoBehaviour
    {
        #region Properties & Fields

        /// <summary>
        /// Reference to the TerrainManager component that handles terrain operations.
        /// </summary>
        [SerializeField]private TerrainManager terrainManager;
        
        // Population and selection state
        private List<TerrainGenome> _population = new();
        private List<int> _selectedIndices = new();

        /// <summary>
        /// Gets the current generation number in the evolution process.
        /// </summary>
        public int CurrentGeneration { get; private set; }

        /// <summary>
        /// Gets or sets the size of the population for each generation.
        /// </summary>
        public int PopulationSize { get; set; } = 8;

        /// <summary>
        /// Gets or sets the probability of crossover between individuals during evolution.
        /// </summary>
        public float CrossoverProbability { get; set; } = 0.7f;

        /// <summary>
        /// Gets or sets the probability of mutation for individuals during evolution.
        /// </summary>
        public float MutationProbability { get; set; } = 0.1f;

        /// <summary>
        /// Gets the current number of individuals in the population.
        /// </summary>
        public int PopulationCount => _population.Count;
        #endregion
        
        #region Unity Methods
        private void Awake()
        {
            if (terrainManager != null) return;
            terrainManager = FindAnyObjectByType<TerrainManager>();
                
            if (terrainManager == null)
            {
                Debug.LogError("No Terrain Manager found. Please assign it in the inspector.");
            }
        }
        #endregion
        
        #region Public Methods
        /// <summary>
        /// Initializes the population with random terrain genomes.
        /// </summary>
        /// <param name="seedGenome">Optional seed genome to include in the initial population. If null, all genomes will be randomly generated.</param>
        public void InitializePopulation(TerrainGenome seedGenome = null)
        {
            _population.Clear();
            _selectedIndices.Clear();
            CurrentGeneration = 0;
            
            // If we have a seed genome, include it in the initial population
            if (seedGenome != null)
            {
                _population.Add(seedGenome.Clone());
            }
            
            // Create random genomes to fill the population
            while (_population.Count < PopulationSize)
            {
                TerrainGenome newGenome = TerrainGenome.CreateSmartTerrainGenome();
                newGenome.name = $"Generation 0 - Individual {_population.Count}";
                _population.Add(newGenome);
            }
        }
        
        /// <summary>
        /// Evolves the next generation based on selected individuals. 
        /// Uses elitism to keep the best individual and creates new genomes through crossover and mutation.
        /// </summary>
        public void EvolveNextGeneration()
        {
            if (_selectedIndices.Count < 1) // This is already handled in the UI, but just in case
            {
                return;
            }
            
            // Get the selected genomes as parents
            List<TerrainGenome> parents = new List<TerrainGenome>();
            foreach (int index in _selectedIndices)
            {
                parents.Add(_population[index]);
            }
            
            // Create new population
            List<TerrainGenome> newPopulation = new List<TerrainGenome>();
            
            // Elitism: Keep the best individual (first selected)
            TerrainGenome bestIndividual = parents[0].Clone();
            bestIndividual.name = $"Generation {CurrentGeneration + 1} - Elite";
            newPopulation.Add(bestIndividual);
            
            // Add one new completely new random terrain
            TerrainGenome newTerrain = TerrainGenome.CreateSmartTerrainGenome();
            newTerrain.name = $"Generation {CurrentGeneration + 1} - Random Terrain";
            newPopulation.Add(newTerrain);
            
            // Create new individuals through crossover and mutation
            while (newPopulation.Count < PopulationSize)
            {
                // Select random parents for crossover
                TerrainGenome parent1 = parents[Random.Range(0, parents.Count)];
                TerrainGenome parent2 = parents.Count > 1 
                    ? parents[Random.Range(0, parents.Count)] 
                    : TerrainGenome.CreateSmartTerrainGenome(); // If only one parent, create random second parent
                
                TerrainGenome child;
                
                // Crossover with CrossoverProbability
                if (Random.value < CrossoverProbability && parents.Count > 1)
                {
                    child = parent1.Crossover(parent2);
                }
                else // Keep the original parent
                {
                    child = parent1.Clone();
                }
                
                // Mutation
                if (Random.value < MutationProbability)
                {
                    child.Mutate(Random.Range(2,6)); // Number of mutations
                }
                
                child.name = $"Generation {CurrentGeneration + 1} - Individual {newPopulation.Count}";
                newPopulation.Add(child);
            }
            
            // Update population and generation counter
            _population = newPopulation;
            CurrentGeneration++;
            _selectedIndices.Clear();
        }
        
        /// <summary>
        /// Gets an individual from the population at the specified index.
        /// </summary>
        /// <param name="index">Index of the individual to retrieve.</param>
        /// <returns>The requested TerrainGenome, or null if the index is out of range.</returns>
        public TerrainGenome GetIndividual(int index)
        {
            if (index < 0 || index >= _population.Count)
            {
                return null;
            }
            
            return _population[index];
        }
        
        /// <summary>
        /// Sets the selected indices for the next evolution cycle.
        /// </summary>
        /// <param name="indices">List of selected population indices.</param>
        public void SetSelectedIndices(List<int> indices)
        {
            _selectedIndices = new List<int>(indices);
        }
        
        /// <summary>
        /// Applies a selected individual's genome to the terrain.
        /// </summary>
        /// <param name="index">Index of the individual to apply.</param>
        public void ApplyIndividual(int index)
        {
            if (index < 0 || index >= _population.Count)
            {
                return;
            }
            TerrainGenome genome = _population[index];
            terrainManager.ApplyOperations(genome.Operations, $"Apply {genome.name}");
        }
        #endregion
    }
}