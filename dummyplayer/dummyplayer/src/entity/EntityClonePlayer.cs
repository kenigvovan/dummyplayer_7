using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.GameContent;
using System.Collections.Generic;
using System;
using Vintagestory.API.MathTools;
using System.IO;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;
using Vintagestory.API.Config;
using Vintagestory.Common;
using berg.src;
using dummyplayer.src.Inventory;

namespace dummyplayer.src.entity
{
    public class EntityClonePlayer : EntityAgent
    {
        protected InventoryBase inv;
        protected InventoryBase invOthers;
        string sourceEntityUID;
        long timeStampToDisappear;
        double counter = 0;

        public override bool StoreWithChunk
        {
            get { return true; }
        }


        public override IInventory GearInventory
        {
            get
            {
                return inv;
            }
        }

        public int ActiveSlotNumber
        {
            get { return WatchedAttributes.GetInt("ActiveSlotNumber", 16); }
            set { WatchedAttributes.SetInt("ActiveSlotNumber", GameMath.Clamp(value, 16, 25)); WatchedAttributes.MarkPathDirty("ActiveSlotNumber"); }
        }

        public override ItemSlot RightHandItemSlot
        {
            get
            {
                return inv[ActiveSlotNumber];
            }
        }

        public override ItemSlot LeftHandItemSlot
        {
            get
            {
                return inv[16];
            }
        }

        public override byte[] LightHsv
        {
            get
            {
                byte[] rightHsv = RightHandItemSlot?.Itemstack?.Block?.GetLightHsv(World.BlockAccessor, null, RightHandItemSlot.Itemstack);
                byte[] leftHsv = LeftHandItemSlot?.Itemstack?.Block?.GetLightHsv(World.BlockAccessor, null, LeftHandItemSlot.Itemstack);

                if (rightHsv == null) return leftHsv;
                if (leftHsv == null) return rightHsv;

                float totalval = rightHsv[2] + leftHsv[2];
                float t = leftHsv[2] / totalval;

                return new byte[]
                {
                    (byte)(leftHsv[0] * t + rightHsv[0] * (1-t)),
                    (byte)(leftHsv[1] * t + rightHsv[1] * (1-t)),
                    Math.Max(leftHsv[2], rightHsv[2])
                };
            }
        }

        public EntityClonePlayer() : base()
        {
            //WatchedAttributes.GetString()
            inv = new InventoryNPCGear(null, null, 17);
            invOthers = new InventoryOther(null, null, 29);
        }
        public void setSourceEntityUID(string uid)
        {
            sourceEntityUID = uid;
        }
        public override void Initialize(EntityProperties properties, ICoreAPI api, long chunkindex3d)
        {
            base.Initialize(properties, api, chunkindex3d);

            inv.LateInitialize("gearinv-" + EntityId, api);
            invOthers.LateInitialize("invOther-" + EntityId, api);

            AnimManager.HeadController = new EntityHeadController(AnimManager, this, Properties.Client.LoadedShape);
        }

        public override void OnGameTick(float dt)
        {
            base.OnGameTick(dt);
            if (World.Side == EnumAppSide.Server)
            {
                counter += dt;
                if (counter > 5)
                {
                    counter = 0;
                    if (DateTimeOffset.UtcNow.ToUnixTimeSeconds() >= timeStampToDisappear)
                    {
                        if (dummyplayer.playersClones.ContainsKey(sourceEntityUID))
                        {
                            dummyplayer.playersClones.Remove(sourceEntityUID);
                        }
                        if (dummyplayer.playersSavedHealth.ContainsKey(sourceEntityUID))
                        {
                            ITreeAttribute treeClone = WatchedAttributes.GetTreeAttribute("health");
                            float curHealth = treeClone.GetFloat("currenthealth");
                            dummyplayer.playersSavedHealth[sourceEntityUID] = curHealth;
                        }
                        else
                        {
                            ITreeAttribute treeClone = WatchedAttributes.GetTreeAttribute("health");
                            float curHealth = treeClone.GetFloat("currenthealth");
                            dummyplayer.playersSavedHealth.Add(sourceEntityUID, curHealth);
                        }
                        Die(EnumDespawnReason.Removed);
                    }
                }
            }
        }

