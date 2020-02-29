# EvoStone

This project is the .NET successor to [EvoSabber](https://github.com/tehqin/EvoSabber) and implements the Hearthstone experiments for the paper *[Covariance Matrix Adaptation for the Rapid Illumination of Behavior Space](https://arxiv.org/abs/1912.02400)*. The project contains distributed implementations of evolutionary algorithms CMA-ES, MAP-Elites, and CMA-ME, the new algorithm detailed in the paper. 

This project is designed to be run on a High-Performance Computing (HPC) cluster and is divided into two subprojects `DeckEvaluator` (for running Hearthstone games and collecting data from those games) and `StrategySearch` (for running distributed versions of each evolutionary algorithm). Unlike EvoSabber, EvoStone is a unified .NET project and all subprojects can be compiled through a single command.

## Installation
To install the project, you need to install the [.NET](https://dotnet.microsoft.com/download) developer toolkit for your system. You may also need the [NuGet](https://docs.microsoft.com/en-us/nuget/install-nuget-client-tools) client tools for updating dependencies for the project. Included in this project is a `setup.py` script to make installation easier. Users can install Python 3 or follow the commands in the script for installation.

Next you need to compile [SabberStone](https://github.com/HearthSim/SabberStone), the Hearthstone simulator for running games. Follow the instructions on the SabberStone github compile the project into `SabberStone.dll`.

Next move to the `TestBed/StrategySearch` directory. From here you can run the `setup.py` script.

```
python3 setup.py
```
That's it! Your project is now setup to run experiments from the paper.

## Running Experiments (Locally)

The setup script created three empty folders in the `TestBed/StrategySearch` directory: `active`, `boxes`, and `logs`. The `active` folder is used for initial communication between distributed nodes and for letting the workers know when the search is complete. The `boxes` folder is for sending neural networks to the `DeckEvaluator` and receiving results. The `logs` folder holds CSV files for logging information about the neural net policies and elite maps from the search. 

First we need to start the control node responsible for running our search (CMA-ES, MAP-Elites, CMA-ME, etc). To do this, run the following command.

```
dotnet bin/StrategySearch.dll config/rogue_me_exp.tml
```

The first parameter passed is the config file for the experiment. Here we are running MAP-Elites using the Tempo Rogue deck. However, the search isn't moving because it doesn't have any worker nodes to play games. To start a worker node, run the following command.

```
dotnet bin/DeckEvaluator.dll 1
```

This command starts a new DeckEvaluator node. The first parameter is the node ID. You can start multiple nodes locally, but you must specify a different node for each worker. The node will take a strategy generated from the search algorithm and play 200 games using that strategy. Once the games are complete, the node will send results back to the control node and await a new strategy.

## Running Experiments (Distributed)

If you run the search locally, you will realize the experiment is a bit slow. That is why the search was designed to be distributed and run on an HPC cluster. Included in the `TestBed/StrategySearch` folder are two GridEngine scripts. You can start an experiment running the following commands.

```
qsub startSearch.sh
qsub startWorker.sh
```

The setup script copied your DLL files into the bin directory to make the project standalone. You only need to copy the `TestBed/StrategySearch` directory up to your cluster to run experiments.

## Config Files

There are two types of config files for EvoStone. The first specifies the experiment level parameters (see below).

```
[Evaluation]
OpponentDeckSuite = "resources/decks/suites/eliteMeta.tml"
DeckPools = ["resources/decks/pools/eliteDecks.tml",
             "resources/decks/pools/metaDecks.tml"]

[[Evaluation.PlayerStrategies]]
NumGames = 200
Strategy = "NeuralNet"

[Search]
Type = "MAP-Elites"
ConfigFilename = "config/me_config.tml"

[Network]
LayerSizes = [15, 5, 4, 1]

[Player]
DeckPool = "Meta Decks"
DeckName = "Tempo Rogue"

[[Nerfs]]
CardName = "EVIL Miscreant"
NewManaCost = 3
NewAttack = 1
NewHealth = 5

[[Nerfs]]
CardName = "Raiding Party"
NewManaCost = 3
```

The config file specifies how many games are played, the opponents to play against, the architecture of the neural net, and other useful information. However, the parameters specific to the search being run are setup in a different config file `me_config.tml` (see below).

```
[Search]
InitialPopulation = 500
NumToEvaluate = 50000
MutationPower = 0.05

[Map]
Type = "FixedFeature"
StartSize = 100
EndSize = 100

[[Map.Features]]
Name = "NumTurns"
MinValue = 5.0
MaxValue = 15.0

[[Map.Features]]
Name = "HandSize"
MinValue = 1.0
MaxValue = 7.0
```

This config file specifies the behavior dimensions for the map of elites and parameters specific to running the search algorithm.
