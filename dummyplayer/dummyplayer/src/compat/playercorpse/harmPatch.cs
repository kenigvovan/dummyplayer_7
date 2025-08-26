using CommonLib.Utils;
using dummyplayer.src.entity;
using HarmonyLib;
using PlayerCorpse.Entities;
using PlayerCorpse.Systems;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;
using static PlayerCorpse.Config;

namespace dummyplayer.src.compat.playercorpse
{
    [HarmonyPatch]
    public class harmPatch
    {
        public static bool Prefix_DeathContentManager_OnEntityDeath(DeathContentManager __instance, Entity entity, DamageSource damageSource)
        {
            if (entity is EntityClonePlayer entityClone)
            {
                OnPlayerDeath_Copy(entity as EntityClonePlayer, damageSource);
                return false;
            }
            return true;
        }

        //19.7
        private static void OnPlayerDeath_Copy(EntityClonePlayer clonePlayer, DamageSource damageSource)
        {
            bool isKeepContent = clonePlayer.Properties?.Server?.Attributes?.GetBool("keepContents") ?? false;
            if (isKeepContent)
            {
                return;
            }

            var corpseEntity = CreateCorpseEntity(clonePlayer);
            if (corpseEntity.Inventory != null && !corpseEntity.Inventory.Empty)
            {
                if (PlayerCorpse.Core.Config.CreateWaypoint == CreateWaypointMode.Always)
                {
                    CreateDeathPoint(clonePlayer);
                }

                // Save content for /returnthings
                if (PlayerCorpse.Core.Config.MaxDeathContentSavedPerPlayer > 0)
                {
                    SaveDeathContent(corpseEntity.Inventory, clonePlayer);
                }

                // Spawn corpse
                if (PlayerCorpse.Core.Config.CreateCorpse)
                {
                    clonePlayer.Api.World.SpawnEntity(corpseEntity);

                    string message = string.Format(
                        "Created {0} at {1}, id {2}",
                        corpseEntity.GetName(),
                        corpseEntity.SidedPos.XYZ.RelativePos(clonePlayer.Api),
                        corpseEntity.EntityId);
                    
                    PlayerCorpseCompat.ModPC.Logger.Notification(message);
                    if (PlayerCorpse.Core.Config.DebugMode)
                    {
                        clonePlayer.Api.BroadcastMessage(message);
                    }
                }

                // Or drop all if corpse creations is disabled
                else
                {
                    corpseEntity.Inventory.DropAll(corpseEntity.Pos.XYZ);
                }
            }
            else
            {
                string message = string.Format(
                    "Inventory is empty, {0}'s corpse not created",
                    corpseEntity.OwnerName);

                PlayerCorpseCompat.ModPC.Logger.Notification(message);
                if (PlayerCorpse.Core.Config.DebugMode)
                {
                    clonePlayer.Api.BroadcastMessage(message);
                }
            }
        }
        private static EntityPlayerCorpse CreateCorpseEntity(EntityClonePlayer clonePlayer)
        {
            var entityType = clonePlayer.Api.World.GetEntityType(new AssetLocation(PlayerCorpse.Constants.ModId, "playercorpse"));

            if (clonePlayer.Api.World.ClassRegistry.CreateEntity(entityType) is not EntityPlayerCorpse corpse)
            {
                throw new Exception("Unable to instantiate player corpse");
            }

            corpse.OwnerUID = clonePlayer.sourceEntityUID;
            corpse.OwnerName = clonePlayer.sourceName;
            corpse.CreationTime = clonePlayer.Api.World.Calendar.TotalHours;
            corpse.CreationRealDatetime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            corpse.Inventory = TakeContentFromPlayer(clonePlayer);

            // Fix dancing corpse issue
            BlockPos floorPos = TryFindFloor(clonePlayer.Pos.AsBlockPos, clonePlayer.Api);

            // Attempt to align the corpse to the center of the block so that it does not crawl higher
            Vec3d pos = floorPos.ToVec3d().Add(.5, 0, .5);

            corpse.ServerPos.SetPos(pos);
            corpse.Pos.SetPos(pos);
            corpse.World = clonePlayer.Api.World;

            return corpse;
        }
        private static BlockPos TryFindFloor(BlockPos pos, ICoreAPI api)
        {
            var floorPos = new BlockPos(pos.dimension);
            for (int i = pos.Y; i > 0; i--)
            {
                floorPos.Set(pos.X, i, pos.Z);
                var block = api.World.BlockAccessor.GetBlock(floorPos);
                if (block.BlockId != 0 && block.CollisionBoxes?.Length > 0)
                {
                    floorPos.Set(pos.X, i + 1, pos.Z);
                    return floorPos;
                }
            }
            return pos;
        }
        public static void CreateDeathPoint(EntityClonePlayer clonePlayer)
        {
            var wmm = clonePlayer.Api.ModLoader.GetModSystem<WorldMapManager>(true).MapLayers.FirstOrDefault((MapLayer ml) => ml is WaypointMapLayer) as WaypointMapLayer;
            Waypoint waypoint = new Waypoint
            {
                Color = (1| -16777216),
                OwningPlayerUid = clonePlayer.sourceEntityUID,
                Position = clonePlayer.Pos.AsBlockPos.ToVec3d(),
                Title = "death",
                Icon = PlayerCorpse.Core.Config.WaypointIcon,
                Pinned = PlayerCorpse.Core.Config.PinWaypoint,
                Guid = Guid.NewGuid().ToString()
            };
            wmm.Waypoints.Add(waypoint);
            Waypoint[] array = wmm.Waypoints.Where((Waypoint p) => p.OwningPlayerUid == clonePlayer.sourceEntityUID).ToArray();
            //wmm.ResendWaypoints(player);
            //return array.Length - 1;
            return;
            /*var format = "/waypoint addati {0} ={1} ={2} ={3} {4} {5} Death: {6}";
            var icon = PlayerCorpse.Core.Config.WaypointIcon;
            var pos = byPlayer.Entity.ServerPos.AsBlockPos;
            var isPinned = PlayerCorpse.Core.Config.PinWaypoint;
            var color = PlayerCorpse.Core.Config.WaypointColor;
            var deathTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            string message = string.Format(format, icon, pos.X, pos.Y, pos.Z, isPinned, color, deathTime);

            byPlayer.Entity.Api.ChatCommands.ExecuteUnparsed(message, new TextCommandCallingArgs
            {
                Caller = new Caller
                {
                    Player = byPlayer,
                    Pos = byPlayer.Entity.Pos.XYZ,
                    FromChatGroupId = GlobalConstants.CurrentChatGroup
                }
            });*/
        }
        private static InventoryGeneric TakeContentFromPlayer(EntityClonePlayer clonePlayer)
        {
            var inv = new InventoryGeneric(GetMaxCorpseSlots(clonePlayer), $"playercorpse-{clonePlayer.sourceEntityUID}", clonePlayer.Api);

            int lastSlotId = 0;
            //foreach (var invClassName in PlayerCorpse.Core.Config.SaveInventoryTypes)
            {
                // Skip armor if it does not drop after death
               /* bool isDropArmor = clonePlayer.Properties.Server?.Attributes?.GetBool("dropArmorOnDeath") ?? false;
                if (invClassName == GlobalConstants.characterInvClassName && !isDropArmor)
                {
                    continue;
                }*/

                // XSkills slots fix
                /*if (invClassName.Equals(GlobalConstants.backpackInvClassName) &&
                    byPlayer.InventoryManager.GetOwnInventory("xskillshotbar") != null)
                {
                    int i = 0;
                    var backpackInv = byPlayer.InventoryManager.GetOwnInventory(invClassName);
                    foreach (var slot in backpackInv)
                    {
                        if (i > backpackInv.Count - 4) // Extra backpack slots
                        {
                            break;
                        }
                        inv[lastSlotId++].Itemstack = TakeSlotContent(slot);
                    }
                    continue;
                }*/

                foreach (var slot in clonePlayer.GearInventory)
                {
                    inv[lastSlotId++].Itemstack = TakeSlotContent(slot);
                }
                foreach (var slot in clonePlayer.OtherInventory)
                {
                    inv[lastSlotId++].Itemstack = TakeSlotContent(slot);
                }
            }

            return inv;
        }
        private static ItemStack? TakeSlotContent(ItemSlot slot)
        {
            if (slot.Empty)
            {
                return null;
            }

            // Skip the player's clothing (not armor)
            if (slot.Inventory.ClassName == GlobalConstants.characterInvClassName)
            {
                bool isArmor = slot.Itemstack.ItemAttributes?["protectionModifiers"].Exists ?? false;
                if (!isArmor)
                {
                    return null;
                }
            }

            return slot.TakeOutWhole();
        }
        private static int GetMaxCorpseSlots(EntityClonePlayer clonePlayer)
        {
            return clonePlayer.GearInventory.Count + clonePlayer.OtherInventory.Count;
        }
        public static void SaveDeathContent(InventoryGeneric inventory, EntityClonePlayer clonePlayer)
        {
            string path = GetDeathDataPath(clonePlayer);
            string[] files = GetDeathDataFiles(clonePlayer);

            for (int i = files.Length - 1; i > PlayerCorpse.Core.Config.MaxDeathContentSavedPerPlayer - 2; i--)
            {
                File.Delete(files[i]);
            }

            var tree = new TreeAttribute();
            inventory.ToTreeAttributes(tree);

            string name = $"inventory-{DateTime.Now:yyyy-MM-dd-HH-mm-ss}.dat";
            File.WriteAllBytes($"{path}/{name}", tree.ToBytes());
        }
        public static string GetDeathDataPath(EntityClonePlayer clonePlayer)
        {
            ICoreAPI api = clonePlayer.Api;
            string uidFixed = Regex.Replace(clonePlayer.sourceEntityUID, "[^0-9a-zA-Z]", "");
            string localPath = Path.Combine("ModData", api?.World?.SavegameIdentifier ?? "null", "playercorpse", uidFixed);
            return api.GetOrCreateDataPath(localPath);
        }

        public static string[] GetDeathDataFiles(EntityClonePlayer clonePlayer)
        {
            string path = GetDeathDataPath(clonePlayer);
            return Directory
                .GetFiles(path)
                .OrderByDescending(f => new FileInfo(f).Name)
                .ToArray();
        }

    }
}
