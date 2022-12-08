using Bindito.Core;
using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TANSTAAFL.TIMBERBORN.WaterAbsorption.TickTracker;
using Timberborn.BlockSystem;
using Timberborn.Core;
using Timberborn.Cutting;
using Timberborn.EntitySystem;
using Timberborn.Gathering;
using Timberborn.GoodConsumingBuildingSystem;
using Timberborn.Growing;
using Timberborn.MapIndexSystem;
using Timberborn.NaturalResources;
using Timberborn.SingletonSystem;
using Timberborn.SoilMoistureSystem;
using Timberborn.WaterSystem;
using Timberborn.Goods;
using UnityEngine;
using Timberborn.IrrigationSystem;
using System.IO;
using Timberborn.Common;

namespace TANSTAAFL.TIMBERBORN.WaterAbsorption
{
    public class RegisteredGrowable : MonoBehaviour, IRegisteredComponent
    {
        internal Growable _growable;
        private DryObject _dryObject;
        private BlockObject _blockObject;
        private int? _cachedX;
        private int? _cachedY;
        private short _cacheAge = 0;
        private System.Random _random = new System.Random();

        private static EntityComponentRegistry _entityComponentRegistry;
        private static MapIndexService _mapIndexService;
        private static WaterSimulator _waterSimulator;
        private static WaterMap _waterMap;
        private static SoilMoistureSimulator _soilMoistureSimulator;

        private static readonly (int x, int y) _north = (0, -1);
        private static readonly (int x, int y) _east = (-1, 0);
        private static readonly (int x, int y) _west = (1, 0);
        private static readonly (int x, int y) _south = (0, 1);
        private static readonly (int x, int y) _northeast = (-1, -1);
        private static readonly (int x, int y) _northwest = (1, -1);
        private static readonly (int x, int y) _southeast = (-1, 1);
        private static readonly (int x, int y) _southwest = (1, 1);

        private static Dictionary<int, Dictionary<int, float>> _irrigationAccumulator = new Dictionary<int, Dictionary<int, float>>();

        public static Dictionary<short, string> GrowableTypeOrder = new Dictionary<short, string>();

        internal static float[][] _soilMap;

        private static bool logDebug = false;

        [Inject]
        public void InjectDependencies(EntityComponentRegistry entityComponentRegistry, MapIndexService mapIndexService, WaterSimulator waterSimulator, WaterMap waterMap, SoilMoistureSimulator soilMoistureSimulator)
        {
            _entityComponentRegistry = entityComponentRegistry;
            _mapIndexService = mapIndexService;
            _waterSimulator = waterSimulator;
            _waterMap = waterMap;
            _soilMoistureSimulator = soilMoistureSimulator;
        }

        public void Awake()
        {
            _growable = GetComponent<Growable>();
            _dryObject = GetComponent<DryObject>();
            _blockObject = GetComponent<BlockObject>();
        }

        internal static void HandleGrowables(short currentTick)
        {
            var growables = _entityComponentRegistry
                .GetEnabled<RegisteredGrowable>()
                .Where(x => !x._dryObject.IsDry);

            if (!growables.Any())
            {
                return;
            }

            string growableType = GetGrowableType(growables, currentTick);

            if (string.IsNullOrEmpty(growableType))
            {
                return;
            }

            //WaterAbsorptionPlugin.Log.LogInfo($"GrowableType: {growableType} Count: {growables.Where(x => x._growable.name == growableType).Count()}");

            WaterService.GenerateWateredMap(_waterMap, _mapIndexService);
            RegisteredIrrigator.GenerateIrrigationTowerLocations();

            foreach (var growable in growables.Where(x => x._growable.name == growableType))
            {
                growable.ConsumeWater();
            }
        }

        public static string GetGrowableType(IEnumerable<RegisteredGrowable> growables, short currentTick)
        {
            if (GrowableTypeOrder.Keys.Contains(currentTick))
            {
                return GrowableTypeOrder[currentTick];
            }

            return GetNextGrowableType(growables, currentTick);
        }

        private static string GetNextGrowableType(IEnumerable<RegisteredGrowable> growables, short currentTick)
        {
            var next = growables.Select(x => x._growable.name).Distinct().Where(x => !GrowableTypeOrder.Values.Contains(x)).FirstOrDefault();

            if (next == null)
            {
                return null;
            }

            GrowableTypeOrder[currentTick] = next;

            return next;
        }

