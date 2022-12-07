using Bindito.Core;
using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

namespace TANSTAAFL.TIMBERBORN.WaterAbsorption
{
    public class RegisteredIrrigator : MonoBehaviour, IRegisteredComponent
    {
        internal IrrigationTower _irrigationTower;
        internal BlockObject _blockObject;
        internal GoodConsumingBuilding _goodConsumingBuilding;

        private static EntityComponentRegistry _entityComponentRegistry;

        internal static List<(int x, int y)> _irrigationTowerLocations = new List<(int x, int y)>();

        public static Dictionary<short, string> GrowableTypeOrder = new Dictionary<short, string>();

        [Inject]
        public void InjectDependencies(EntityComponentRegistry entityComponentRegistry, MapIndexService mapIndexService, WaterSimulator waterSimulator, WaterMap waterMap)
        {
            _entityComponentRegistry = entityComponentRegistry;
        }

        public void Awake()
        {
            _irrigationTower = GetComponent<IrrigationTower>();
            _blockObject = GetComponent<BlockObject>();
            _goodConsumingBuilding = GetComponent<GoodConsumingBuilding>();
            _goodConsumingBuilding._goodPerHour = 0f;
        }

        internal static IEnumerable<RegisteredIrrigator> GetIrrigators()
        {
            var irrigators = _entityComponentRegistry
                .GetEnabled<RegisteredIrrigator>()
                .Where(x => x._irrigationTower._addedToService);

            return irrigators;
        }

        internal static void GenerateIrrigationTowerLocations()
        {
            var irigators = GetIrrigators();

            _irrigationTowerLocations.Clear();

            foreach (var irrigator in irigators)
            {
                _irrigationTowerLocations.Add((irrigator._blockObject.Coordinates.x, irrigator._blockObject.Coordinates.y));
            }
        }
    }
}
