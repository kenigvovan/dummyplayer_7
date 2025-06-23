using HarmonyLib;
using PlayerCorpse.Systems;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.Client.NoObf;
using Vintagestory.GameContent;

namespace dummyplayer.src.compat.playercorpse
{
    public class PlayerCorpseCompat: ModSystem
    {
        public static Harmony harmonyInstance;
        public const string harmonyID = "dummyplayer.PlayerCorpseCompat.Patches";
        public static Mod ModPC;
        public override double ExecuteOrder()
        {
            return 1.03;
        }
        public override bool ShouldLoad(EnumAppSide forSide)
        {
            if(dummyplayer.api == null)
            {
                return false;
            }
            return dummyplayer.api.ModLoader.IsModEnabled("playercorpse");
        }
        public override void StartServerSide(ICoreServerAPI api)
        {
            if(!dummyplayer.api.ModLoader.IsModEnabled("playercorpse"))
            {
                return;
            }
            harmonyInstance = new Harmony(harmonyID);
            ModPC = api.ModLoader.GetModSystem<PlayerCorpse.Systems.DeathContentManager>().Mod;
            harmonyInstance.Patch(typeof(DeathContentManager).GetMethod("OnEntityDeath", BindingFlags.NonPublic | BindingFlags.Instance), prefix: new HarmonyMethod(typeof(harmPatch).GetMethod("Prefix_DeathContentManager_OnEntityDeath")));
        }
    }
}