        public override void OnEntitySpawn()
        {
            base.OnEntitySpawn();
            //this.
            if (World.Side == EnumAppSide.Client)
            {
                (Properties.Client.Renderer as EntityShapeRenderer).DoRenderHeldItem = true;
            }
            if (World.Side == EnumAppSide.Server && sourceEntityUID != null)
            {
                dummyplayer.playersClones.Add(sourceEntityUID, this);
            }
        }
        public override void OnEntityLoaded()
        {
            base.OnEntityLoaded();
            if (World.Side == EnumAppSide.Server && sourceEntityUID != "")
            {
                //this.Die();
                if (!dummyplayer.playersClones.ContainsKey(sourceEntityUID))
                {
                    dummyplayer.playersClones.Add(sourceEntityUID, this);
                }
            }
        }
        public override void ToBytes(BinaryWriter writer, bool forClient)
        {
            TreeAttribute tree;
            WatchedAttributes["gearInv"] = tree = new TreeAttribute();
            inv.ToTreeAttributes(tree);

            TreeAttribute treeO;
            WatchedAttributes["invOther"] = treeO = new TreeAttribute();
            inv.ToTreeAttributes(treeO);
            WatchedAttributes.SetString("sourceEntityUID", sourceEntityUID);

            base.ToBytes(writer, forClient);
        }

        public void setTimestampToDisappear(long val)
        {
            timeStampToDisappear = val;
        }
        public override void FromBytes(BinaryReader reader, bool forClient)
        {
            base.FromBytes(reader, forClient);

            TreeAttribute tree = WatchedAttributes["gearInv"] as TreeAttribute;
            if (tree != null) inv.FromTreeAttributes(tree);

            TreeAttribute treeO = WatchedAttributes["invOther"] as TreeAttribute;
            if (treeO != null) inv.FromTreeAttributes(treeO);

            sourceEntityUID = WatchedAttributes.GetString("sourceEntityUID");
        }

        public override void OnInteract(EntityAgent byEntity, ItemSlot slot, Vec3d hitPosition, EnumInteractMode mode)
        {
            base.OnInteract(byEntity, slot, hitPosition, mode);

            /*if ((byEntity as EntityPlayer)?.Controls.Sneak == true && mode == EnumInteractMode.Interact && byEntity.World.Side == EnumAppSide.Server)
            {
                inv.DiscardAll();
                WatchedAttributes.MarkAllDirty();
            }*/
        }

