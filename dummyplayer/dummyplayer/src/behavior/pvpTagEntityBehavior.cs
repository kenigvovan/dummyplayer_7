using berg.src;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;

namespace dummyplayer.src.behavior
{
    internal class pvpTagEntityBehavior : EntityBehavior
    {
        public float timer = 0;
        public bool playerMentionedStart = false;
        public bool playerMentionedEnd = true;
        public pvpTagEntityBehavior(Entity entity) : base(entity)
        {
        }

        public override string PropertyName()
        {
            return "pvpTag";
        }

        public override void DidAttack(DamageSource source, EntityAgent targetEntity, ref EnumHandling handled)
        {
            base.DidAttack(source, targetEntity, ref handled);
            return;
            /*if(source.SourceEntity is EntityPlayer targetPlayer)
            {
                pvpTagEntityBehavior tpeb = targetPlayer.GetBehavior<pvpTagEntityBehavior>();
                if(tpeb != null)
                {
                    if(targetPlayer.Api.Side == EnumAppSide.Server)
                    {
                        if (!playerMentionedStart)
                        {
                            playerMentionedStart = true;
                            playerMentionedEnd = false;
                            (targetPlayer.Player as IServerPlayer).SendMessage(GlobalConstants.InfoLogChatGroup, Lang.Get("dummyplayer:start_pvp_tag_timer", Config.Current.SECONDS_PVP_TAG_TIMER.Val), EnumChatType.Notification);
                        }
                    }
                    tpeb.timer = Config.Current.SECONDS_PVP_TAG_TIMER.Val;
                }                
            }*/
        }
        public override void OnEntityReceiveDamage(DamageSource damageSource, ref float damage)
        {
            base.OnEntityReceiveDamage(damageSource, ref damage);
            if (damageSource.SourceEntity is EntityPlayer ourPlayer && entity is EntityPlayer)
            {
                {
                    if (ourPlayer.Api.Side == EnumAppSide.Server)
                    {
                        if (!playerMentionedStart)
                        {
                            ((entity as EntityPlayer).Player as IServerPlayer).SendMessage(GlobalConstants.InfoLogChatGroup, Lang.Get("dummyplayer:start_pvp_tag_timer", Config.Current.SECONDS_PVP_TAG_TIMER.Val), EnumChatType.Notification);
                            playerMentionedStart = true;
                            playerMentionedEnd = false;
                        }
                        timer = Config.Current.SECONDS_PVP_TAG_TIMER.Val;

                        pvpTagEntityBehavior tpeb = ourPlayer.GetBehavior<pvpTagEntityBehavior>();
                        if (tpeb != null)
                        {
                            if (!tpeb.playerMentionedStart)
                            {
                                tpeb.playerMentionedStart = true;
                                tpeb.playerMentionedEnd = false;
                                (ourPlayer.Player as IServerPlayer).SendMessage(GlobalConstants.InfoLogChatGroup, Lang.Get("dummyplayer:start_pvp_tag_timer", Config.Current.SECONDS_PVP_TAG_TIMER.Val), EnumChatType.Notification);
                            }

                            tpeb.timer = Config.Current.SECONDS_PVP_TAG_TIMER.Val;
                        }

                    }
                }
            }
        }
        public override void OnGameTick(float deltaTime)
        {
            base.OnGameTick(deltaTime);
            if (entity.Api.Side == EnumAppSide.Server)
                if (timer <= 0)
                {
                    if (!playerMentionedEnd)
                    {
                        ((entity as EntityPlayer).Player as IServerPlayer).SendMessage(GlobalConstants.InfoLogChatGroup, Lang.Get("dummyplayer:end_pvp_tag_timer"), EnumChatType.Notification);
                        playerMentionedEnd = true;
                        playerMentionedStart = false;
                    }
                    return;
                }
                else
                {
                    timer -= deltaTime;
                }

        }
        public override void OnEntityDeath(DamageSource damageSourceForDeath)
        {
            base.OnEntityDeath(damageSourceForDeath);
            timer = 0;
            playerMentionedStart = false;
            playerMentionedEnd = true;
        }
        public override void Initialize(EntityProperties properties, JsonObject attributes)
        {
            base.Initialize(properties, attributes);
            timer = 0;
            playerMentionedEnd = true;
            playerMentionedStart = false;
        }
    }
}
