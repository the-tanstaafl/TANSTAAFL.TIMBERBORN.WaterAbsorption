using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Timberborn.Navigation;
using Timberborn.Persistence;
using Timberborn.SingletonSystem;
using Timberborn.WaterSystem;

namespace TANSTAAFL.TIMBERBORN.WaterAbsorption.Config
{
    public class WaterAbsorptionConfigLoader : ISaveableSingleton, ILoadableSingleton, IPostLoadableSingleton
    {
        private static readonly SingletonKey WaterAbsorptionStateRestorerKey = new SingletonKey("WaterAbsorptionStateRestorer");
        private static readonly PropertyKey<WaterAbsorptionConfig> SavedWaterAbsorptionStateKey = new PropertyKey<WaterAbsorptionConfig>("WaterAbsorptionState");

        private WaterSimulationSettings _waterSimulationSettings;
        internal static WaterAbsorptionConfig _savedConfig;
        private WaterAbsorptionConfigSerializer _configSerializer;

        private readonly ISingletonLoader _singletonLoader;


        public WaterAbsorptionConfigLoader(WaterSimulationSettings waterSimulationSettings, ISingletonLoader singletonLoader, WaterAbsorptionConfigSerializer configSerializer)
        {
            _waterSimulationSettings = waterSimulationSettings;
            _singletonLoader = singletonLoader;
            _configSerializer = configSerializer;
        }

        public void Load()
        {
            _savedConfig = null;
            if (_singletonLoader.HasSingleton(WaterAbsorptionStateRestorerKey))
            {
                IObjectLoader singleton = _singletonLoader.GetSingleton(WaterAbsorptionStateRestorerKey);
                _savedConfig = singleton.Get(SavedWaterAbsorptionStateKey, _configSerializer);
            }
        }

        public void PostLoad()
        {
            if (_savedConfig == null)
            {
                _savedConfig = new WaterAbsorptionConfig();
            }

            ApplyConfigs(_waterSimulationSettings);
        }

        public void Save(ISingletonSaver singletonSaver)
        {
            singletonSaver.GetSingleton(WaterAbsorptionStateRestorerKey).Set(SavedWaterAbsorptionStateKey, _savedConfig, _configSerializer);
        }

        public static void ApplyConfigs(WaterSimulationSettings waterSimulationSettings)
        {
            waterSimulationSettings.NormalEvaporationSpeed = _savedConfig._normalEvaporationSpeed * _savedConfig.NormalEvaporationSpeedMultiplier;
            waterSimulationSettings.FastEvaporationSpeed = _savedConfig._fastEvaporationSpeed * _savedConfig.FastEvaporationSpeedMultiplier;
        }
    }
}
