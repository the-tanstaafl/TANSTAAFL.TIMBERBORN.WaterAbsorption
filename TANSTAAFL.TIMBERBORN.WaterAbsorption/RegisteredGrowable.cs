using Bindito.Core;
using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Timberborn.BlockSystem;
using Timberborn.EntitySystem;
using Timberborn.Growing;
using Timberborn.MapIndexSystem;
using Timberborn.SoilMoistureSystem;
using Timberborn.WaterSystem;
using Timberborn.Goods;
using UnityEngine;
using Timberborn.NaturalResourcesLifeCycle;
using TANSTAAFL.TIMBERBORN.WaterAbsorption.Config;

namespace TANSTAAFL.TIMBERBORN.WaterAbsorption
{
    public class RegisteredGrowable : MonoBehaviour, IRegisteredComponent
    {
        internal Growable _growable;
        internal DryObject _dryObject;
        internal BlockObject _blockObject;
        internal LivingNaturalResource _livingNaturalResource;
        internal int? _cachedX;
        internal int? _cachedY;
        private short _cacheAge = 0;
        private System.Random _random = new System.Random();

        private static MapIndexService _mapIndexService;
        private static WaterSimulator _waterSimulator;
        private static SoilMoistureSimulator _soilMoistureSimulator;

        private static Dictionary<int, Dictionary<int, float>> _irrigationAccumulator = new Dictionary<int, Dictionary<int, float>>();

        private static bool logDebug = false;

        [Inject]
        public void InjectDependencies(MapIndexService mapIndexService, WaterSimulator waterSimulator, SoilMoistureSimulator soilMoistureSimulator)
        {
            _mapIndexService = mapIndexService;
            _waterSimulator = waterSimulator;
            _soilMoistureSimulator = soilMoistureSimulator;
        }

        public void Awake()
        {
            _growable = GetComponent<Growable>();
            _dryObject = GetComponent<DryObject>();
            _blockObject = GetComponent<BlockObject>();
            _livingNaturalResource = GetComponent<LivingNaturalResource>();
        }

        internal void ConsumeWater()
        {
            if (_cacheAge > 3 && _random.Next(10 - _cacheAge) == 0)
            {
                _cacheAge = 0;
                _cachedX = null;
                _cachedY = null;
            }

            (bool foundWater, bool waterIsIrrigationTower) = CheckIfCached();

            if (!foundWater)
            {
                (foundWater, waterIsIrrigationTower) = new WaterSearch.WaterSearcher(this, _mapIndexService, _soilMoistureSimulator).FindLocationAdvanced();
            }

            if (!foundWater)
            {
                if (logDebug)
                {
                    WaterAbsorptionPlugin.Log.LogWarning($"NO WATER FOUND!!!");
                }

                return;
            }

            if (waterIsIrrigationTower)
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

            _irrigationAccumulator[coordinates.y][coordinates.x] += WaterAbsorptionConfigLoader._savedConfig.IrrigatorTickIncrement;

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
                if (logDebug)
                {
                    WaterAbsorptionPlugin.Log.LogWarning($"NO IRRIGATION TOWER FOUND!!!");
                }

                return;
            }

            var amount = irrigator._goodConsumingBuilding.Inventory.AmountInStock("Water");
            if (amount == 0)
            {
                if (logDebug)
                {
                    WaterAbsorptionPlugin.Log.LogWarning($"NO WATER IN IRRIGATION TOWER!!!");
                }

                return;
            }

            irrigator._goodConsumingBuilding.Inventory.Take(new GoodAmount("Water", 1));
            if (irrigator._goodConsumingBuilding.Inventory.UnreservedAmountInStock(irrigator._goodConsumingBuilding._supply) <= 0)
            {
                irrigator._goodConsumingBuilding._supplyLeft = 0;
            }
        }
        
        private (bool foundWater, bool waterIsIrrigationTower) CheckIfCached()
        {
            if (_cachedX.HasValue && _cachedY.HasValue)
            {
                if (WaterService._wateredMap[_cachedY.Value][_cachedX.Value])
                {
                    _cacheAge++;
                    return (true, false);
                }
                
                if (RegisteredIrrigator._irrigationTowerLocations.Any(x => x.x == _cachedX.Value && x.y == _cachedY.Value))
                {
                    _cacheAge++;
                    return (true, true);
                }

                _cacheAge = 0;
                _cachedX = null;
                _cachedY = null;
            }

            return (false, false);
        }

        private void UpdateWaterDepth()
        {
            if (!_cachedX.HasValue || !_cachedY.HasValue)
            {
                if (logDebug)
                {
                    WaterAbsorptionPlugin.Log.LogWarning($"_cachedX: {(_cachedX.HasValue ? _cachedX.Value.ToString() : "NULL")} _cachedY: {(_cachedY.HasValue ? _cachedY.Value.ToString() : "NULL")}");
                }
                
                return;
            }

            _waterSimulator.UpdateWaterDepth(new Vector2Int(_cachedX.Value, _cachedY.Value), WaterAbsorptionConfigLoader._savedConfig.GrowableTickWaterDepth);
        }
    }
}
