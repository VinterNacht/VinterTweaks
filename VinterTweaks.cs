using HarmonyLib;
using System.Reflection;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using VinterTweaks.blockentities;
using VinterTweaks.Blocks.Coins;

namespace VinterTweaks
{
    public class VinterTweaks : ModSystem 
    {

        private Harmony _harmony = new Harmony("harmoniousVT");

        public override bool ShouldLoad(EnumAppSide forSide)
        {
            return true;
        }

        public override void StartServerSide(ICoreServerAPI sapi)
        {
            sapi.RegisterCommand("getplayerpos", "Outputs player's position to server console", "/getplayerpos PlayerName", (IServerPlayer player, int groupId, CmdArgs args) => {
                string playerName = args.PopWord();
                foreach (IServerPlayer onlinePlayer in sapi.World.AllOnlinePlayers)
                {
                    if (onlinePlayer.PlayerName.Equals(playerName))
                    {
                        var pos = onlinePlayer?.Entity?.Pos?.AsBlockPos;
                        if (pos != null)
                        {
                            sapi.Logger.Notification("getplayerpos: Player {0} is at {1}", onlinePlayer.PlayerName, pos);
                            return;
                        }
                    }
                }
                sapi.Logger.Notification("getplayerpos: Could not find player named {0}", playerName);

            }, Privilege.tp);
            _harmony = new Harmony("goxmeor.lostinthewild");
            _harmony.PatchAll(Assembly.GetExecutingAssembly());
        }

        public override void Start (ICoreAPI api)
        {
            base.Start(api);
            api.RegisterBlockClass("aethercoinspile", typeof(AetherCoinsPile));
            api.RegisterBlockEntityClass("beaethercoinpile", typeof(BEAetherCoinsPile));
            PatchGame();
           
        }
        public override void Dispose()
        {
            var harmony = new Harmony("harmoniousfog");
            harmony.UnpatchAll("harmoniousfog");
        }

        private void PatchGame()
        {
            Mod.Logger.Event("Applying Harmony patches");
            var harmony = new Harmony("harmoniousvt");
            var original = typeof(Vintagestory.Client.NoObf.HudDebugScreen).GetMethod("OnFinalizeFrame");
            var patches = Harmony.GetPatchInfo(original);
            if (patches != null && patches.Owners.Contains("harmoniousvt"))
            {
                return;
            }
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }

        private void UnPatchGame()
        {
            Mod.Logger.Event("Unapplying Harmony patches");

            _harmony.UnpatchAll();
        }

    }
}

[HarmonyPatch]
class BlockEntityHUDPatches
{

    [HarmonyPrefix]
    [HarmonyPatch(typeof(Vintagestory.Client.NoObf.HudDebugScreen), "OnFinalizeFrame")]
    private static bool Patch_HudDebugScreen_OnFinalizeFrame_Prefix(
        Vintagestory.Client.NoObf.HudDebugScreen __instance, float dt)
    {
        __instance.CallMethod("UpdateGraph", dt);
        return false;
    }

}


//[HarmonyPatch(typeof(ServerLogger))]
//[HarmonyPatch("Log")]
//public class Patch_ServerLogger_Log
//{
//    public static bool Prefix(EnumLogType logType, ref string message, params object[] args)
//    {
//        if (message == "Placing player at {0} {1} {2}")
//        {
//            message = "Placing player";
//        }
//        else if (message == ("Teleporting player {0} to {1}"))
//        {
//            message = "Teleporting player {0}";
//        }
//        else if (message == ("Teleporting entity {0} to {1}"))
//        {
//            message = "Teleporting entity {0}";
//        }
//        return true; // run original method
//    }
//}

public static class HarmonyReflectionExtensions
{
    /// <summary>
    ///     Calls a method within an instance of an object, via reflection. This can be an internal or private method within another assembly.
    /// </summary>
    /// <typeparam name="T">The return type, expected back from the method.</typeparam>
    /// <param name="instance">The instance to call the method from.</param>
    /// <param name="method">The name of the method to call.</param>
    /// <param name="args">The arguments to pass to the method.</param>
    /// <returns>The return value of the reflected method call.</returns>
    public static void CallMethod(this object instance, string method, params object[] args)
    {
        AccessTools.Method(instance.GetType(), method)?.Invoke(instance, args);
    }
}

