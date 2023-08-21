using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;

namespace dummyplayer.src.Inventory
{
    public class SavedInventory
    {
        public ItemSlot[] backPackSlots;

        public List<ItemStack> backPackContents = new List<ItemStack>();

        public SavedInventory()
        {
            backPackSlots = new ItemSlot[4];
        }
    }
}
