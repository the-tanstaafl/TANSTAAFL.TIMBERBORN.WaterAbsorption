using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using TANSTAAFL.TIMBERBORN.WaterAbsorption.TickTracker;
using TimberApi.Common.Extensions;
using TimberApi.ConsoleSystem;
using TimberApi.ModSystem;
using Timberborn.EditorStarter;
using Timberborn.EntitySystem;
using Timberborn.Growing;
using Timberborn.IrrigationSystem;
using Timberborn.MapIndexSystem;
using Timberborn.NaturalResourcesModelSystem;
using Timberborn.NaturalResourcesReproduction;
using Timberborn.Persistence;
using Timberborn.SoilMoistureSystem;
using Timberborn.TickSystem;
using Timberborn.WaterSystem;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.UIElements;

namespace TANSTAAFL.TIMBERBORN.WaterAbsorption
{
    [HarmonyPatch]
    public class WaterAbsorptionPlugin : IModEntrypoint
    {
        public static WaterAbsorptionConfig Config;
        internal static IConsoleWriter Log;

        public void Entry(IMod mod, IConsoleWriter consoleWriter)
        {
            Log = consoleWriter;
            Config = mod.Configs.Get<WaterAbsorptionConfig>();

            var harmony = new Harmony("tanstaafl.plugins.WaterAbsorption");
            harmony.PatchAll();
        }

        /// <summary>
        /// Patch the WaterSimulator.Load to alter the Evaporation values
        /// </summary>
        /// <param name="__instance"></param>
        [HarmonyPatch(typeof(WaterSimulator), nameof(WaterSimulator.Load))]
        public static void Postfix(WaterSimulator __instance)
        {
            //__instance._waterSimulationSettings._fastEvaporationDepthThreshold
            __instance._waterSimulationSettings._normalEvaporationSpeed *= 0.25f;
            __instance._waterSimulationSettings._fastEvaporationSpeed *= 0.25f;
        }
    }
}
