using System;
using System.Collections.Generic;
using System.IO;

using DeckSearch.Config;
using DeckSearch.Mapping.Sizers;
using DeckSearch.Search;

/* This is a FeatureMap that slides its feature boundaries periodically.
 * Instead of having even feature boundaries, they are readjusted so that
 * each group is more evenly distributed.
 */

namespace DeckSearch.Mapping
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
                _groupBoundaries[featureId][index] < feature)
         {
            index++;
         }

         return Math.Max(0, index-1);
      }

      private void AddToMap(Individual toAdd)
      {
         var features = new int[NumFeatures];
         for (int i=0; i<NumFeatures; i++)
            features[i] = GetFeatureIndex(i, toAdd.Features[i]);
         string index = string.Join(":", features);
     
         if (!EliteMap.ContainsKey(index))
         {
            _eliteIndices.Add(index);
            EliteMap.Add(index, toAdd);
            CellCount.Add(index, 0);
         }
         else if (EliteMap[index].Fitness < toAdd.Fitness)
         {
            EliteMap[index] = toAdd;
         }

         CellCount[index] += 1;
      }

      // Update the boundaries of each feature.
      // Add all the individuals available to the map.
      private void Remap()
      {
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

      public void Add(Individual toAdd)
      {
         _allIndividuals.Add(toAdd);

         if (_allIndividuals.Count % _remapFrequency == 1)
            Remap();
         else 
            AddToMap(toAdd);
      }

      public Individual GetRandomElite()
      {
         int pos = rnd.Next(_eliteIndices.Count);
         string index = _eliteIndices[pos];
         return EliteMap[index];
      }
   }
}
