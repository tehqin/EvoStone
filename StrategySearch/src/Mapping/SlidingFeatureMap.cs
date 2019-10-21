using System;
using System.Collections.Generic;
using System.IO;

using StrategySearch.Config;
using StrategySearch.Mapping.Sizers;
using StrategySearch.Search;

/* This is a FeatureMap that slides its feature boundaries periodically.
 * Instead of having even feature boundaries, they are readjusted so that
 * each group is more evenly distributed.
 */

namespace StrategySearch.Mapping
{
   class SlidingFeatureMap : FeatureMap
   {
      private static Random rnd = new Random();
      private List<Individual> _allIndividuals;

      private MapSizer _groupSizer;
      private int _maxIndividualsToEvaluate;
      private int _remapFrequency;

      public int NumGroups { get; private set; }
      public int NumFeatures { get; private set; }
      public Dictionary<string, Individual> EliteMap { get; private set; }
      public Dictionary<string, int> CellCount { get; private set; }

      private List<double>[] _groupBoundaries;
      private List<string> _eliteIndices;

      public SlidingFeatureMap(int numToEvaluate, MapParams config, MapSizer groupSizer)
      {
         _allIndividuals = new List<Individual>();
         _groupSizer = groupSizer;
         _maxIndividualsToEvaluate = numToEvaluate;
         _remapFrequency = config.RemapFrequency;
         NumFeatures = config.Features.Length;
      
         _groupBoundaries = new List<double>[NumFeatures];
      }

      private int GetFeatureIndex(int featureId, double feature)
      {
         // Find the bucket index we belong on this dimension
         int index = 0;
         while (index < NumGroups && 
                _groupBoundaries[featureId][index] < feature + 1e-9)
         {
            index++;
         }

         return Math.Max(0, index-1);
      }

      private bool AddToMap(Individual toAdd)
      {
         var features = new int[NumFeatures];
         for (int i=0; i<NumFeatures; i++)
            features[i] = GetFeatureIndex(i, toAdd.Features[i]);
         string index = string.Join(":", features);
     
         bool replacedElite = false;
         if (!EliteMap.ContainsKey(index))
         {
            _eliteIndices.Add(index);
            EliteMap.Add(index, toAdd);
            CellCount.Add(index, 0);
            replacedElite = true;
         }
         else if (EliteMap[index].Fitness < toAdd.Fitness)
         {
            EliteMap[index] = toAdd;
            replacedElite = true;
         }

         CellCount[index] += 1;
         return replacedElite;
      }

      // Update the boundaries of each feature.
      // Add all the individuals available to the map.
      private void Remap()
      {
         Console.WriteLine("REMAP ------------------");
         double portionDone = 1.0 * _allIndividuals.Count / _maxIndividualsToEvaluate;
         NumGroups = _groupSizer.GetSize(portionDone);

         var features = new List<double>[NumFeatures];
         for (int i=0; i<NumFeatures; i++)
            features[i] = new List<double>();
      
         foreach (Individual cur in _allIndividuals)
            for (int i=0; i<NumFeatures; i++)
               features[i].Add(cur.Features[i]);
      
         for (int i=0; i<NumFeatures; i++)
            features[i].Sort();
        
         for (int i=0; i<NumFeatures; i++)
         {
            _groupBoundaries[i] = new List<double>();
         
            for (int x=0; x<NumGroups; x++)
            {
               int sampleIndex = x * _allIndividuals.Count / NumGroups;
               _groupBoundaries[i].Add(features[i][sampleIndex]);
            }
         }

         // Populate the feature map using the new boundaries.
         _eliteIndices = new List<string>();
         EliteMap = new Dictionary<string,Individual>();
         CellCount = new Dictionary<string,int>();
         foreach (Individual cur in _allIndividuals)
            AddToMap(cur);
      }

      public double GetFeatureScalar(int i)
      {
         return 1.0;
      }

      public bool Add(Individual toAdd)
      {
         if (_allIndividuals.Count % _remapFrequency == 0)
            Remap();
            
         _allIndividuals.Add(toAdd);
         return AddToMap(toAdd);
      }

      public Individual GetRandomElite()
      {
         int pos = rnd.Next(_eliteIndices.Count);
         string index = _eliteIndices[pos];
         return EliteMap[index];
      }
   }
}