        public override void Die(EnumDespawnReason reason = EnumDespawnReason.Death, DamageSource damageSourceForDeath = null)
        {
            base.Die(reason, damageSourceForDeath);
            dummyplayer.sapi.Logger.Error("reason " + reason.ToString());

            if (World.Side == EnumAppSide.Server)
                dummyplayer.playersClones.Remove(sourceEntityUID);
            if (reason != EnumDespawnReason.Removed && reason != EnumDespawnReason.Unload && reason != EnumDespawnReason.OutOfRange)
            {
                inv.DropAll(SidedPos.XYZ);
                invOthers.DropAll(SidedPos.XYZ);
                if (damageSourceForDeath != null && damageSourceForDeath.SourceEntity is EntityPlayer entityPlayer)
                {
                    dummyplayer.playersToDieUids.Add(sourceEntityUID, entityPlayer.PlayerUID);
                    return;
                }
                dummyplayer.playersToDieUids.Add(sourceEntityUID, "");
            }
        }
        public void addDrops(IServerPlayer player)
        {
            //Players cloths and armor
            InventoryCharacter playerCharacter = (InventoryCharacter)player.InventoryManager.GetOwnInventory("character");
            //Player's hotbar
            IInventory playerHotbar = player.InventoryManager.GetHotbarInventory();
            InventoryPlayerBackPacks playerBackpacks = (InventoryPlayerBackPacks)player.InventoryManager.GetOwnInventory("backpack");
            if (playerCharacter.Count > inv.Count || invOthers.Count < playerHotbar.Count + 4)
            {
                inv = new InventoryNPCGear(null, null, playerCharacter.Count);
                invOthers = new InventoryOther(null, null, playerHotbar.Count + 4);
            }
            //if(Config.Current.DROP_CLOTHS)
            for (int i = 0; i < 15; i++)
            {
                if (!Config.Current.DROP_CLOTHS.Val && i < 12)
                {
                    continue;
                }

                if (!Config.Current.DROP_ARMOR.Val && (i == 12 || i == 13 || i == 14))
                {
                    continue;
                }

                if (playerCharacter[i].Itemstack == null)
                    continue;
                inv[i].Itemstack = playerCharacter[i].Itemstack.Clone();
                //playerCharacter[i].Itemstack = null;
                //playerCharacter[i].MarkDirty();
            }

            if (Config.Current.DROP_HOTBAR.Val)
            {
                for (int i = 0; i < playerHotbar.Count; i++)
                {
                    if (playerHotbar[i].Itemstack == null)
                        continue;
                    var t = playerHotbar[i];
                    invOthers[i].Itemstack = playerHotbar[i].Itemstack.Clone();
                    //playerHotbar[i].Itemstack = null;
                    //playerHotbar[i].MarkDirty();

                }
            }
            if (Config.Current.DROP_BAGS.Val)
            {
                for (int i = 0; i < 4; i++)
                {
                    if (playerBackpacks[i].Itemstack == null)
                        continue;
                    invOthers[i + 11].Itemstack = playerBackpacks[i].Itemstack.Clone();
                    //playerBackpacks[i].Itemstack = null;
                    //playerBackpacks[i].MarkDirty();
                }
            }
        }
        public void returnDrops(IServerPlayer player)
        {
            //Players cloths and armor
            InventoryCharacter playerCharacter = (InventoryCharacter)player.InventoryManager.GetOwnInventory("character");
            //Player's hotbar
            IInventory playerHotbar = player.InventoryManager.GetHotbarInventory();
            InventoryPlayerBackPacks playerBackpacks = (InventoryPlayerBackPacks)player.InventoryManager.GetOwnInventory("backpack");
            if (playerCharacter.Count + playerHotbar.Count + playerBackpacks.Count > inv.Count)
            {
                inv = new InventoryNPCGear(null, null, playerCharacter.Count + playerHotbar.Count + playerBackpacks.Count);
            }
            //Armor, cloths
            for (int i = 0; i < 15; i++)
            {
                if (!Config.Current.DROP_ARMOR.Val && (i == 12 || i == 13 || i == 14))
                {
                    continue;
                }
                if (inv[i].Itemstack == null)
                    continue;
                playerCharacter[i].Itemstack = inv[i].Itemstack.Clone();
                playerCharacter[i].MarkDirty();
                inv[i].Itemstack = null;
            }

            //Hotbar
            if (Config.Current.DROP_HOTBAR.Val)
            {
                for (int i = 0; i < 11; i++)
                {
                    if (inv[15 + i].Itemstack == null)
                        continue;
                    playerHotbar[i].Itemstack = inv[15 + i].Itemstack;
                    playerHotbar[i].MarkDirty();
                    inv[15 + i].Itemstack = null;
                }
            }

            //Bags
            if (Config.Current.DROP_BAGS.Val)
            {
                for (int i = 0; i < inv.Count - 11 - 15; i++)
                {
                    if (inv[i + 11 + 15].Itemstack == null)
                        continue;
                    playerBackpacks[i].Itemstack = inv[i + 11 + 15].Itemstack;
                    playerBackpacks[i].MarkDirty();
                    inv[i + 11 + 15].Itemstack = null;
                }
            }
        }
        public override bool ReceiveDamage(DamageSource damageSource, float damage)
        {
            float dmg = handleDefense(damage, damageSource);
            return base.ReceiveDamage(damageSource, dmg);
        }


