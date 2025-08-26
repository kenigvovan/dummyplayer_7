using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using dummyplayer.src.entity;
using HarmonyLib;
using PlayerCorpse.Systems;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.Client.NoObf;

namespace dummyplayer.src.compat.CO
{
    public class COCompat: ModSystem
    {
        public override double ExecuteOrder()
        {
            return 1.03;
        }
        public override bool ShouldLoad(EnumAppSide forSide)
        {
            if (dummyplayer.api == null)
            {
                return false;
            }
            return dummyplayer.api.ModLoader.IsModEnabled("overhaullib");
        }
        public override void StartServerSide(ICoreServerAPI api)
        {
            if (!dummyplayer.api.ModLoader.IsModEnabled("overhaullib"))
            {
                return;
            }
            EntityClonePlayer.coActive = true;
        }
    }
}
