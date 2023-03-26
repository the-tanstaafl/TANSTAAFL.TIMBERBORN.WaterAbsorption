using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Timberborn.BlockSystem;
using Timberborn.MapIndexSystem;
using Timberborn.SoilMoistureSystem;
using UnityEngine;

namespace TANSTAAFL.TIMBERBORN.WaterAbsorption.WaterSearch
{
    internal class WaterSearcher
    {
        private RegisteredGrowable _registeredGrowable;
        private BlockObject _blockObject;
        private static MapIndexService _mapIndexService;
        private static SoilMoistureSimulator _soilMoistureSimulator;

        private static bool logDebug = false;

        internal WaterSearcher(RegisteredGrowable registeredGrowable, MapIndexService mapIndexService, SoilMoistureSimulator soilMoistureSimulator)
        {
            _registeredGrowable = registeredGrowable;
            _blockObject = registeredGrowable._blockObject;
            _mapIndexService = mapIndexService;
            _soilMoistureSimulator = soilMoistureSimulator;
        }

        internal void FindLocationAdvanced(ref bool found, ref bool isIrrigationTower)
        {
            Dictionary<Direction.CardinalDirection, float> moistureLevels = new Dictionary<Direction.CardinalDirection, float>();

            for (short i = 0; i < 8; i++)
            {
                Direction.CardinalDirection cardinalDirection = (Direction.CardinalDirection)i;
                (int x, int y) item = Direction.CardinalDirectionXY[cardinalDirection];

                var pointX = _blockObject.Coordinates.x + item.x;
                var pointY = _blockObject.Coordinates.y + item.y;
                if (logDebug)
                {
                    WaterAbsorptionPlugin.Log.LogInfo($"i: {i} iX: {item.x} iY: {item.y} cX: {_blockObject.Coordinates.x} xY: {_blockObject.Coordinates.y} pointX: {pointX} pointY: {pointY}");
                }

                if (pointX < 0 || pointY < 0 || pointX > _mapIndexService.MapSize.x || pointY > _mapIndexService.MapSize.y)
                {
                    continue;
                }

                CheckIfWaterOrIrrigator(ref found, ref isIrrigationTower, pointX, pointY);
                if (found)
                {
                    return;
                }

                var index = _mapIndexService.CoordinatesToIndex(new Vector2Int(pointX, pointY));

                if (logDebug)
                {
                    WaterAbsorptionPlugin.Log.LogInfo($"index: {index}");
                }

                moistureLevels[cardinalDirection] = _soilMoistureSimulator.MoistureLevels[index];
            }

            var highest = moistureLevels.OrderByDescending(x => x.Value).First();

            if (highest.Value == 0)
            {
                if (logDebug)
                {
                    WaterAbsorptionPlugin.Log.LogWarning("IT IS DRY!!!");
                }

                return;
            }

            if (logDebug)
            {
                foreach (var item in moistureLevels)
                {
                    WaterAbsorptionPlugin.Log.LogInfo($"index: {item.Key} Value: {item.Value}");
                }
                WaterAbsorptionPlugin.Log.LogInfo($"Direction: {highest.Key} Value: {highest.Value}");
            }

            var direction = highest.Key;

            (int x, int y) directionValues = Direction.CardinalDirectionXY[direction];

            if (directionValues.x == 0 && directionValues.y == 0)
            {
                if (logDebug)
                {
                    WaterAbsorptionPlugin.Log.LogWarning($"directionValues are ZEROs");
                }
            }

            var newX = _blockObject.Coordinates.x + directionValues.x;
            var newY = _blockObject.Coordinates.y + directionValues.y;

            Find(ref found, ref isIrrigationTower, newX, newY, 0, direction);
        }

        private void Find(ref bool found, ref bool isIrrigationTower, int x, int y, short depth, Direction.CardinalDirection cardinalDirection)
        {
            depth++;

            var highest = 0f;

            foreach (var point in Direction.CardinalDirectionPoints[cardinalDirection])
            {
                var xy = Direction.CardinalDirectionXY[point];
                var pointX = x + xy.x;
                var pointY = y + xy.y;

                (highest, cardinalDirection) = ProccessCardinalDirection(pointX, pointY, ref found, ref isIrrigationTower, highest, cardinalDirection, point);
            }

            if (ShouldStopSearch(highest, depth))
            {
                return;
            }

            var newXY = Direction.CardinalDirectionXY[cardinalDirection];
            Find(ref found, ref isIrrigationTower, x + newXY.x, y + newXY.y, depth, cardinalDirection);
        }

