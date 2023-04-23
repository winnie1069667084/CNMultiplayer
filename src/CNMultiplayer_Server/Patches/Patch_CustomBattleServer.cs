using HarmonyLib;
using TaleWorlds.MountAndBlade.Diamond;

namespace CNMultiplayer.Patches
{
    [HarmonyPatch(typeof(CustomBattleServer), "UpdateBattleStats")]
    internal class Patch_UpdateBattleStats
    {
        public static bool Prefix()
        {
            return false;
        }
    }

    [HarmonyPatch(typeof(CustomBattleServer), "BattleFinished")]
    internal class Patch_BattleFinished
    {
        public static bool Prefix()
        {
            return false;
        }
    }

    [HarmonyPatch(typeof(CustomBattleServer), "BattleStarted")]
    internal class Patch_BattleStarted
    {
        public static bool Prefix()
        {
            return false;
        }
    }

    [HarmonyPatch(typeof(CustomBattleServer), "BeforeStartingNextBattle")]
    internal class Patch_BeforeStartingNextBattle
    {
        public static bool Prefix(IBadgeComponent ____badgeComponent)
        {
            IBadgeComponent badgeComponent = ____badgeComponent;
            badgeComponent?.OnStartingNextBattle();
            return false;
        }
    }
}
