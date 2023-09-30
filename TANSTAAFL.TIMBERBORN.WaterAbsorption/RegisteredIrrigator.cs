using Bindito.Core;
using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Timberborn.BlockSystem;
using Timberborn.Core;
using Timberborn.Cutting;
using Timberborn.EntitySystem;
using Timberborn.Gathering;
using Timberborn.GoodConsumingBuildingSystem;
using Timberborn.Growing;
using Timberborn.MapIndexSystem;
using Timberborn.NaturalResources;
using Timberborn.SingletonSystem;
using Timberborn.SoilMoistureSystem;
using Timberborn.WaterSystem;
using Timberborn.Goods;
using UnityEngine;
using Timberborn.IrrigationSystem;
using Timberborn.Common;
using Timberborn.BaseComponentSystem;

namespace TANSTAAFL.TIMBERBORN.WaterAbsorption
{
    public class RegisteredIrrigator : BaseComponent, IRegisteredComponent
    {
        internal IrrigationTower _irrigationTower;
        internal BlockObject _blockObject;
        internal GoodConsumingBuilding _goodConsumingBuilding;

        public void Awake()
        {
            _irrigationTower = GetComponentFast<IrrigationTower>();
            _blockObject = GetComponentFast<BlockObject>();
            _goodConsumingBuilding = GetComponentFast<GoodConsumingBuilding>();
            _goodConsumingBuilding._goodPerHour = 0f;
        }
    }
}