        private (float highest, Direction.CardinalDirection direction) ProccessCardinalDirection(int pointX, int pointY, ref bool found, ref bool isIrrigationTower, float highest, Direction.CardinalDirection oldDirection, Direction.CardinalDirection newDirection)
        {
            if (!IsInsideMap(pointX, pointY))
            {
                return (highest, oldDirection);
            }

            CheckIfWaterOrIrrigator(ref found, ref isIrrigationTower, pointX, pointY);
            if (found)
            {
                return (highest, oldDirection);
            }

            var level = GetWaterLevel(pointX, pointY);

            if (level > highest)
            {
                return (level, newDirection);
            }

            return (highest, oldDirection);
        }

        private float GetWaterLevel(int pointX, int pointY)
        {
            var index = _mapIndexService.CoordinatesToIndex(new Vector2Int(pointX, pointY));
            return _soilMoistureSimulator.MoistureLevels[index];
        }

        private bool IsInsideMap(int pointX, int pointY)
        {
            return pointX >= 0 && pointY >= 0 && pointX <= _mapIndexService.MapSize.x && pointY <= _mapIndexService.MapSize.y;
        }

        private bool ShouldStopSearch(float highest, short depth)
        {
            return highest == 0 || depth > WaterAbsorptionPlugin.Config.MaxSearchDepth;
        }

        private void CheckIfWaterOrIrrigator(ref bool found, ref bool isIrrigationTower, int x, int y)
        {
            if (WaterService._wateredMap[y][x])
            {
                found = true;
                _registeredGrowable._cachedX = x;
                _registeredGrowable._cachedY = y;
                return;
            }

            if (RegisteredIrrigator._irrigationTowerLocations.Any(item => item.x == x && item.y == y))
            {
                found = true;
                isIrrigationTower = true;
                _registeredGrowable._cachedX = x;
                _registeredGrowable._cachedY = y;
                return;
            }
        }

        private void LogAround(int x, int y, short direction, float highest, int pointX, int pointY)
        {
            if (logDebug)
            {
                WaterAbsorptionPlugin.Log.LogWarning($"MAX DEPTH SEARCH RANGE REACHED!!! OriX: {_blockObject.Coordinates.x} OriY: {_blockObject.Coordinates.y} Dir: {direction} Highest: {highest} x: {x} y: {y}");

                //var index = _mapIndexService.CoordinatesToIndex(new Vector2Int(pointX + _north.x, pointY + _north.y));
                //var level = _soilMoistureSimulator.MoistureLevels[index];
                //WaterAbsorptionPlugin.Log.LogInfo($"_north: {level}");
                //index = _mapIndexService.CoordinatesToIndex(new Vector2Int(pointX + _east.x, pointY + _east.y));
                //level = _soilMoistureSimulator.MoistureLevels[index];
                //WaterAbsorptionPlugin.Log.LogInfo($"_east: {level}");
                //index = _mapIndexService.CoordinatesToIndex(new Vector2Int(pointX + _west.x, pointY + _west.y));
                //level = _soilMoistureSimulator.MoistureLevels[index];
                //WaterAbsorptionPlugin.Log.LogInfo($"_west: {level}");
                //index = _mapIndexService.CoordinatesToIndex(new Vector2Int(pointX + _south.x, pointY + _south.y));
                //level = _soilMoistureSimulator.MoistureLevels[index];
                //WaterAbsorptionPlugin.Log.LogInfo($"_south: {level}");
                //index = _mapIndexService.CoordinatesToIndex(new Vector2Int(pointX + _northeast.x, pointY + _northeast.y));
                //level = _soilMoistureSimulator.MoistureLevels[index];
                //WaterAbsorptionPlugin.Log.LogInfo($"_northeast: {level}");
                //index = _mapIndexService.CoordinatesToIndex(new Vector2Int(pointX + _northwest.x, pointY + _northwest.y));
                //level = _soilMoistureSimulator.MoistureLevels[index];
                //WaterAbsorptionPlugin.Log.LogInfo($"_northwest: {level}");
                //index = _mapIndexService.CoordinatesToIndex(new Vector2Int(pointX + _southeast.x, pointY + _southeast.y));
                //level = _soilMoistureSimulator.MoistureLevels[index];
                //WaterAbsorptionPlugin.Log.LogInfo($"_southeast: {level}");
                //index = _mapIndexService.CoordinatesToIndex(new Vector2Int(pointX + _southwest.x, pointY + _southwest.y));
                //level = _soilMoistureSimulator.MoistureLevels[index];
                //WaterAbsorptionPlugin.Log.LogInfo($"_southwest: {level}");
            }
        }
    }
}
