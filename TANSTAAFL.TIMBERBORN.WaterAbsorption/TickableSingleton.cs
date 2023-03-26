using Bindito.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Timberborn.EntitySystem;
using Timberborn.MapIndexSystem;
using Timberborn.TickSystem;
using Timberborn.WaterSystem;

namespace TANSTAAFL.TIMBERBORN.WaterAbsorption.TickTracker
{
    public class TickableSingleton : ITickableSingleton
    {
        private static EntityComponentRegistry _entityComponentRegistry;
        private static MapIndexService _mapIndexService;
        private static WaterMap _waterMap;

        [Inject]
        public void InjectDependencies(EntityComponentRegistry entityComponentRegistry, MapIndexService mapIndexService, WaterMap waterMap)
        {
            _entityComponentRegistry = entityComponentRegistry;
            _mapIndexService = mapIndexService;
            _waterMap = waterMap;
        }

        public short CurrentTick { get; private set; } = -1;

        public void Tick()
        {
            if (CurrentTick == WaterAbsorptionPlugin.Config.MaxTicks - 1)
            {
                CurrentTick = -1;
            }

            CurrentTick++;

            GrowableHandler.HandleGrowables(_entityComponentRegistry, _mapIndexService, _waterMap, CurrentTick);
        }
    }
}