        Dictionary<int, EnumCharacterDressType[]> clothingDamageTargetsByAttackTacket = new Dictionary<int, EnumCharacterDressType[]>()
        {
            { 0, new EnumCharacterDressType[] { EnumCharacterDressType.Head, EnumCharacterDressType.Face, EnumCharacterDressType.Neck } },
            { 1, new EnumCharacterDressType[] { EnumCharacterDressType.UpperBody, EnumCharacterDressType.UpperBodyOver, EnumCharacterDressType.Shoulder, EnumCharacterDressType.Arm, EnumCharacterDressType.Hand } },
            { 2, new EnumCharacterDressType[] { EnumCharacterDressType.LowerBody, EnumCharacterDressType.Foot } }
        };
        private float handleDefense(float damage, DamageSource dmgSource)
        {
            // Does not protect against non-attack damages
            EnumDamageType type = dmgSource.Type;
            if (type != EnumDamageType.BluntAttack && type != EnumDamageType.PiercingAttack && type != EnumDamageType.SlashingAttack) return damage;
            if (dmgSource.Source == EnumDamageSource.Internal || dmgSource.Source == EnumDamageSource.Suicide) return damage;

            ItemSlot armorSlot;
            IInventory inv = GearInventory;
            double rnd = Api.World.Rand.NextDouble();


            int attackTarget;

            if ((rnd -= 0.2) < 0)
            {
                // Head
                armorSlot = inv[12];
                attackTarget = 0;
            }
            else if ((rnd -= 0.5) < 0)
            {
                // Body
                armorSlot = inv[13];
                attackTarget = 1;
            }
            else
            {
                // Legs
                armorSlot = inv[14];
                attackTarget = 2;
            }

            // Apply full damage if no armor is in this slot
            if (armorSlot.Empty || !(armorSlot.Itemstack.Item is ItemWearable))
            {
                EnumCharacterDressType[] dressTargets = clothingDamageTargetsByAttackTacket[attackTarget];
                EnumCharacterDressType target = dressTargets[Api.World.Rand.Next(dressTargets.Length)];

                ItemSlot targetslot = GearInventory[(int)target];
                if (!targetslot.Empty)
                {
                    // Wolf: 10 hp damage = 10% condition loss
                    // Ram: 10 hp damage = 2.5% condition loss
                    // Bronze locust: 10 hp damage = 5% condition loss
                    float mul = 0.25f;
                    if (type == EnumDamageType.SlashingAttack) mul = 1f;
                    if (type == EnumDamageType.PiercingAttack) mul = 0.5f;

                    float diff = -damage / 100 * mul;

                    if (Math.Abs(diff) > 0.05)
                    {
                        Api.World.PlaySoundAt(new AssetLocation("sounds/effect/clothrip"), this);
                    }

                    (targetslot.Itemstack.Collectible as ItemWearable)?.ChangeCondition(targetslot, diff);
                }

                return damage;
            }

            ProtectionModifiers protMods = (armorSlot.Itemstack.Item as ItemWearable).ProtectionModifiers;

            int weaponTier = dmgSource.DamageTier;
            float flatDmgProt = protMods.FlatDamageReduction;
            float percentProt = protMods.RelativeProtection;

            for (int tier = 1; tier <= weaponTier; tier++)
            {
                bool aboveTier = tier > protMods.ProtectionTier;

                float flatLoss = aboveTier ? protMods.PerTierFlatDamageReductionLoss[1] : protMods.PerTierFlatDamageReductionLoss[0];
                float percLoss = aboveTier ? protMods.PerTierRelativeProtectionLoss[1] : protMods.PerTierRelativeProtectionLoss[0];

                if (aboveTier && protMods.HighDamageTierResistant)
                {
                    flatLoss /= 2;
                    percLoss /= 2;
                }

                flatDmgProt -= flatLoss;
                percentProt *= 1 - percLoss;
            }

            // Durability loss is the one before the damage reductions
            float durabilityLoss = 0.5f + damage * Math.Max(0.5f, (weaponTier - protMods.ProtectionTier) * 3);
            int durabilityLossInt = GameMath.RoundRandom(Api.World.Rand, durabilityLoss);

            // Now reduce the damage
            damage = Math.Max(0, damage - flatDmgProt);
            damage *= 1 - Math.Max(0, percentProt);

            armorSlot.Itemstack.Collectible.DamageItem(Api.World, this, armorSlot, durabilityLossInt);

            if (armorSlot.Empty)
            {
                Api.World.PlaySoundAt(new AssetLocation("sounds/effect/toolbreak"), this);
            }

            return damage;
        }
    }
}
