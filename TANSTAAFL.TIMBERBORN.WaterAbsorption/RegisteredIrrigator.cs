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
using Timberborn.Common;
using Timberborn.BaseComponentSystem;

namespace TANSTAAFL.TIMBERBORN.WaterAbsorption
{
    public class RegisteredIrrigator : BaseComponent, IRegisteredComponent
    {
        internal IrrigationTower _irrigationTower;
        internal BlockObject _blockObject;
        internal GoodConsumingBuilding _goodConsumingBuilding;

        private static EntityComponentRegistry _entityComponentRegistry;

        internal static List<(int x, int y)> _irrigationTowerLocations = new List<(int x, int y)>();
        internal static Dictionary<Vector2Int, Vector2Int> _irrigationTowerEntranceLocations = new Dictionary<Vector2Int, Vector2Int>();

        [Inject]
        public void InjectDependencies(EntityComponentRegistry entityComponentRegistry, MapIndexService mapIndexService, WaterSimulator waterSimulator, WaterMap waterMap)
        {
            _entityComponentRegistry = entityComponentRegistry;
        }

        public void Awake()
        {
            _irrigationTower = GetComponentFast<IrrigationTower>();
            _blockObject = GetComponentFast<BlockObject>();
            _goodConsumingBuilding = GetComponentFast<GoodConsumingBuilding>();
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
                foreach (var coordinate in irrigator._blockObject.PositionedBlocks.GetOccupiedCoordinates().XY().Distinct())
                {
                    _irrigationTowerLocations.Add((coordinate.x, coordinate.y));
                    _irrigationTowerEntranceLocations[new Vector2Int(coordinate.x, coordinate.y)] = irrigator._blockObject.Coordinates.XY();
                }
            }
        }
    }
}
