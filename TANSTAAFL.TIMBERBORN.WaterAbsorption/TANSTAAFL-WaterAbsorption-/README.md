# WaterAbsorption

WaterAbsorption is a Timberborn mod that alters the evaporation rate of water and adds absorption of water to Growables.

# Usage

There are six values to alter, listed below:

* NormalEvaporationSpeedMultiplier: Number by wich the normal water evaporation is multiplied (Default 0.25f)
* FastEvaporationSpeedMultiplier: Number by wich the fast water evaporation (when the level is below 0.2) is multiplied (Default 0.25f)
* IrrigatorTickIncrement: 1 over how many times Growables have to tick for 1 water be consumed in the Irrigator (Default 0.001f)
* GrowableTickWaterDepth: By how much the closest water tile of a Growable is reduced when it ticks (Default -0.000005f)
* MaxTicks: Every Growable is processed in a game Tick. MaxTicks has to be higher than the number of Growables used (Default 13)
* MaxSearchDepth: How far a Growable will search for water to absorb. Relevant if buildings that moisture further like the Big Irrigation Tower from the mod Water Extention are used (Default 25)

Change the value of the variables in the config, which is probably in BepInEx\plugins\WaterAbsorption\configs\WaterAbsorption.json

If the config is not showing, try to launch the game and start up a save, that should create it.

# Issues

If there's a warning message in the log about a missing JSON property on game launch, delete the config file and restart the game to recreate it.

# Contributions
PRs are always welcome on the github page!

# Changelog

## v1.0.4 - 8.12.2022
- Alters config options
- Adds README

## v1.0.3 - 8.12.2022
- Adds config options

## v1.0.2 - 8.12.2022
- Fix minor issues

## v1.0.0 - 8.12.2022
- Initial release