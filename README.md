# EvoStone

This project is the .NET successor to [EvoSabber](https://github.com/tehqin/EvoSabber) and implements the Hearthstone experiments for the paper "Covariance Matrix Adaptation for the Rapid Illumination of Behavior Space". The project contains distributed implementations of evolutionary algorithms CMA-ES, MAP-Elites, and CMA-ME, the new algorithm detailed in the paper. 

This project is designed to be run on a High-Performance Computing cluster and is divided into two subprojects `DeckEvaluator` (for running Hearthstone games and collecting data from those games) and `StrategySearch` which coordinates running distributed versions of each evolutionary algorithm and logging results. Unlike EvoSabber, EvoStone is a unified .NET project and all subprojects can be compiled through a single command.

## Installation
To install the project, you need to install the [.NET](https://dotnet.microsoft.com/download) developer toolkit for your system. You may also need the [NuGet](https://docs.microsoft.com/en-us/nuget/install-nuget-client-tools) client tools for updating dependencies for the project. Included in this project is a `setup.py` script to make installation easier. Users can install Python 3 or follow the commands in the script for installation.

Next you need to compile [SabberStone](https://github.com/HearthSim/SabberStone), the Hearthstone simulator for running games. The default configuration has EvoStone and SabberStone placed in the same folder, but users can modify the csproj file for each subproject to point to a different dll location.

Next move to the `TestBed/StrategySearch` directory. From here you can run the `setup.py` script.

```
python3 setup.py
```
That's it! Your project is now setup to run experiments from the paper.

## Running Experiments (Locally)

The setup script created three empty folders in the `TestBed/StrategySearch` directory: `active`, `boxes`, and `logs`. The `active` folder is used for initial communication between distributed nodes and for letting the workers know when the search is complete. The `boxes` folder is for sending neural networks to the `DeckEvaluator` and receiving results. The `logs` folder holds CSV files for logging information about the neural net policies and elite maps from the search. 

## Running Experiments (Distributed)

## Config Files
