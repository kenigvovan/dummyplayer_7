
using dummyplayer.src.behavior;
using dummyplayer.src.entity;
using dummyplayer.src.harmony;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.CommandAbbr;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;
using Vintagestory.Common;
using Vintagestory.GameContent;

namespace dummyplayer.src
{
    internal class dummyplayer: ModSystem
    {
        public static Harmony harmonyInstance;
        public const string harmonyID = "dummyplayer.Patches";
        public static Dictionary<string, EntityClonePlayer> playersClones;
        public static Dictionary<string, string> playersToDieUids;
        public static Dictionary<string, float> playersSavedHealth;
        public ICoreServerAPI sapi;
        public static ICoreAPI api;
        public static Config config;
        public override void StartPre(ICoreAPI api)
        {
            base.StartPre(api);
            dummyplayer.api = api;
        }
        public override bool ShouldLoad(EnumAppSide forSide)
        {
            return base.ShouldLoad(forSide);
        }
        public override void StartClientSide(ICoreClientAPI api)
        {
            base.StartClientSide(api);
            if(config == null)
            {
                config = new();
                config.loadDatabaseConfig(api);
            }
        }
        public override void Start(ICoreAPI api)
        {
            base.Start(api);
            api.RegisterEntity("EntityClonePlayer", typeof(EntityClonePlayer));
            api.RegisterEntityBehaviorClass("pvpTag", typeof(pvpTagEntityBehavior));
        }
        public override void StartServerSide(ICoreServerAPI api)
        {           
            base.StartServerSide(api);
            playersClones = new Dictionary<string, EntityClonePlayer>();
            playersToDieUids = new Dictionary<string, string>();
            playersSavedHealth = new Dictionary<string, float>();
            sapi = api;
            api.Event.PlayerDisconnect += onPlayerDisconnect;
            api.Event.PlayerNowPlaying += onPlayerNowPlaying;
            api.Event.ServerRunPhase(EnumServerRunPhase.ModsAndConfigReady, onModsAndConfigReady);

            api.Event.GameWorldSave += SaveModStructuresToSaveGame;

            api.ChatCommands.Create("dummyplayer")
                .RequiresPlayer().RequiresPrivilege(Privilege.controlserver)
                    .BeginSub("todie")
                        .HandleWith(onToDieCommand)
                    .EndSub();
            
            var s = api.WorldManager.SaveGame.GetData<Dictionary<string, string>>("dummyplayertodienew");
            if(s != null)
            {
                playersToDieUids = s;
            }
            var saveHealthDict = api.WorldManager.SaveGame.GetData<Dictionary<string, float>>("dummyplayersavehealth");
            if (saveHealthDict != null)
            {
                playersSavedHealth = saveHealthDict;
            }
            harmonyInstance = new Harmony(harmonyID);
            harmonyInstance.Patch(typeof(Vintagestory.API.Common.EntityPlayer).GetMethod("OnHurt"), prefix: new HarmonyMethod(typeof(harmPatch).GetMethod("Prefix_OnHurt")));
        }
        public void onModsAndConfigReady()
        {
            config = new();
            config.loadDatabaseConfig(api);
        }

