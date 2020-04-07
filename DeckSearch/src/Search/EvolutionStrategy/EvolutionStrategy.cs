using System;
using System.Linq;
using System.Collections.Generic;

using DeckSearch.Config;
using SabberStoneCore.Model;

namespace DeckSearch.Search.EvolutionStrategy
{
    class EvolutionStrategyAlgorithm : SearchAlgorithm
    {
        private EvolutionStrategyParams _params;
        private List<Individual> _parents;
        private int _individualsEvaluated;
        private int _individualsDispatched;
        private static Random rnd = new Random();
        public EvolutionStrategyAlgorithm(EvolutionStrategyParams config) 
        {
            _params = config;
            _parents = null;
            _individualsEvaluated = 0;
            _individualsDispatched = 0;

        }

        private Individual ChooseParent()
        {
            int pos = rnd.Next(_parents.Count);
            return _parents[pos];
        }

        public Individual GenerateIndividual(List<Card> cardSet)
        {
            _individualsDispatched++;
            Individual ind = _individualsDispatched < _params.Search.InitialPopulation ? 
                             Individual.GenerateRandomIndividual(cardSet)
                           : ChooseParent().Mutate();
            return ind;
        }

        public bool IsBlocking() => _individualsDispatched >= _params.Search.InitialPopulation 
                                    &&_individualsEvaluated == 0;

        public bool IsRunning() => _individualsEvaluated < _params.Search.NumToEvaluate;
        

        public void ReturnEvaluatedIndividual(Individual ind)
        {
            ind.ID = _individualsEvaluated;
            _individualsEvaluated++;
            _parents.Add(ind);
            if (_individualsEvaluated >= _params.Search.InitialPopulation) {
                // choose parents if there are enough evaluated individuals
                _parents = _parents.OrderBy(o => o.Fitness)
                           .Reverse().Take(_params.Search.NumParents).ToList();
            }
        }
    }
}