using Bindito.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Timberborn.Growing;
using Timberborn.IrrigationSystem;
using Timberborn.MapIndexSystem;
using Timberborn.MapSystemUI;
using Timberborn.SoilMoistureSystem;
using Timberborn.TickSystem;
using Timberborn.TimeSystem;
using Timberborn.WaterSystem;
using UnityEngine;
using UnityEngine.UIElements;

namespace TANSTAAFL.TIMBERBORN.WaterAbsorption.TickTracker
{
    public class TickableSingleton : ITickableSingleton
    {
        public short CurrentTick { get; private set; } = -1;

        public void Tick()
        {
            if (CurrentTick == WaterAbsorptionPlugin.Config.MaxTicks - 1)
            {
                CurrentTick = -1;
            }

            CurrentTick++;

            //WaterAbsorptionPlugin.Log.LogInfo($"Tick => {CurrentTick}");

            RegisteredGrowable.HandleGrowables(CurrentTick);
        }
    }
}
