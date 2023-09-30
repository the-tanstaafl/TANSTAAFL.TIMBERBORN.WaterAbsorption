using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Timberborn.Common;
using Timberborn.EntitySystem;
using UnityEngine;

namespace TANSTAAFL.TIMBERBORN.WaterAbsorption
{
    internal class IrrigatorHandler
    {
        internal static List<(int x, int y)> _irrigationTowerLocations = new List<(int x, int y)>();
        internal static Dictionary<Vector2Int, Vector2Int> _irrigationTowerEntranceLocations = new Dictionary<Vector2Int, Vector2Int>();

        internal static IEnumerable<RegisteredIrrigator> GetIrrigators(EntityComponentRegistry entityComponentRegistry)
        {
            return entityComponentRegistry
                .GetEnabled<RegisteredIrrigator>()
                .Where(x => x._irrigationTower._addedToService);
        }

        internal static void GenerateIrrigationTowerLocations(EntityComponentRegistry entityComponentRegistry)
        {
            var irigators = GetIrrigators(entityComponentRegistry);

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
