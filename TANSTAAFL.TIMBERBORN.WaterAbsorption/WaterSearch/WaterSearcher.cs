using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TANSTAAFL.TIMBERBORN.WaterAbsorption.Config;
using Timberborn.BlockSystem;
using Timberborn.MapIndexSystem;
using Timberborn.SoilMoistureSystem;
using UnityEngine;
using UnityEngine.UIElements;

namespace TANSTAAFL.TIMBERBORN.WaterAbsorption.WaterSearch
{
    internal class WaterSearcher
    {
        private RegisteredGrowable _registeredGrowable;
        private static MapIndexService _mapIndexService;
        private static SoilMoistureSimulator _soilMoistureSimulator;

        internal WaterSearcher(RegisteredGrowable registeredGrowable, MapIndexService mapIndexService, SoilMoistureSimulator soilMoistureSimulator)
        {
            _registeredGrowable = registeredGrowable;
            _mapIndexService = mapIndexService;
            _soilMoistureSimulator = soilMoistureSimulator;
        }

        internal (bool foundWater, bool waterIsIrrigationTower) FindLocationAdvanced()
        {
            var moistureLevels = new Dictionary<Direction.CardinalDirection, float>();

            for (short i = 0; i < 8; i++)
            {
                var cardinalDirection = (Direction.CardinalDirection)i;
                var (x, y) = GetNeighborCoordinates(cardinalDirection);

                if (!IsInsideMap(x, y)) continue;

                var (foundWater, waterIsIrrigationTower) = CheckIfWaterOrIrrigationTower(x, y);
                if (foundWater) return (true, waterIsIrrigationTower);

                var moistureLevel = GetMoistureLevel(x, y);
                moistureLevels[cardinalDirection] = moistureLevel;
            }

            var highestMoistureDirection = GetHighestMoistureDirection(moistureLevels);
            if (highestMoistureDirection == null) return (false, false);

            var (newX, newY) = GetNeighborCoordinates(highestMoistureDirection.Value);
            return Find(newX, newY, 0, highestMoistureDirection.Value);
        }

        private (bool foundWater, bool waterIsIrrigationTower) Find(int x, int y, short depth, Direction.CardinalDirection cardinalDirection)
        {
            depth++;

            var highest = 0f;

            foreach (var point in Direction.CardinalDirectionPoints[cardinalDirection])
            {
                var xy = Direction.CardinalDirectionXY[point];
                var pointX = x + xy.x;
                var pointY = y + xy.y;

                bool foundWater = false;
                bool waterIsIrrigationTower = false;
                (foundWater, waterIsIrrigationTower, highest, cardinalDirection) = ProccessCardinalDirection(pointX, pointY, highest, cardinalDirection, point);
                if (foundWater)
                {
                    return (foundWater, waterIsIrrigationTower);
                }
            }

            if (ShouldStopSearch(highest, depth))
            {
                return (false, false);
            }

            var newXY = Direction.CardinalDirectionXY[cardinalDirection];
            return Find(x + newXY.x, y + newXY.y, depth, cardinalDirection);
        }

        private (bool foundWater, bool waterIsIrrigationTower, float highest, Direction.CardinalDirection direction) ProccessCardinalDirection(int pointX, int pointY, float highest, Direction.CardinalDirection oldDirection, Direction.CardinalDirection newDirection)
        {
            if (!IsInsideMap(pointX, pointY))
            {
                return (false, false, highest, oldDirection);
            }

            var (foundWater, waterIsIrrigationTower) = CheckIfWaterOrIrrigationTower(pointX, pointY);
            if (foundWater)
            {
                return (foundWater, waterIsIrrigationTower, highest, oldDirection);
            }

            var level = GetMoistureLevel(pointX, pointY);

            if (level > highest)
            {
                return (false, false, level, newDirection);
            }

            return (false, false, highest, oldDirection);
        }

        private float GetMoistureLevel(int pointX, int pointY)
        {
            var index = _mapIndexService.CoordinatesToIndex(new Vector2Int(pointX, pointY));
            return _soilMoistureSimulator.MoistureLevels[index];
        }

        private bool IsInsideMap(int pointX, int pointY)
        {
            return pointX >= 0 && pointY >= 0 && pointX <= _mapIndexService.MapSize.x && pointY <= _mapIndexService.MapSize.y;
        }

