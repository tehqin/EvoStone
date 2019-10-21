using System;
using System.Collections.Generic;
using System.IO;

using StrategySearch.Config;
using StrategySearch.Mapping.Sizers;
using StrategySearch.Search;

/* This is a FeatureMap that has fixed boundaries at even intervals.
 * It is exactly as described in the original MAP-Elites paper.
 */

namespace StrategySearch.Mapping
{
   class FixedFeatureMap : FeatureMap
   {
      private static Random rnd = new Random();

      private MapSizer _groupSizer;
      private int _numIndividualsEvaluated;
      private int _maxIndividualsToEvaluate;

      public int NumGroups { get; private set; }
      public int NumFeatures { get; private set; }
      public Dictionary<string, Individual> EliteMap { get; private set; }
      public Dictionary<string, int> CellCount { get; private set; }

      private List<string> _eliteIndices;
      private double[] _lowGroupBound;
      private double[] _highGroupBound;

		public FixedFeatureMap(int numToEvaluate, MapParams config, MapSizer groupSizer)
      {
         _groupSizer = groupSizer;
         _numIndividualsEvaluated = 0;
         _maxIndividualsToEvaluate = numToEvaluate;
         NumGroups = -1;

         NumFeatures = config.Features.Length;
         _lowGroupBound = new double[NumFeatures];
         _highGroupBound = new double[NumFeatures];
         for (int i=0; i<NumFeatures; i++)
         {
            _lowGroupBound[i] = config.Features[i].MinValue;
            _highGroupBound[i] = config.Features[i].MaxValue;
         }

         _eliteIndices = new List<string>();
         EliteMap = new Dictionary<string,Individual>();
         CellCount = new Dictionary<string,int>();
      }

      private int GetFeatureIndex(int featureId, double feature)
      {
         if (feature-1e-9 <= _lowGroupBound[featureId])
            return 0;
         if (_highGroupBound[featureId] <= feature+1e-9)
            return NumGroups-1;

         double gap = _highGroupBound[featureId] - _lowGroupBound[featureId] + 1;
         double pos = feature - _lowGroupBound[featureId];
         int index = (int)((NumGroups * pos + 1e-9) / gap);
         return index;
      }

      private string GetIndex(Individual cur)
      {
         var features = new int[NumFeatures];
         for (int i=0; i<NumFeatures; i++)
            features[i] = GetFeatureIndex(i, cur.Features[i]);
         return string.Join(":", features);
      }

		private bool AddToMap(Individual toAdd)
      {
         string index = GetIndex(toAdd);

         bool replacedElite = false;
         if (!EliteMap.ContainsKey(index))
         {
            toAdd.IsNovel = true;
            toAdd.Delta = toAdd.Fitness;
            _eliteIndices.Add(index);
            EliteMap.Add(index, toAdd);
            CellCount.Add(index, 0);
            replacedElite = true;
         }
         else if (EliteMap[index].Fitness < toAdd.Fitness)
         {
            toAdd.Delta = toAdd.Fitness - EliteMap[index].Fitness;
            EliteMap[index] = toAdd;
            replacedElite = true;
         }

         CellCount[index] += 1;
         return replacedElite;
      }

      private void Remap(int nextNumGroups)
      {
         NumGroups = nextNumGroups;

         List<Individual> allElites = new List<Individual>();
         foreach (string index in _eliteIndices)
            allElites.Add(EliteMap[index]); 

         _eliteIndices = new List<string>();
         EliteMap = new Dictionary<string,Individual>();
         CellCount = new Dictionary<string,int>();
         foreach (Individual cur in allElites)
            AddToMap(cur);
      }

      public double GetFeatureScalar(int i)
      {
         return _highGroupBound[i] - _lowGroupBound[i];
      }

      public bool Add(Individual toAdd)
      {
         _numIndividualsEvaluated++;
         double portionDone =
            1.0 * _numIndividualsEvaluated / _maxIndividualsToEvaluate;
         int nextNumGroups = _groupSizer.GetSize(portionDone);
         if (nextNumGroups != NumGroups)
            Remap(nextNumGroups);
            
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
