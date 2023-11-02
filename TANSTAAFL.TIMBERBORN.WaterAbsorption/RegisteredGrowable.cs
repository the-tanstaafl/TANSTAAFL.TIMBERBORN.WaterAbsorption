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
using TANSTAAFL.TIMBERBORN.WaterAbsorption.Config;
using Timberborn.BaseComponentSystem;
using Timberborn.NaturalResourcesLifecycle;

namespace TANSTAAFL.TIMBERBORN.WaterAbsorption
{
    public class RegisteredGrowable : BaseComponent, IRegisteredComponent
    {
        internal Growable _growable;
        internal DryObject _dryObject;
        internal BlockObject _blockObject;
        internal LivingNaturalResource _livingNaturalResource;
        internal int? _cachedX;
        internal int? _cachedY;
        private short _cacheAge = 0;
        private System.Random _random = new System.Random();

        private static EntityComponentRegistry _entityComponentRegistry;
        private static MapIndexService _mapIndexService;
        private static WaterChangeService _waterChangeService;
        private static SoilMoistureSimulator _soilMoistureSimulator;

        private static Dictionary<int, Dictionary<int, float>> _irrigationAccumulator = new Dictionary<int, Dictionary<int, float>>();

        private static bool logDebug = false;

        [Inject]
        public void InjectDependencies(EntityComponentRegistry entityComponentRegistry, MapIndexService mapIndexService, WaterChangeService waterChangeService, SoilMoistureSimulator soilMoistureSimulator)
        {
            _entityComponentRegistry = entityComponentRegistry;
            _mapIndexService = mapIndexService;
            _waterChangeService = waterChangeService;
            _soilMoistureSimulator = soilMoistureSimulator;
        }

        public void Awake()
        {
            _growable = GetComponentFast<Growable>();
            _dryObject = GetComponentFast<DryObject>();
            _blockObject = GetComponentFast<BlockObject>();
            _livingNaturalResource = GetComponentFast<LivingNaturalResource>();
        }

        internal void ConsumeWater()
        {
            if (_cacheAge > 3 && _random.Next(10 - _cacheAge) == 0)
            {
                _cacheAge = 0;
                _cachedX = null;
                _cachedY = null;
            }

            bool foundWater = CheckIfCached();

            if (!foundWater)
            {
                foundWater = new WaterSearch.WaterSearcher(this, _mapIndexService, _soilMoistureSimulator).FindLocationAdvanced();
            }

            if (!foundWater)
            {
                if (logDebug)
                {
                    WaterAbsorptionPlugin.Log.LogWarning($"NO WATER FOUND!!!");
                }

                return;
            }
            UpdateWaterDepth();
        }
        
        private bool CheckIfCached()
        {
            if (_cachedX.HasValue && _cachedY.HasValue)
            {
                if (WaterService._wateredMap[_cachedY.Value][_cachedX.Value])
                {
                    _cacheAge++;
                    return true;
                }
                
                _cacheAge = 0;
                _cachedX = null;
                _cachedY = null;
            }

            return false;
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

            _waterChangeService.EnqueueWaterChange(new Vector2Int(_cachedX.Value, _cachedY.Value), WaterAbsorptionConfigLoader._savedConfig.GrowableTickWaterDepth, 0);
        }
    }
}
