using System;
using System.Collections.Generic;
using System.Text;
using TimberApi.ConfigSystem;

namespace TANSTAAFL.TIMBERBORN.WaterAbsorption
{
    public class WaterAbsorptionConfig
    {
        public WaterAbsorptionConfig() { }
        public WaterAbsorptionConfig(float normalEvaporationSpeedMultiplier, float fastEvaporationSpeedMultiplier,
            float growableTickWaterDepth, int maxTicks, int maxSearchDepth)
        {
            NormalEvaporationSpeedMultiplier = normalEvaporationSpeedMultiplier;
            FastEvaporationSpeedMultiplier = fastEvaporationSpeedMultiplier;
            GrowableTickWaterDepth = growableTickWaterDepth;
            MaxTicks = maxTicks;
            MaxSearchDepth = maxSearchDepth;
        }

        internal readonly float _normalEvaporationSpeed = 0.0002f;
        internal readonly float _fastEvaporationSpeed = 0.002f;

        public float NormalEvaporationSpeedMultiplier = 0.25f;
        public float FastEvaporationSpeedMultiplier = 0.5f;
        public float GrowableTickWaterDepth = -0.00001f;
        public int MaxTicks = 13;
        public int MaxSearchDepth = 25;
    }
}
