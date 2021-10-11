using Vintagestory.API.Common;
using Vintagestory.GameContent;
using VinterTweaks.Items.Tools;

namespace VinterTweaks
{
    public class VinterTweaks : ModSystem 
    {
        public override bool ShouldLoad(EnumAppSide forSide)
        {
            return true;
        }
        public override void Start (ICoreAPI api)
        {
            base.Start(api);
            api.RegisterItemClass("itemvtaxe", typeof(ItemVTAxe));
            api.RegisterItemClass("itemvtsaw", typeof(ItemVTSaw));
            BlockEntityFirewoodPile
        }
    }
}
