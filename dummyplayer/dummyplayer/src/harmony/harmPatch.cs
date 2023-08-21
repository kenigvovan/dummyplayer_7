using berg.src;
using dummyplayer.src.behavior;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;

namespace dummyplayer.src.harmony
{
    [HarmonyPatch]
    internal class harmPatch
    {
        /*public virtual void OnHurt(DamageSource dmgSource, float damage)
        {
        }*/
        public static void Prefix_OnHurt(EntityPlayer __instance, DamageSource damageSource, float damage)
        {
            pvpTagEntityBehavior pteb = __instance.GetBehavior<pvpTagEntityBehavior>();


            if (damageSource != null && (damageSource.SourceEntity is EntityPlayer || damageSource.CauseEntity is EntityPlayer))
            {
                EntityPlayer sourcePlayer;
                if (damageSource.SourceEntity is EntityPlayer)
                {
                    sourcePlayer = damageSource.SourceEntity as EntityPlayer;
                }
                else if (damageSource.CauseEntity is EntityPlayer)
                {
                    sourcePlayer = damageSource.CauseEntity as EntityPlayer;
                }
                else
                {
                    return;
                }
                if (pteb != null)
                {
                    pteb.timer = 30;
                    if (!pteb.playerMentionedStart)
                    {
                        pteb.playerMentionedEnd = false;
                        ((pteb.entity as EntityPlayer).Player as IServerPlayer).SendMessage(GlobalConstants.InfoLogChatGroup, Lang.Get("dummyplayer:start_pvp_tag_timer", Config.Current.SECONDS_PVP_TAG_TIMER.Val), EnumChatType.Notification);
                    }
                }
                else
                {
                    var behtmp = new pvpTagEntityBehavior(__instance);
                    behtmp.timer = 30;
                    __instance.AddBehavior(behtmp);
                    pteb.playerMentionedEnd = false;
                    ((behtmp.entity as EntityPlayer).Player as IServerPlayer).SendMessage(GlobalConstants.InfoLogChatGroup, Lang.Get("dummyplayer:start_pvp_tag_timer", Config.Current.SECONDS_PVP_TAG_TIMER.Val), EnumChatType.Notification);
                }

                pteb = sourcePlayer.GetBehavior<pvpTagEntityBehavior>();

                if (pteb != null)
                {
                    pteb.timer = 30;
                    if (!pteb.playerMentionedStart)
                    {
                        pteb.playerMentionedEnd = false;
                        ((pteb.entity as EntityPlayer).Player as IServerPlayer).SendMessage(GlobalConstants.InfoLogChatGroup, Lang.Get("dummyplayer:start_pvp_tag_timer", Config.Current.SECONDS_PVP_TAG_TIMER.Val), EnumChatType.Notification);
                    }
                }
                else
                {
                    var behtmp = new pvpTagEntityBehavior(__instance);
                    behtmp.timer = 30;
                    __instance.AddBehavior(behtmp);
                    pteb.playerMentionedEnd = false;
                    ((behtmp.entity as EntityPlayer).Player as IServerPlayer).SendMessage(GlobalConstants.InfoLogChatGroup, Lang.Get("dummyplayer:start_pvp_tag_timer", Config.Current.SECONDS_PVP_TAG_TIMER.Val), EnumChatType.Notification);
                }
            }
        }


    }
}
