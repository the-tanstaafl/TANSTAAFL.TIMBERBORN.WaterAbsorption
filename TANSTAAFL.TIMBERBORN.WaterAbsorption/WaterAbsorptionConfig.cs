using System;
using System.Collections.Generic;
using System.Text;
using TimberApi.ConfigSystem;

namespace TANSTAAFL.TIMBERBORN.WaterAbsorption
{
    public class WaterAbsorptionConfig : IConfig
    {
        public string ConfigFileName => "WaterAbsorption";

        public float NormalEvaporationSpeedMultiplier = 0.25f;
        public float FastEvaporationSpeedMultiplier = 0.5f;
        public float IrrigatorTickIncrement = 0.001f;
        public float GrowableTickWaterDepth = -0.00001f;
        public short MaxTicks = 13;
        public short MaxSearchDepth = 25;
    }
}