        public void onPlayerNowPlaying(IServerPlayer player)
        {
            if(playersToDieUids.TryGetValue(player.PlayerUID, out string val))
            {
                playersToDieUids.Remove(player.PlayerUID);
                removeItemsFromPlayerInvAfterLogin(player);
                if (config.KILL_AFTER_LOGIN)
                {
                    //string val = "test";
                    IPlayer killer = this.sapi.World.PlayerByUid(val);
                    if (killer != null)
                    {
                        DamageSource dmgS = new DamageSource();
                        dmgS.SourceEntity = killer.Entity;
                        dmgS.Type = EnumDamageType.SlashingAttack;
                        dmgS.Source = EnumDamageSource.Player;
                        player.Entity.Die(EnumDespawnReason.Death, dmgS);
                    }
                    else
                    {
                        DamageSource dmgS = new DamageSource();
                        dmgS.Type = EnumDamageType.Suffocation;
                        dmgS.Source = EnumDamageSource.Internal;
                        player.Entity.Die(EnumDespawnReason.Death, dmgS);
                    }
                }
            }
            else if(playersClones.ContainsKey(player.PlayerUID) || dummyplayer.config.SPAWN_CLONE_ON_PLAYER_LEAVE)
            {
                //return drops to player and remove entity
                if(!playersClones.TryGetValue(player.PlayerUID, out EntityClonePlayer entityClonePlayer))
                {
                    return;
                }
                playersClones.Remove(player.PlayerUID);
                ITreeAttribute treeClone = entityClonePlayer.WatchedAttributes.GetTreeAttribute("health");
                float curHealth = treeClone.GetFloat("currenthealth");
                ITreeAttribute treePlayer = player.Entity.WatchedAttributes.GetTreeAttribute("health");
                treePlayer.SetFloat("currenthealth", curHealth);
                player.Entity.WatchedAttributes.MarkAllDirty();
                entityClonePlayer.returnDrops(player);
                entityClonePlayer.Die(EnumDespawnReason.Removed);
            }
            else if(playersSavedHealth.ContainsKey(player.PlayerUID))
            {
                ITreeAttribute treePlayer = player.Entity.WatchedAttributes.GetTreeAttribute("health");
                playersSavedHealth.TryGetValue(player.PlayerUID, out float currentHp);
                treePlayer.SetFloat("currenthealth", currentHp);
                player.Entity.WatchedAttributes.MarkAllDirty();
                playersSavedHealth.Remove(player.PlayerUID);
            }
        }
        public void removeItemsFromPlayerInvAfterLogin(IServerPlayer player)
        {
            //Players cloths and armor
            InventoryCharacter playerCharacter = ((InventoryCharacter)player.InventoryManager.GetOwnInventory("character"));
            //Player's hotbar
            IInventory playerHotbar = player.InventoryManager.GetHotbarInventory();
            InventoryPlayerBackPacks playerBackpacks = ((InventoryPlayerBackPacks)player.InventoryManager.GetOwnInventory("backpack"));
            //if(Config.Current.DROP_CLOTHS)
            for (int i = 0; i < 15; i++)
            {
                if (!config.DROP_CLOTHS && i < 12)
                {
                    continue;
                }

                if (!config.DROP_ARMOR && (i == 12 || i == 13 || i == 14))
                {
                    continue;
                }

                if (playerCharacter[i].Itemstack == null)
                    continue;
                playerCharacter[i].Itemstack = null;
                playerCharacter[i].MarkDirty();
            }

            if (config.DROP_HOTBAR)
            {
                for (int i = 0; i < playerHotbar.Count; i++)
                {
                    if (playerHotbar[i].Itemstack == null)
                        continue;
                    playerHotbar[i].Itemstack = null;
                    playerHotbar[i].MarkDirty();

                }
            }
            if (config.DROP_BAGS)
            {
                for (int i = 0; i < 4; i++)
                {
                    if (playerBackpacks[i].Itemstack == null)
                        continue;
                    playerBackpacks[i].Itemstack = null;
                    playerBackpacks[i].MarkDirty();
                }
            }
        }
        public static void SpawnPlayerClone(IServerPlayer player)
        {
            if (playersClones.ContainsKey(player.PlayerUID))
            {
                playersClones.TryGetValue(player.PlayerUID, out EntityClonePlayer ecp);
                ecp.Die(EnumDespawnReason.Removed);
                playersClones.Remove(player.PlayerUID);
            }
            EntityProperties entit1yType = player.Entity.World.GetEntityType(new AssetLocation("dummyplayer", "cloneplayer"));

            EntityClonePlayer entity = (EntityClonePlayer)player.Entity.World.ClassRegistry.CreateEntity(entit1yType);
            entity.setTimestampToDisappear(DateTimeOffset.UtcNow.ToUnixTimeSeconds() + config.TIME_TO_DISAPPEAR);
            
            entity.setSourceEntityUID(player.PlayerUID);
            entity.sourceName = player.PlayerName;
            
            entity.InChunkIndex3d = player.Entity.InChunkIndex3d;
            ITreeAttribute treetmp = player.Entity.WatchedAttributes.GetTreeAttribute("health").Clone();

            entity.Initialize(entit1yType.Clone(), player.Entity.World.Api, player.Entity.InChunkIndex3d);
           
            entity.ServerPos = player.Entity.ServerPos.Copy();
            entity.Pos.SetFrom(player.Entity.ServerPos);
            entity.WatchedAttributes.SetAttribute("health", treetmp);
            treetmp = player.Entity.WatchedAttributes.GetTreeAttribute("skinConfig").Clone();
            entity.WatchedAttributes.GetTreeAttribute("nametag").SetString("name", "[bot]" + player.PlayerName);
            entity.WatchedAttributes.GetTreeAttribute("nametag").SetBool("showtagonlywhentargeted", false);
            Random rand = player.Entity.World.Rand;
            entity.Pos.Motion.Set((0.125 - 0.25 * rand.NextDouble()) / 2.0, (0.1 + 0.1 * rand.NextDouble()) / 2.0, (0.125 - 0.25 * rand.NextDouble()) / 2.0);
            entity.WatchedAttributes.SetAttribute("skinConfig", treetmp);
            entity.WatchedAttributes.MarkAllDirty();
            entity.ServerPos.Pitch = 0;
            player.Entity.World.SpawnEntity(entity);
            entity.addDrops(player);
            EntityBehaviorTexturedClothing ebhtc = entity.GetBehavior<EntityBehaviorTexturedClothing>();
            InventoryBase inv = ebhtc.Inventory;
        }
        public static TextCommandResult onToDieCommand(TextCommandCallingArgs args)
        {

            IServerPlayer player = args.Caller.Player as IServerPlayer;
            SpawnPlayerClone(player);
            TextCommandResult tcr = new TextCommandResult();
            tcr.Status = EnumCommandStatus.Success;
            

            StringBuilder sb = new StringBuilder();
            sb.Append("Player uid | killer uid");
            foreach(var it in playersToDieUids)
            {
                sb.Append(it.Key + " | " + (it.Value.Equals("") ? "No killer" : it.Value));
            }
            player.SendMessage(0, sb.ToString(), EnumChatType.Notification);

            return tcr;
        }

