using LINQtoCSV;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Text;
using TimberApi.UiBuilderSystem;
using Timberborn.CoreUI;
using Timberborn.Navigation;
using Timberborn.WaterSystem;
using UnityEngine.UIElements;
using static UnityEngine.UIElements.Length.Unit;

namespace TANSTAAFL.TIMBERBORN.WaterAbsorption.Config
{
    public class WaterAbsorptionConfigBox
    {
        public static Action OpenOptionsDelegate;
        private readonly DialogBoxShower _dialogBoxShower;
        private readonly UIBuilder _builder;
        private NavigationDistance _navigationDistance;

        private VisualElement _root;
        private WaterSimulationSettings _waterSimulationSettings;

        public WaterAbsorptionConfigBox(WaterSimulationSettings waterSimulationSettings, DialogBoxShower dialogBoxShower, UIBuilder builder, NavigationDistance navigationDistance)
        {
            OpenOptionsDelegate = OpenOptionsPanel;
            _dialogBoxShower = dialogBoxShower;
            _builder = builder;
            _navigationDistance = navigationDistance;
            _waterSimulationSettings = waterSimulationSettings;
        }

        private void OpenOptionsPanel()
        {
            var defaultValues = new WaterAbsorptionConfig();

            _root = _builder.CreateComponentBuilder()
                            .CreateVisualElement()
                            .AddPreset(factory => factory.Labels()
                                .GameTextBig(name: "NormalEvaporationSpeedMultiplierLabel",
                                        text: $"Normal Evaporation Speed Multiplier: default({defaultValues.NormalEvaporationSpeedMultiplier})",
                                        builder: builder => builder.SetStyle(style => style.alignSelf = Align.Center)))
                            .AddPreset(factory => factory.TextFields().InGameTextField(new Length(100, Pixel), name: "NormalEvaporationSpeedMultiplierTextField"))

                            .AddPreset(factory => factory.Labels()
                                .GameTextBig(name: "FastEvaporationSpeedMultiplierLabel",
                                        text: $"Fast Evaporation Speed Multiplier: default({defaultValues.FastEvaporationSpeedMultiplier})",
                                        builder: builder => builder.SetStyle(style => style.alignSelf = Align.Center)))
                            .AddPreset(factory => factory.TextFields().InGameTextField(new Length(100, Pixel), name: "FastEvaporationSpeedMultiplierTextField"))

                            .AddPreset(factory => factory.Labels()
                                .GameTextBig(name: "IrrigatorTickIncrementLabel",
                                        text: $"Irrigator Tick Increment: default({defaultValues.IrrigatorTickIncrement})",
                                        builder: builder => builder.SetStyle(style => style.alignSelf = Align.Center)))
                            .AddPreset(factory => factory.TextFields().InGameTextField(new Length(100, Pixel), name: "IrrigatorTickIncrementTextField"))

                            .AddPreset(factory => factory.Labels()
                                .GameTextBig(name: "GrowableTickWaterDepthLabel",
                                        text: $"Growable Tick WaterDepth: default({defaultValues.GrowableTickWaterDepth})",
                                        builder: builder => builder.SetStyle(style => style.alignSelf = Align.Center)))
                            .AddPreset(factory => factory.TextFields().InGameTextField(new Length(100, Pixel), name: "GrowableTickWaterDepthTextField"))

                            .AddPreset(factory => factory.Labels()
                                .GameTextBig(name: "MaxTicksLabel",
                                        text: $"Max Ticks: default({defaultValues.MaxTicks})",
                                        builder: builder => builder.SetStyle(style => style.alignSelf = Align.Center)))
                            .AddPreset(factory => factory.TextFields().InGameTextField(new Length(100, Pixel), name: "MaxTicksTextField"))

                            .AddPreset(factory => factory.Labels()
                                .GameTextBig(name: "MaxSearchDepthLabel",
                                        text: $"Max Search Depth: default({defaultValues.MaxSearchDepth})",
                                        builder: builder => builder.SetStyle(style => style.alignSelf = Align.Center)))
                            .AddPreset(factory => factory.TextFields().InGameTextField(new Length(100, Pixel), name: "MaxSearchDepthTextField"))

                            .BuildAndInitialize();

            var normalEvaporationTextField = _root.Q<TextField>("NormalEvaporationSpeedMultiplierTextField");
            normalEvaporationTextField.value = WaterAbsorptionConfigLoader._savedConfig.NormalEvaporationSpeedMultiplier.ToString();
            
            var fastEvaporationTextField = _root.Q<TextField>("FastEvaporationSpeedMultiplierTextField");
            fastEvaporationTextField.value = WaterAbsorptionConfigLoader._savedConfig.FastEvaporationSpeedMultiplier.ToString();

            var irrigatorTickIncrementTextField = _root.Q<TextField>("IrrigatorTickIncrementTextField");
            irrigatorTickIncrementTextField.value = WaterAbsorptionConfigLoader._savedConfig.IrrigatorTickIncrement.ToString();

            var growableTickWaterDepthTextField = _root.Q<TextField>("GrowableTickWaterDepthTextField");
            growableTickWaterDepthTextField.value = WaterAbsorptionConfigLoader._savedConfig.GrowableTickWaterDepth.ToString();

            var maxTicksTextField = _root.Q<TextField>("MaxTicksTextField");
            maxTicksTextField.value = WaterAbsorptionConfigLoader._savedConfig.MaxTicks.ToString();

            var maxSearchDepthTextField = _root.Q<TextField>("MaxSearchDepthTextField");
            maxSearchDepthTextField.value = WaterAbsorptionConfigLoader._savedConfig.MaxSearchDepth.ToString();

            var builder = _dialogBoxShower.Create()
                .AddContent(_root)
                .SetConfirmButton(UpdateConfigs, "Save")
                .SetCancelButton(() => { }, "Cancel");

            builder.Show();
        }

