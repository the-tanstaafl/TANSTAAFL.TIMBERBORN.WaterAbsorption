using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using TANSTAAFL.TIMBERBORN.WaterAbsorption.Config;
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
using Timberborn.Options;
using Timberborn.OptionsGame;
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
        internal static IConsoleWriter Log;

        public void Entry(IMod mod, IConsoleWriter consoleWriter)
        {
            Log = consoleWriter;

            var harmony = new Harmony("tanstaafl.plugins.WaterAbsorption");
            harmony.PatchAll();
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(GameOptionsBox), "GetPanel")]
        static void ShowConfigBox(ref VisualElement __result)
        {
            VisualElement root = __result.Query("OptionsBox");

            Button button = new() { classList = { "menu-button" } };
            button.text = "WaterAbsorption config";
            button.clicked += WaterAbsorptionConfigBox.OpenOptionsDelegate;

            root.Insert(4, button);
        }
    }
}
