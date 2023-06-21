using System;
using System.Collections.Generic;
using System.Text;
using Timberborn.Persistence;

namespace TANSTAAFL.TIMBERBORN.WaterAbsorption.Config
{
    public class WaterAbsorptionConfigSerializer : IObjectSerializer<WaterAbsorptionConfig>
    {
        private static readonly PropertyKey<float> NormalEvaporationSpeedMultiplierKey = new PropertyKey<float>("NormalEvaporationSpeedMultiplier");
        private static readonly PropertyKey<float> FastEvaporationSpeedMultiplierKey = new PropertyKey<float>("FastEvaporationSpeedMultiplier");
        private static readonly PropertyKey<float> IrrigatorTickIncrementKey = new PropertyKey<float>("IrrigatorTickIncrement");
        private static readonly PropertyKey<float> GrowableTickWaterDepthKey = new PropertyKey<float>("GrowableTickWaterDepth");
        private static readonly PropertyKey<int> MaxTicksKey = new PropertyKey<int>("MaxTicks");
        private static readonly PropertyKey<int> MaxSearchDepthKey = new PropertyKey<int>("MaxSearchDepth");

        public void Serialize(WaterAbsorptionConfig value, IObjectSaver objectSaver)
        {
            objectSaver.Set(NormalEvaporationSpeedMultiplierKey, value.NormalEvaporationSpeedMultiplier);
            objectSaver.Set(FastEvaporationSpeedMultiplierKey, value.FastEvaporationSpeedMultiplier);
            objectSaver.Set(IrrigatorTickIncrementKey, value.IrrigatorTickIncrement);
            objectSaver.Set(GrowableTickWaterDepthKey, value.GrowableTickWaterDepth);
            objectSaver.Set(MaxTicksKey, value.MaxTicks);
            objectSaver.Set(MaxSearchDepthKey, value.MaxSearchDepth);
        }

        public Obsoletable<WaterAbsorptionConfig> Deserialize(IObjectLoader objectLoader)
        {
            var defaultValues = new WaterAbsorptionConfig();

            var normalEvaporation = defaultValues.NormalEvaporationSpeedMultiplier;
            if (objectLoader.Has(NormalEvaporationSpeedMultiplierKey))
            {
                normalEvaporation = objectLoader.Get(NormalEvaporationSpeedMultiplierKey);
            }

            var fastEvaporation = defaultValues.FastEvaporationSpeedMultiplier;
            if (objectLoader.Has(FastEvaporationSpeedMultiplierKey))
            {
                fastEvaporation = objectLoader.Get(FastEvaporationSpeedMultiplierKey);
            }

            var irrigatorTickIncrement = defaultValues.IrrigatorTickIncrement;
            if (objectLoader.Has(IrrigatorTickIncrementKey))
            {
                irrigatorTickIncrement = objectLoader.Get(IrrigatorTickIncrementKey);
            }

            var growableTickWaterDepth = defaultValues.GrowableTickWaterDepth;
            if (objectLoader.Has(GrowableTickWaterDepthKey))
            {
                growableTickWaterDepth = objectLoader.Get(GrowableTickWaterDepthKey);
            }

            var maxTicks = defaultValues.MaxTicks;
            if (objectLoader.Has(MaxTicksKey))
            {
                maxTicks = objectLoader.Get(MaxTicksKey);
            }

            var maxSearchDepth = defaultValues.MaxSearchDepth;
            if (objectLoader.Has(MaxSearchDepthKey))
            {
                maxSearchDepth = objectLoader.Get(MaxSearchDepthKey);
            }

            return new WaterAbsorptionConfig(normalEvaporation, fastEvaporation, irrigatorTickIncrement, growableTickWaterDepth, maxTicks, maxSearchDepth);
        }
    }
}