        public void onPlayerDisconnect(IServerPlayer player)
        {
            pvpTagEntityBehavior pteb = player.Entity.GetBehavior<pvpTagEntityBehavior>();
            if(!dummyplayer.config.SPAWN_CLONE_ON_PLAYER_LEAVE && (pteb == null || pteb.timer <= 0))
            {
                return;
            }
            if (pteb != null)
            {
                pteb.timer = 0;
            }
            SpawnPlayerClone(player);
        }

        public void SaveModStructuresToSaveGame()
        {
            if (playersToDieUids != null)
            {
                sapi.WorldManager.SaveGame.StoreData<Dictionary<string, string>>("dummyplayertodienew", dummyplayer.playersToDieUids);
                sapi.Logger.Debug("[dummyplayer] Dispose::playersToDieUids saved in SaveGame");
            }
            if (playersSavedHealth != null)
            {
                sapi.WorldManager.SaveGame.StoreData<Dictionary<string, float>>("dummyplayersavehealth", dummyplayer.playersSavedHealth);
                sapi.Logger.Debug("[dummyplayer] Dispose::playersSavedHealth saved in SaveGame");
            }
        }
        public override void Dispose()
        {
            base.Dispose();
            if(harmonyInstance != null)
            {
                harmonyInstance.UnpatchAll(dummyplayer.harmonyID);
                harmonyInstance = null;
            }                 
            api = null;
            config = null;
        }       
    }
}