        private void UpdateConfigs()
        {
            var normalEvaporationText = _root.Q<TextField>("NormalEvaporationSpeedMultiplierTextField").value;
            var fastEvaporationText = _root.Q<TextField>("FastEvaporationSpeedMultiplierTextField").value;
            var irrigatorTickIncrementText = _root.Q<TextField>("IrrigatorTickIncrementTextField").value;
            var growableTickWaterDepthText = _root.Q<TextField>("GrowableTickWaterDepthTextField").value;
            var maxTicksText = _root.Q<TextField>("MaxTicksTextField").value;
            var maxSearchDepthText = _root.Q<TextField>("MaxSearchDepthTextField").value;

            var savedConfig = WaterAbsorptionConfigLoader._savedConfig;

            if (float.TryParse(normalEvaporationText, out float normalEvaporation))
            {
                savedConfig.NormalEvaporationSpeedMultiplier = normalEvaporation;
            }

            if (float.TryParse(fastEvaporationText, out float fastEvaporation))
            {
                savedConfig.FastEvaporationSpeedMultiplier = fastEvaporation;
            }

            if (float.TryParse(irrigatorTickIncrementText, out float irrigatorTickIncrement))
            {
                savedConfig.IrrigatorTickIncrement = irrigatorTickIncrement;
            }

            if (float.TryParse(growableTickWaterDepthText, out float growableTickWaterDepth))
            {
                savedConfig.GrowableTickWaterDepth = growableTickWaterDepth;
            }

            if (int.TryParse(maxTicksText, out int maxTicks))
            {
                savedConfig.MaxTicks = maxTicks;
            }

            if (int.TryParse(maxSearchDepthText, out int maxSearchDepth))
            {
                savedConfig.MaxSearchDepth = maxSearchDepth;
            }

            WaterAbsorptionConfigLoader.ApplyConfigs(_waterSimulationSettings);
        }
    }
}
