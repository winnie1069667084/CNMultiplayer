using HarmonyLib;
using TaleWorlds.MountAndBlade;

namespace CNMultiplayer.Patches
{
    [HarmonyPatch(typeof(BannerlordNetwork), "EndMultiplayerLobbyMission")]
    internal class Patch_EndMultiplayerLobbyMission
    {
        public static bool Prefix()
        {
            return false;
        }
    }
}
