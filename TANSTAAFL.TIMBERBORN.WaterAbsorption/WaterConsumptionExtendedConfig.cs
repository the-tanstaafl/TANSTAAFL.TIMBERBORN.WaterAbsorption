using System;
using System.Collections.Generic;
using System.Text;
using TimberApi.ConfigSystem;

namespace TANSTAAFL.TIMBERBORN.WaterAbsorption
{
    public class WaterAbsorptionConfig : IConfig
    {
        public string ConfigFileName => "WaterAbsorption";

        public float PlantConsumtion;
    }
}
