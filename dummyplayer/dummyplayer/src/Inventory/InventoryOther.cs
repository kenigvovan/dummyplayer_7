using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace dummyplayer.src.Inventory
{
    public class InventoryOther: InventoryBase
    {
        ItemSlot[] slots;

        public InventoryOther(string className, string id, ICoreAPI api, int slotsCount) : base(className, id, api)
        {
            slots = GenEmptySlots(slotsCount);
            baseWeight = 2.5f;
        }

        public InventoryOther(string inventoryId, ICoreAPI api, int slotsCount) : base(inventoryId, api)
        {
            slots = GenEmptySlots(slotsCount);
            baseWeight = 2.5f;
        }

        public override void OnItemSlotModified(ItemSlot slot)
        {
            base.OnItemSlotModified(slot);
        }


        public override int Count
        {
            get { return slots.Length; }
        }

        public override ItemSlot this[int slotId] { get { return slots[slotId]; } set { slots[slotId] = value; } }

        public override void FromTreeAttributes(ITreeAttribute tree)
        {
            List<ItemSlot> modifiedSlots = new List<ItemSlot>();
            slots = SlotsFromTreeAttributes(tree, slots, modifiedSlots);
            for (int i = 0; i < modifiedSlots.Count; i++) DidModifyItemSlot(modifiedSlots[i]);
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            SlotsToTreeAttributes(slots, tree);
        }

        protected override ItemSlot NewSlot(int slotId)
        {
            if (slotId == 25) return new ItemSlotOffhand(this);
            return new ItemSlotSurvival(this);
        }

        public override void DiscardAll()
        {
            base.DiscardAll();
            for (int i = 0; i < Count; i++)
            {
                DidModifyItemSlot(this[i]);
            }
        }

        public override void OnOwningEntityDeath(Vec3d pos)
        {
            // Don't drop contents on death
        }
    }
}
