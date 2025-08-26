using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Datastructures;
using Vintagestory.Client.NoObf;

namespace dummyplayer.src.gui
{
    public class HudPvpState : HudElement
    {
        public HudPvpState(ICoreClientAPI capi) : base(capi)
        {
            
        }
        public void ComposeGui()
        {
            
            //ElementBounds bgBounds = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
            //bgBounds.BothSizing = ElementSizing.FitToChildren;
            /* float num = 850f;
             ElementBounds bounds1 = new ElementBounds()
             {
                 Alignment = EnumDialogArea.CenterBottom,
                 BothSizing = ElementSizing.Fixed,
                 fixedWidth = num,
                 fixedHeight = 100
             };*/
            HudStatbar bar = null;
            foreach (var gui in this.capi.Gui.OpenedGuis)
            {
                if(gui is HudStatbar)
                {
                    bar = (HudStatbar)gui;
                    break;
                }
            }
            if (bar != null)
            {
                var hb = bar.Composers["statbar"].GetStatbar("saturationstatbar");
                if (hb != null)
                {
                    ElementBounds dialogBounds = ElementBounds.Fixed((int)hb.Bounds.absX - 50, (int)hb.Bounds.absY - 150);
                    ElementBounds dialogBounds2 = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.CenterBottom);
                    dialogBounds.BothSizing = ElementSizing.Fixed;
                    //bgBounds.absOffsetY = hb.Bounds.absY - 100;
                    dialogBounds.Alignment = EnumDialogArea.LeftTop;
                    Composers["pvpstate"] = capi.Gui.CreateCompo("pvpstate-statbar", dialogBounds);
                  
                    Composers["pvpstate"].AddIconButton("dummyplayer:swords-emblem", null, new ElementBounds().WithFixedSize(32, 32));
                    Composers["pvpstate"].Compose();
                    TryOpen();
                }
            }
        }
    }
}
