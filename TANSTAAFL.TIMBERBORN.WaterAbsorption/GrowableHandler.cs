using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Timberborn.EntitySystem;
using Timberborn.MapIndexSystem;
using Timberborn.WaterSystem;

namespace TANSTAAFL.TIMBERBORN.WaterAbsorption
{
    internal class GrowableHandler
    {
        internal static Dictionary<short, string> GrowableTypeOrder = new Dictionary<short, string>();

        internal static void HandleGrowables(EntityComponentRegistry entityComponentRegistry, MapIndexService mapIndexService, WaterMap waterMap, short currentTick)
        {
            var growables = entityComponentRegistry
                .GetEnabled<RegisteredGrowable>()
                .Where(x => !x._livingNaturalResource.IsDead && !x._dryObject.IsDry);

            if (!growables.Any())
            {
                return;
            }

            string growableType = GetGrowableType(growables, currentTick);
            if (string.IsNullOrEmpty(growableType))
            {
                return;
            }

            WaterService.GenerateWateredMap(waterMap, mapIndexService);

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
    }
}
