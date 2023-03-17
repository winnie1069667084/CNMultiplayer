using HarmonyLib;
using TaleWorlds.MountAndBlade;
using NetworkMessages.FromServer;

namespace CNMultiplayer
{
    [HarmonyPatch(typeof(MultiplayerTeamSelectComponent), "GetAutoTeamBalanceDifference")]
    internal class Patch_GetAutoTeamBalanceDifference//修改两个阵营允许的最大人数差（只能用HarmonyHelper才能生效）
    {
        public static void Postfix(ref int __result)
        {
            if (__result != 0)
            {
                __result = 1;
            }
        }
    }
}