        internal void ConsumeWater()
        {
            if (_cacheAge > 3 && _random.Next(10 - _cacheAge) == 0)
            {
                _cacheAge = 0;
                _cachedX = null;
                _cachedY = null;
            }

            bool found = false;
            bool isIrrigationTower = false;
            CheckIfCached(ref found, ref isIrrigationTower);

            if (!found)
            {
                FindLocationAdvanced(ref found, ref isIrrigationTower);
            }

            if (!found)
            {
                WaterAbsorptionPlugin.Log.LogWarning($"NO WATER FOUND!!!");
                return;
            }

            if (isIrrigationTower)
            {
                ConsumeWaterFromIrrigator();
                return;
            }

            UpdateWaterDepth();
        }

        private void ConsumeWaterFromIrrigator()
        {
            var coordinates = RegisteredIrrigator._irrigationTowerEntranceLocations[new Vector2Int(_cachedX.Value, _cachedY.Value)];

            if (!_irrigationAccumulator.ContainsKey(coordinates.y))
            {
                _irrigationAccumulator[coordinates.y] = new Dictionary<int, float>();
            }

            if (!_irrigationAccumulator[coordinates.y].ContainsKey(coordinates.x))
            {
                _irrigationAccumulator[coordinates.y][coordinates.x] = 0;
            }

            _irrigationAccumulator[coordinates.y][coordinates.x] += WaterAbsorptionPlugin.Config.IrrigatorTickIncrement;

            if (_irrigationAccumulator[coordinates.y][coordinates.x] < 1)
            {
                return;
            }

            _irrigationAccumulator[coordinates.y][coordinates.x] = 0;

            var irrigator = RegisteredIrrigator.GetIrrigators()
                .Where(x => x._blockObject.Coordinates.x == coordinates.x && x._blockObject.Coordinates.y == coordinates.y)
                .FirstOrDefault();

            if (irrigator == null)
            {
                WaterAbsorptionPlugin.Log.LogWarning($"NO IRRIGATION TOWER FOUND!!!");
                return;
            }

            var amount = irrigator._goodConsumingBuilding.Inventory.AmountInStock("Water");
            if (amount == 0)
            {
                WaterAbsorptionPlugin.Log.LogWarning($"NO WATER IN IRRIGATION TOWER!!!");
                return;
            }

            irrigator._goodConsumingBuilding.Inventory.Take(new GoodAmount("Water", 1));
            if (irrigator._goodConsumingBuilding.Inventory.UnreservedAmountInStock(irrigator._goodConsumingBuilding._supply) <= 0)
            {
                irrigator._goodConsumingBuilding._supplyLeft = 0;
            }

            //WaterAbsorptionPlugin.Log.LogWarning($"Water consumed from irrigation tower");
        }

