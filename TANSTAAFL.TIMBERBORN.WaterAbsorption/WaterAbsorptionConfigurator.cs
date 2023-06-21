using Bindito.Core;
using System;
using System.Collections.Generic;
using System.Text;
using TANSTAAFL.TIMBERBORN.WaterAbsorption.Config;
using TANSTAAFL.TIMBERBORN.WaterAbsorption.TickTracker;
using TimberApi.ConfiguratorSystem;
using TimberApi.EntityLinkerSystem;
using TimberApi.SceneSystem;
using Timberborn.Buildings;
using Timberborn.Growing;
using Timberborn.IrrigationSystem;
using Timberborn.TemplateSystem;

namespace TANSTAAFL.TIMBERBORN.WaterAbsorption
{
    [Configurator(SceneEntrypoint.InGame)]
    public class WaterAbsorptionConfigurator : IConfigurator
    {
        public void Configure(IContainerDefinition containerDefinition)
        {
            containerDefinition.Bind<TickableSingleton>().AsSingleton();
            containerDefinition.Bind<WaterAbsorptionConfigLoader>().AsSingleton();
            containerDefinition.Bind<WaterAbsorptionConfigSerializer>().AsSingleton();
            containerDefinition.Bind<WaterAbsorptionConfigBox>().AsSingleton();
            containerDefinition.MultiBind<TemplateModule>().ToProvider(ProvideTemplateModule).AsSingleton();
        }

        private static TemplateModule ProvideTemplateModule()
        {
            var builder = new TemplateModule.Builder();
            builder.AddDecorator<Growable, RegisteredGrowable>();
            builder.AddDecorator<IrrigationTower, RegisteredIrrigator>();
            return builder.Build();
        }
    }
}