        private Direction.CardinalDirection? GetHighestMoistureDirection(Dictionary<Direction.CardinalDirection, float> moistureLevels)
        {
            var highestMoistureLevel = moistureLevels.OrderByDescending(x => x.Value).First();

            if (highestMoistureLevel.Value == 0)
            {
                return null;
            }

            return highestMoistureLevel.Key;
        }

        private (int x, int y) GetNeighborCoordinates(Direction.CardinalDirection direction)
        {
            var neighborXY = Direction.CardinalDirectionXY[direction];
            return (_registeredGrowable._blockObject.Coordinates.x + neighborXY.x, _registeredGrowable._blockObject.Coordinates.y + neighborXY.y);
        }

        private bool ShouldStopSearch(float highest, short depth)
        {
            return highest == 0 || depth > WaterAbsorptionConfigLoader._savedConfig.MaxSearchDepth;
        }

        private (bool foundWater, bool waterIsIrrigationTower) CheckIfWaterOrIrrigationTower(int x, int y)
        {
            if (WaterService._wateredMap[y][x])
            {
                _registeredGrowable._cachedX = x;
                _registeredGrowable._cachedY = y;
                return (true, false);
            }

            if (IrrigatorHandler._irrigationTowerLocations.Any(item => item.x == x && item.y == y))
            {
                _registeredGrowable._cachedX = x;
                _registeredGrowable._cachedY = y;
                return (true, true);
            }

            return (false, false);
        }

        private void LogAround(int x, int y, short direction, float highest, int pointX, int pointY)
        {
            //if (logDebug)
            //{
            //    WaterAbsorptionPlugin.Log.LogWarning($"MAX DEPTH SEARCH RANGE REACHED!!! OriX: {_blockObject.Coordinates.x} OriY: {_blockObject.Coordinates.y} Dir: {direction} Highest: {highest} x: {x} y: {y}");

            //    var index = _mapIndexService.CoordinatesToIndex(new Vector2Int(pointX + _north.x, pointY + _north.y));
            //    var level = _soilMoistureSimulator.MoistureLevels[index];
            //    WaterAbsorptionPlugin.Log.LogInfo($"_north: {level}");
            //    index = _mapIndexService.CoordinatesToIndex(new Vector2Int(pointX + _east.x, pointY + _east.y));
            //    level = _soilMoistureSimulator.MoistureLevels[index];
            //    WaterAbsorptionPlugin.Log.LogInfo($"_east: {level}");
            //    index = _mapIndexService.CoordinatesToIndex(new Vector2Int(pointX + _west.x, pointY + _west.y));
            //    level = _soilMoistureSimulator.MoistureLevels[index];
            //    WaterAbsorptionPlugin.Log.LogInfo($"_west: {level}");
            //    index = _mapIndexService.CoordinatesToIndex(new Vector2Int(pointX + _south.x, pointY + _south.y));
            //    level = _soilMoistureSimulator.MoistureLevels[index];
            //    WaterAbsorptionPlugin.Log.LogInfo($"_south: {level}");
            //    index = _mapIndexService.CoordinatesToIndex(new Vector2Int(pointX + _northeast.x, pointY + _northeast.y));
            //    level = _soilMoistureSimulator.MoistureLevels[index];
            //    WaterAbsorptionPlugin.Log.LogInfo($"_northeast: {level}");
            //    index = _mapIndexService.CoordinatesToIndex(new Vector2Int(pointX + _northwest.x, pointY + _northwest.y));
            //    level = _soilMoistureSimulator.MoistureLevels[index];
            //    WaterAbsorptionPlugin.Log.LogInfo($"_northwest: {level}");
            //    index = _mapIndexService.CoordinatesToIndex(new Vector2Int(pointX + _southeast.x, pointY + _southeast.y));
            //    level = _soilMoistureSimulator.MoistureLevels[index];
            //    WaterAbsorptionPlugin.Log.LogInfo($"_southeast: {level}");
            //    index = _mapIndexService.CoordinatesToIndex(new Vector2Int(pointX + _southwest.x, pointY + _southwest.y));
            //    level = _soilMoistureSimulator.MoistureLevels[index];
            //    WaterAbsorptionPlugin.Log.LogInfo($"_southwest: {level}");
            //}
        }
    }
}
