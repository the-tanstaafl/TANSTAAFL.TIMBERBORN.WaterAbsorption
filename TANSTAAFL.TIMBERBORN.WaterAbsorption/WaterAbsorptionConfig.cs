using System;
using System.Collections.Generic;
using System.Text;
using TimberApi.ConfigSystem;

namespace TANSTAAFL.TIMBERBORN.WaterAbsorption
{
    public class WaterAbsorptionConfig : IConfig
    {
        public string ConfigFileName => "WaterAbsorption";

        public float NormalEvaporationSpeed = 0.25f;
        public float FastEvaporationSpeed = 0.25f;
        public float IrrigatorTickIncrement = 0.001f;
        public float GrowableTickWaterDepth = -0.000005f;
        public short MaxTicks = 13;
        public short MaxSearchDepth = 25;
    }
}