        private void FindLocationAdvanced(ref bool found, ref bool isIrrigationTower)
        {
            Dictionary<short, float> moistureLevels = new Dictionary<short, float>();

            for (short i = 0; i < 8; i++)
            {
                (int x, int y) item;
                switch (i)
                {
                    case 0:
                        item = _north;
                        break;
                    case 1:
                        item = _east;
                        break;
                    case 2:
                        item = _west;
                        break;
                    case 3:
                        item = _south;
                        break;
                    case 4:
                        item = _northeast;
                        break;
                    case 5:
                        item = _northwest;
                        break;
                    case 6:
                        item = _southeast;
                        break;
                    case 7:
                        item = _southwest;
                        break;
                    default:
                        throw new Exception("WHYYYYYYYYYYY");
                }

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

                moistureLevels[i] = _soilMoistureSimulator.MoistureLevels[index];
            }

            var highest = moistureLevels.OrderByDescending(x => x.Value).First();

            if (highest.Value == 0)
            {
                WaterAbsorptionPlugin.Log.LogWarning("IT IS DRY!!!");
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

            (int x, int y) directionValues;
            switch (direction)
            {
                case 0:
                    directionValues = _north;
                    break;
                case 1:
                    directionValues = _east;
                    break;
                case 2:
                    directionValues = _west;
                    break;
                case 3:
                    directionValues = _south;
                    break;
                case 4:
                    directionValues = _northeast;
                    break;
                case 5:
                    directionValues = _northwest;
                    break;
                case 6:
                    directionValues = _southeast;
                    break;
                case 7:
                    directionValues = _southwest;
                    break;
                default:
                    throw new Exception("WHYYYYYYYYYYY 2");
            }

            if (directionValues.x == 0 && directionValues.y == 0)
            {
                WaterAbsorptionPlugin.Log.LogWarning($"directionValues are ZEROs");
            }

            var newX = _blockObject.Coordinates.x + directionValues.x;
            var newY = _blockObject.Coordinates.y + directionValues.y;

            short depth = 0;

            switch (direction)
            {
                case 0:
                    FindN(ref found, ref isIrrigationTower, newX, newY, depth);
                    break;
                case 1:
                    FindE(ref found, ref isIrrigationTower, newX, newY, depth);
                    break;
                case 2:
                    FindW(ref found, ref isIrrigationTower, newX, newY, depth);
                    break;
                case 3:
                    FindS(ref found, ref isIrrigationTower, newX, newY, depth);
                    break;
                case 4:
                    FindNE(ref found, ref isIrrigationTower, newX, newY, depth);
                    break;
                case 5:
                    FindNW(ref found, ref isIrrigationTower, newX, newY, depth);
                    break;
                case 6:
                    FindSE(ref found, ref isIrrigationTower, newX, newY, depth);
                    break;
                case 7:
                    FindSW(ref found, ref isIrrigationTower, newX, newY, depth);
                    break;
            }
        }

        private void FindN(ref bool found, ref bool isIrrigationTower, int x, int y, short depth)
        {
            depth++;

            short direction = 0;
            var highest = 0f;

            var pointX = x + _north.x;
            var pointY = y + _north.y;

            if (pointX >= 0 && pointY >= 0 && pointX <= _mapIndexService.MapSize.x && pointY <= _mapIndexService.MapSize.y)
            {
                CheckIfWaterOrIrrigator(ref found, ref isIrrigationTower, pointX, pointY);
                if (found)
                {
                    return;
                }

                var index = _mapIndexService.CoordinatesToIndex(new Vector2Int(pointX, pointY));
                highest = _soilMoistureSimulator.MoistureLevels[index];
            }

            pointX = x + _northeast.x;
            pointY = y + _northeast.y;

            if (pointX >= 0 && pointY >= 0 && pointX <= _mapIndexService.MapSize.x && pointY <= _mapIndexService.MapSize.y)
            {
                CheckIfWaterOrIrrigator(ref found, ref isIrrigationTower, pointX, pointY);
                if (found)
                {
                    return;
                }

                var index = _mapIndexService.CoordinatesToIndex(new Vector2Int(pointX, pointY));
                var level = _soilMoistureSimulator.MoistureLevels[index];

                if (level > highest)
                {
                    highest = level;
                    direction = 1;
                }    
            }

            pointX = x + _northwest.x;
            pointY = y + _northwest.y;

            if (pointX >= 0 && pointY >= 0 && pointX <= _mapIndexService.MapSize.x && pointY <= _mapIndexService.MapSize.y)
            {
                CheckIfWaterOrIrrigator(ref found, ref isIrrigationTower, pointX, pointY);
                if (found)
                {
                    return;
                }

                var index = _mapIndexService.CoordinatesToIndex(new Vector2Int(pointX, pointY));
                var level = _soilMoistureSimulator.MoistureLevels[index];

                if (level > highest)
                {
                    highest = level;
                    direction = 2;
                }
            }

            if (highest == 0)
            {
                WaterAbsorptionPlugin.Log.LogWarning("IT IS DRY!!!");
                return;
            }

            if (depth > WaterAbsorptionPlugin.Config.MaxSearchDepth)
            {
                LogAround(x, y, direction, highest, pointX, pointY);
                return;
            }

            if (logDebug)
            {
                WaterAbsorptionPlugin.Log.LogInfo($"Direction: {direction} Value: {highest} Depth: {depth}");
            }

            switch (direction)
            {
                case 0:
                    FindN(ref found, ref isIrrigationTower, x + _north.x, y + _north.y, depth);
                    break;
                case 1:
                    FindNE(ref found, ref isIrrigationTower, x + _northeast.x, y + _northeast.y, depth);
                    break;
                case 2:
                    FindNW(ref found, ref isIrrigationTower, x + _northwest.x, y + _northwest.y, depth);
                    break;
            }
        }

        private void FindE(ref bool found, ref bool isIrrigationTower, int x, int y, short depth)
        {
            depth++;
            short direction = 0;
            var highest = 0f;

            var pointX = x + _east.x;
            var pointY = y + _east.y;

            if (pointX >= 0 && pointY >= 0 && pointX <= _mapIndexService.MapSize.x && pointY <= _mapIndexService.MapSize.y)
            {
                CheckIfWaterOrIrrigator(ref found, ref isIrrigationTower, pointX, pointY);
                if (found)
                {
                    return;
                }

                var index = _mapIndexService.CoordinatesToIndex(new Vector2Int(pointX, pointY));
                highest = _soilMoistureSimulator.MoistureLevels[index];
            }

            pointX = x + _northeast.x;
            pointY = y + _northeast.y;

            if (pointX >= 0 && pointY >= 0 && pointX <= _mapIndexService.MapSize.x && pointY <= _mapIndexService.MapSize.y)
            {
                CheckIfWaterOrIrrigator(ref found, ref isIrrigationTower, pointX, pointY);
                if (found)
                {
                    return;
                }

                var index = _mapIndexService.CoordinatesToIndex(new Vector2Int(pointX, pointY));
                var level = _soilMoistureSimulator.MoistureLevels[index];

                if (level > highest)
                {
                    highest = level;
                    direction = 1;
                }
            }

            pointX = x + _southeast.x;
            pointY = y + _southeast.y;

            if (pointX >= 0 && pointY >= 0 && pointX <= _mapIndexService.MapSize.x && pointY <= _mapIndexService.MapSize.y)
            {
                CheckIfWaterOrIrrigator(ref found, ref isIrrigationTower, pointX, pointY);
                if (found)
                {
                    return;
                }

                var index = _mapIndexService.CoordinatesToIndex(new Vector2Int(pointX, pointY));
                var level = _soilMoistureSimulator.MoistureLevels[index];

                if (level > highest)
                {
                    highest = level;
                    direction = 2;
                }
            }

            if (highest == 0)
            {
                WaterAbsorptionPlugin.Log.LogWarning("IT IS DRY!!!");
                return;
            }

            if (depth > WaterAbsorptionPlugin.Config.MaxSearchDepth)
            {
                LogAround(x, y, direction, highest, pointX, pointY);
                return;
            }

            if (logDebug)
            {
                WaterAbsorptionPlugin.Log.LogInfo($"Direction: {direction} Value: {highest} Depth: {depth}");
            }

            switch (direction)
            {
                case 0:
                    FindE(ref found, ref isIrrigationTower, x + _east.x, y + _east.y, depth);
                    break;
                case 1:
                    FindNE(ref found, ref isIrrigationTower, x + _northeast.x, y + _northeast.y, depth);
                    break;
                case 2:
                    FindSE(ref found, ref isIrrigationTower, x + _southeast.x, y + _southeast.y, depth);
                    break;
            }
        }

        private void FindW(ref bool found, ref bool isIrrigationTower, int x, int y, short depth)
        {
            depth++;

            short direction = 0;
            var highest = 0f;

            var pointX = x + _west.x;
            var pointY = y + _west.y;

            if (pointX >= 0 && pointY >= 0 && pointX <= _mapIndexService.MapSize.x && pointY <= _mapIndexService.MapSize.y)
            {
                CheckIfWaterOrIrrigator(ref found, ref isIrrigationTower, pointX, pointY);
                if (found)
                {
                    return;
                }

                var index = _mapIndexService.CoordinatesToIndex(new Vector2Int(pointX, pointY));
                highest = _soilMoistureSimulator.MoistureLevels[index];
            }

            pointX = x + _northwest.x;
            pointY = y + _northwest.y;

            if (pointX >= 0 && pointY >= 0 && pointX <= _mapIndexService.MapSize.x && pointY <= _mapIndexService.MapSize.y)
            {
                CheckIfWaterOrIrrigator(ref found, ref isIrrigationTower, pointX, pointY);
                if (found)
                {
                    return;
                }

                var index = _mapIndexService.CoordinatesToIndex(new Vector2Int(pointX, pointY));
                var level = _soilMoistureSimulator.MoistureLevels[index];

                if (level > highest)
                {
                    highest = level;
                    direction = 1;
                }
            }

            pointX = x + _southwest.x;
            pointY = y + _southwest.y;

            if (pointX >= 0 && pointY >= 0 && pointX <= _mapIndexService.MapSize.x && pointY <= _mapIndexService.MapSize.y)
            {
                CheckIfWaterOrIrrigator(ref found, ref isIrrigationTower, pointX, pointY);
                if (found)
                {
                    return;
                }

                var index = _mapIndexService.CoordinatesToIndex(new Vector2Int(pointX, pointY));
                var level = _soilMoistureSimulator.MoistureLevels[index];

                if (level > highest)
                {
                    highest = level;
                    direction = 2;
                }
            }

            if (highest == 0)
            {
                WaterAbsorptionPlugin.Log.LogWarning("IT IS DRY!!!");
                return;
            }

            if (depth > WaterAbsorptionPlugin.Config.MaxSearchDepth)
            {
                LogAround(x, y, direction, highest, pointX, pointY);
                return;
            }

            if (logDebug)
            {
                WaterAbsorptionPlugin.Log.LogInfo($"Direction: {direction} Value: {highest} Depth: {depth}");
            }

            switch (direction)
            {
                case 0:
                    FindW(ref found, ref isIrrigationTower, x + _west.x, y + _west.y, depth);
                    break;
                case 1:
                    FindNW(ref found, ref isIrrigationTower, x + _northwest.x, y + _northwest.y, depth);
                    break;
                case 2:
                    FindSW(ref found, ref isIrrigationTower, x + _southwest.x, y + _southwest.y, depth);
                    break;
            }
        }

        private void FindS(ref bool found, ref bool isIrrigationTower, int x, int y, short depth)
        {
            depth++;

            short direction = 0;
            var highest = 0f;

            var pointX = x + _south.x;
            var pointY = y + _south.y;

            if (pointX >= 0 && pointY >= 0 && pointX <= _mapIndexService.MapSize.x && pointY <= _mapIndexService.MapSize.y)
            {
                CheckIfWaterOrIrrigator(ref found, ref isIrrigationTower, pointX, pointY);
                if (found)
                {
                    return;
                }

                var index = _mapIndexService.CoordinatesToIndex(new Vector2Int(pointX, pointY));
                highest = _soilMoistureSimulator.MoistureLevels[index];
            }

            pointX = x + _southeast.x;
            pointY = y + _southeast.y;

            if (pointX >= 0 && pointY >= 0 && pointX <= _mapIndexService.MapSize.x && pointY <= _mapIndexService.MapSize.y)
            {
                CheckIfWaterOrIrrigator(ref found, ref isIrrigationTower, pointX, pointY);
                if (found)
                {
                    return;
                }

                var index = _mapIndexService.CoordinatesToIndex(new Vector2Int(pointX, pointY));
                var level = _soilMoistureSimulator.MoistureLevels[index];

                if (level > highest)
                {
                    highest = level;
                    direction = 1;
                }
            }

            pointX = x + _southwest.x;
            pointY = y + _southwest.y;

            if (pointX >= 0 && pointY >= 0 && pointX <= _mapIndexService.MapSize.x && pointY <= _mapIndexService.MapSize.y)
            {
                CheckIfWaterOrIrrigator(ref found, ref isIrrigationTower, pointX, pointY);
                if (found)
                {
                    return;
                }

                var index = _mapIndexService.CoordinatesToIndex(new Vector2Int(pointX, pointY));
                var level = _soilMoistureSimulator.MoistureLevels[index];

                if (level > highest)
                {
                    highest = level;
                    direction = 2;
                }
            }

            if (highest == 0)
            {
                WaterAbsorptionPlugin.Log.LogWarning("IT IS DRY!!!");
                return;
            }

            if (depth > WaterAbsorptionPlugin.Config.MaxSearchDepth)
            {
                LogAround(x, y, direction, highest, pointX, pointY);
                return;
            }

            if (logDebug)
            {
                WaterAbsorptionPlugin.Log.LogInfo($"Direction: {direction} Value: {highest} Depth: {depth}");
            }

            switch (direction)
            {
                case 0:
                    FindS(ref found, ref isIrrigationTower, x + _south.x, y + _south.y, depth);
                    break;
                case 1:
                    FindSE(ref found, ref isIrrigationTower, x + _southeast.x, y + _southeast.y, depth);
                    break;
                case 2:
                    FindSW(ref found, ref isIrrigationTower, x + _southwest.x, y + _southwest.y, depth);
                    break;
            }
        }

        private void FindNE(ref bool found, ref bool isIrrigationTower, int x, int y, short depth)
        {
            depth++;

            short direction = 0;
            var highest = 0f;

            var pointX = x + _northeast.x;
            var pointY = y + _northeast.y;

            if (pointX >= 0 && pointY >= 0 && pointX <= _mapIndexService.MapSize.x && pointY <= _mapIndexService.MapSize.y)
            {
                CheckIfWaterOrIrrigator(ref found, ref isIrrigationTower, pointX, pointY);
                if (found)
                {
                    return;
                }

                var index = _mapIndexService.CoordinatesToIndex(new Vector2Int(pointX, pointY));
                highest = _soilMoistureSimulator.MoistureLevels[index];
            }

            pointX = x + _north.x;
            pointY = y + _north.y;

            if (pointX >= 0 && pointY >= 0 && pointX <= _mapIndexService.MapSize.x && pointY <= _mapIndexService.MapSize.y)
            {
                CheckIfWaterOrIrrigator(ref found, ref isIrrigationTower, pointX, pointY);
                if (found)
                {
                    return;
                }

                var index = _mapIndexService.CoordinatesToIndex(new Vector2Int(pointX, pointY));
                var level = _soilMoistureSimulator.MoistureLevels[index];

                if (level > highest)
                {
                    highest = level;
                    direction = 1;
                }
            }

            pointX = x + _east.x;
            pointY = y + _east.y;

            if (pointX >= 0 && pointY >= 0 && pointX <= _mapIndexService.MapSize.x && pointY <= _mapIndexService.MapSize.y)
            {
                CheckIfWaterOrIrrigator(ref found, ref isIrrigationTower, pointX, pointY);
                if (found)
                {
                    return;
                }

                var index = _mapIndexService.CoordinatesToIndex(new Vector2Int(pointX, pointY));
                var level = _soilMoistureSimulator.MoistureLevels[index];

                if (level > highest)
                {
                    highest = level;
                    direction = 2;
                }
            }

            if (highest == 0)
            {
                WaterAbsorptionPlugin.Log.LogWarning("IT IS DRY!!!");
                return;
            }

            if (depth > WaterAbsorptionPlugin.Config.MaxSearchDepth)
            {
                LogAround(x, y, direction, highest, pointX, pointY);
                return;
            }

            if (logDebug)
            {
                WaterAbsorptionPlugin.Log.LogInfo($"Direction: {direction} Value: {highest} Depth: {depth}");
            }

            switch (direction)
            {
                case 0:
                    FindNE(ref found, ref isIrrigationTower, x + _northeast.x, y + _northeast.y, depth);
                    break;
                case 1:
                    FindN(ref found, ref isIrrigationTower, x + _north.x, y + _north.y, depth);
                    break;
                case 2:
                    FindE(ref found, ref isIrrigationTower, x + _east.x, y + _east.y, depth);
                    break;
            }
        }

        private void FindNW(ref bool found, ref bool isIrrigationTower, int x, int y, short depth)
        {
            depth++;

            short direction = 0;
            var highest = 0f;

            var pointX = x + _northwest.x;
            var pointY = y + _northwest.y;

            if (pointX >= 0 && pointY >= 0 && pointX <= _mapIndexService.MapSize.x && pointY <= _mapIndexService.MapSize.y)
            {
                CheckIfWaterOrIrrigator(ref found, ref isIrrigationTower, pointX, pointY);
                if (found)
                {
                    return;
                }

                var index = _mapIndexService.CoordinatesToIndex(new Vector2Int(pointX, pointY));
                highest = _soilMoistureSimulator.MoistureLevels[index];
            }

            pointX = x + _north.x;
            pointY = y + _north.y;

            if (pointX >= 0 && pointY >= 0 && pointX <= _mapIndexService.MapSize.x && pointY <= _mapIndexService.MapSize.y)
            {
                CheckIfWaterOrIrrigator(ref found, ref isIrrigationTower, pointX, pointY);
                if (found)
                {
                    return;
                }

                var index = _mapIndexService.CoordinatesToIndex(new Vector2Int(pointX, pointY));
                var level = _soilMoistureSimulator.MoistureLevels[index];

                if (level > highest)
                {
                    highest = level;
                    direction = 1;
                }
            }

            pointX = x + _west.x;
            pointY = y + _west.y;

            if (pointX >= 0 && pointY >= 0 && pointX <= _mapIndexService.MapSize.x && pointY <= _mapIndexService.MapSize.y)
            {
                CheckIfWaterOrIrrigator(ref found, ref isIrrigationTower, pointX, pointY);
                if (found)
                {
                    return;
                }

                var index = _mapIndexService.CoordinatesToIndex(new Vector2Int(pointX, pointY));
                var level = _soilMoistureSimulator.MoistureLevels[index];

                if (level > highest)
                {
                    highest = level;
                    direction = 2;
                }
            }

            if (highest == 0)
            {
                WaterAbsorptionPlugin.Log.LogWarning("IT IS DRY!!!");
                return;
            }

            if (depth > WaterAbsorptionPlugin.Config.MaxSearchDepth)
            {
                LogAround(x, y, direction, highest, pointX, pointY);
                return;
            }

            if (logDebug)
            {
                WaterAbsorptionPlugin.Log.LogInfo($"Direction: {direction} Value: {highest} Depth: {depth}");
            }

            switch (direction)
            {
                case 0:
                    FindNW(ref found, ref isIrrigationTower, x + _northwest.x, y + _northwest.y, depth);
                    break;
                case 1:
                    FindN(ref found, ref isIrrigationTower, x + _north.x, y + _north.y, depth);
                    break;
                case 2:
                    FindW(ref found, ref isIrrigationTower, x + _west.x, y + _west.y, depth);
                    break;
            }
        }

        private void FindSE(ref bool found, ref bool isIrrigationTower, int x, int y, short depth)
        {
            depth++;

            short direction = 0;
            var highest = 0f;

            var pointX = x + _southeast.x;
            var pointY = y + _southeast.y;

            if (pointX >= 0 && pointY >= 0 && pointX <= _mapIndexService.MapSize.x && pointY <= _mapIndexService.MapSize.y)
            {
                CheckIfWaterOrIrrigator(ref found, ref isIrrigationTower, pointX, pointY);
                if (found)
                {
                    return;
                }

                var index = _mapIndexService.CoordinatesToIndex(new Vector2Int(pointX, pointY));
                highest = _soilMoistureSimulator.MoistureLevels[index];
            }

            pointX = x + _south.x;
            pointY = y + _south.y;

            if (pointX >= 0 && pointY >= 0 && pointX <= _mapIndexService.MapSize.x && pointY <= _mapIndexService.MapSize.y)
            {
                CheckIfWaterOrIrrigator(ref found, ref isIrrigationTower, pointX, pointY);
                if (found)
                {
                    return;
                }

                var index = _mapIndexService.CoordinatesToIndex(new Vector2Int(pointX, pointY));
                var level = _soilMoistureSimulator.MoistureLevels[index];

                if (level > highest)
                {
                    highest = level;
                    direction = 1;
                }
            }

            pointX = x + _east.x;
            pointY = y + _east.y;

            if (pointX >= 0 && pointY >= 0 && pointX <= _mapIndexService.MapSize.x && pointY <= _mapIndexService.MapSize.y)
            {
                CheckIfWaterOrIrrigator(ref found, ref isIrrigationTower, pointX, pointY);
                if (found)
                {
                    return;
                }

                var index = _mapIndexService.CoordinatesToIndex(new Vector2Int(pointX, pointY));
                var level = _soilMoistureSimulator.MoistureLevels[index];

                if (level > highest)
                {
                    highest = level;
                    direction = 2;
                }
            }

            if (highest == 0)
            {
                WaterAbsorptionPlugin.Log.LogWarning("IT IS DRY!!!");
                return;
            }

            if (depth > WaterAbsorptionPlugin.Config.MaxSearchDepth)
            {
                LogAround(x, y, direction, highest, pointX, pointY);
                return;
            }

            if (logDebug)
            {
                WaterAbsorptionPlugin.Log.LogInfo($"Direction: {direction} Value: {highest} Depth: {depth}");
            }

            switch (direction)
            {
                case 0:
                    FindSE(ref found, ref isIrrigationTower, x + _southeast.x, y + _southeast.y, depth);
                    break;
                case 1:
                    FindS(ref found, ref isIrrigationTower, x + _south.x, y + _south.y, depth);
                    break;
                case 2:
                    FindE(ref found, ref isIrrigationTower, x + _east.x, y + _east.y, depth);
                    break;
            }
        }

        private void FindSW(ref bool found, ref bool isIrrigationTower, int x, int y, short depth)
        {
            depth++;

            short direction = 0;
            var highest = 0f;

            var pointX = x + _southwest.x;
            var pointY = y + _southwest.y;

            if (pointX >= 0 && pointY >= 0 && pointX <= _mapIndexService.MapSize.x && pointY <= _mapIndexService.MapSize.y)
            {
                CheckIfWaterOrIrrigator(ref found, ref isIrrigationTower, pointX, pointY);
                if (found)
                {
                    return;
                }

                var index = _mapIndexService.CoordinatesToIndex(new Vector2Int(pointX, pointY));
                highest = _soilMoistureSimulator.MoistureLevels[index];
            }

            pointX = x + _south.x;
            pointY = y + _south.y;

            if (pointX >= 0 && pointY >= 0 && pointX <= _mapIndexService.MapSize.x && pointY <= _mapIndexService.MapSize.y)
            {
                CheckIfWaterOrIrrigator(ref found, ref isIrrigationTower, pointX, pointY);
                if (found)
                {
                    return;
                }

                var index = _mapIndexService.CoordinatesToIndex(new Vector2Int(pointX, pointY));
                var level = _soilMoistureSimulator.MoistureLevels[index];

                if (level > highest)
                {
                    highest = level;
                    direction = 1;
                }
            }

            pointX = x + _west.x;
            pointY = y + _west.y;

            if (pointX >= 0 && pointY >= 0 && pointX <= _mapIndexService.MapSize.x && pointY <= _mapIndexService.MapSize.y)
            {
                CheckIfWaterOrIrrigator(ref found, ref isIrrigationTower, pointX, pointY);
                if (found)
                {
                    return;
                }

                var index = _mapIndexService.CoordinatesToIndex(new Vector2Int(pointX, pointY));
                var level = _soilMoistureSimulator.MoistureLevels[index];

                if (level > highest)
                {
                    highest = level;
                    direction = 2;
                }
            }

            if (highest == 0)
            {
                WaterAbsorptionPlugin.Log.LogWarning("IT IS DRY!!!");
                return;
            }

            if (depth > WaterAbsorptionPlugin.Config.MaxSearchDepth)
            {
                LogAround(x, y, direction, highest, pointX, pointY);
                return;
            }

            if (logDebug)
            {
                WaterAbsorptionPlugin.Log.LogInfo($"Direction: {direction} Value: {highest} Depth: {depth}");
            }

            switch (direction)
            {
                case 0:
                    FindSW(ref found, ref isIrrigationTower, x + _southwest.x, y + _southwest.y, depth);
                    break;
                case 1:
                    FindS(ref found, ref isIrrigationTower, x + _south.x, y + _south.y, depth);
                    break;
                case 2:
                    FindW(ref found, ref isIrrigationTower, x + _west.x, y + _west.y, depth);
                    break;
            }
        }

        private void CheckIfWaterOrIrrigator(ref bool found, ref bool isIrrigationTower, int x, int y)
        {
            if (WaterService._wateredMap[y][x])
            {
                found = true;
                _cachedX = x;
                _cachedY = y;
                return;
            }

            if (RegisteredIrrigator._irrigationTowerLocations.Any(item => item.x == x && item.y == y))
            {
                found = true;
                isIrrigationTower = true;
                _cachedX = x;
                _cachedY = y;
                return;
            }
        }

        private void LogAround(int x, int y, short direction, float highest, int pointX, int pointY)
        {
            WaterAbsorptionPlugin.Log.LogWarning($"MAX DEPTH SEARCH RANGE REACHED!!! OriX: {_blockObject.Coordinates.x} OriY: {_blockObject.Coordinates.y} Dir: {direction} Highest: {highest} x: {x} y: {y}");

            if (logDebug)
            {
                var index = _mapIndexService.CoordinatesToIndex(new Vector2Int(pointX + _north.x, pointY + _north.y));
                var level = _soilMoistureSimulator.MoistureLevels[index];
                WaterAbsorptionPlugin.Log.LogInfo($"_north: {level}");
                index = _mapIndexService.CoordinatesToIndex(new Vector2Int(pointX + _east.x, pointY + _east.y));
                level = _soilMoistureSimulator.MoistureLevels[index];
                WaterAbsorptionPlugin.Log.LogInfo($"_east: {level}");
                index = _mapIndexService.CoordinatesToIndex(new Vector2Int(pointX + _west.x, pointY + _west.y));
                level = _soilMoistureSimulator.MoistureLevels[index];
                WaterAbsorptionPlugin.Log.LogInfo($"_west: {level}");
                index = _mapIndexService.CoordinatesToIndex(new Vector2Int(pointX + _south.x, pointY + _south.y));
                level = _soilMoistureSimulator.MoistureLevels[index];
                WaterAbsorptionPlugin.Log.LogInfo($"_south: {level}");
                index = _mapIndexService.CoordinatesToIndex(new Vector2Int(pointX + _northeast.x, pointY + _northeast.y));
                level = _soilMoistureSimulator.MoistureLevels[index];
                WaterAbsorptionPlugin.Log.LogInfo($"_northeast: {level}");
                index = _mapIndexService.CoordinatesToIndex(new Vector2Int(pointX + _northwest.x, pointY + _northwest.y));
                level = _soilMoistureSimulator.MoistureLevels[index];
                WaterAbsorptionPlugin.Log.LogInfo($"_northwest: {level}");
                index = _mapIndexService.CoordinatesToIndex(new Vector2Int(pointX + _southeast.x, pointY + _southeast.y));
                level = _soilMoistureSimulator.MoistureLevels[index];
                WaterAbsorptionPlugin.Log.LogInfo($"_southeast: {level}");
                index = _mapIndexService.CoordinatesToIndex(new Vector2Int(pointX + _southwest.x, pointY + _southwest.y));
                level = _soilMoistureSimulator.MoistureLevels[index];
                WaterAbsorptionPlugin.Log.LogInfo($"_southwest: {level}");
            }
        }

        private void CheckIfCached(ref bool found, ref bool isIrrigationTower)
        {
            if (_cachedX.HasValue && _cachedY.HasValue)
            {
                if (WaterService._wateredMap[_cachedX.Value][_cachedY.Value])
                {
                    found = true;
                    _cacheAge++;
                    return;
                }
                
                if (RegisteredIrrigator._irrigationTowerLocations.Any(x => x.x == _cachedX.Value && x.y == _cachedY.Value))
                {
                    found = true;
                    isIrrigationTower = true;
                    _cacheAge++;
                    return;
                }

                _cacheAge = 0;
                _cachedX = null;
                _cachedY = null;
            }
        }

        private void UpdateWaterDepth()
        {
            if (!_cachedX.HasValue || !_cachedY.HasValue)
            {
                WaterAbsorptionPlugin.Log.LogWarning($"_cachedX: {(_cachedX.HasValue ? _cachedX.Value.ToString() : "NULL")} _cachedY: {(_cachedY.HasValue ? _cachedY.Value.ToString() : "NULL")}");
                return;
            }

            _waterSimulator.UpdateWaterDepth(new Vector2Int(_cachedX.Value, _cachedY.Value), WaterAbsorptionPlugin.Config.GrowableTickWaterDepth);
        }
    }
}
