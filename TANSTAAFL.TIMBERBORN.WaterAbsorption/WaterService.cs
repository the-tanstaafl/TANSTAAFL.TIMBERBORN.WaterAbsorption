using Bindito.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Timberborn.Core;
using Timberborn.EntitySystem;
using Timberborn.IrrigationSystem;
using Timberborn.MapIndexSystem;
using Timberborn.WaterSystem;
using UnityEngine;

namespace TANSTAAFL.TIMBERBORN.WaterAbsorption
{
    internal class WaterService
    {
        internal static bool[][] _wateredMap;

        internal static void GenerateWateredMap(WaterMap _waterMap, MapIndexService _mapIndexService)
        {
            var mapSize = _mapIndexService.MapSize;
            var waterDepths = _waterMap.WaterDepths;

            _wateredMap = new bool[mapSize.x + 1][];

            for (var i = 0; i < _wateredMap.Length; i++)
            {
                _wateredMap[i] = new bool[mapSize.y + 1];
            }

            for (int i = 0; i < waterDepths.Length; i++)
            {
                if (waterDepths[i] == 0)
                {
                    continue;
                }

                var coordinates = _mapIndexService.IndexToCoordinates(i);
                _wateredMap[coordinates.y][coordinates.x] = true;
            }
        }
    }
}
