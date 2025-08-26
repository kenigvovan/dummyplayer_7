using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Common;
using Vintagestory.GameContent;
using Vintagestory.API.Datastructures;
using dummyplayer.src.entity;

namespace dummyplayer.src.behavior
{
    public class EntityBehaviorCloneInventory : EntityBehaviorTexturedClothing
    {
        public override string PropertyName()
        {
            return "cloneinventory";
        }
        public override InventoryBase Inventory
        {
            get
            {
                return this.inv;
            }
        }
        public override string InventoryClassName
        {
            get
            {
                return "cloneinventory";
            }
        }
        public EntityBehaviorCloneInventory(Entity entity)
            : base(entity)
        {
            this.eagent = entity as EntityAgent;
            if (EntityClonePlayer.coActive)
            {
                this.inv = new InventoryNPCGear(null, null, 44);
            }
            else
            {
                this.inv = new InventoryNPCGear(null, null, 19);
            }
        }
        public override void Initialize(EntityProperties properties, JsonObject attributes)
        {
            this.Api = this.entity.World.Api;
            this.inv.LateInitialize("gearinv-" + this.entity.EntityId.ToString(), this.Api);
            this.loadInv();
            this.eagent.WatchedAttributes.RegisterModifiedListener("wearablesInv", new Action(this.wearablesModified));
            base.Initialize(properties, attributes);
        }
        private void wearablesModified()
        {
            this.loadInv();
            this.eagent.MarkShapeModified();
        }
        private EntityAgent eagent;
        private InventoryNPCGear inv;
    }
}
