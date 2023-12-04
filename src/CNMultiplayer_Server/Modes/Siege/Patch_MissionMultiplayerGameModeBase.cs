using HarmonyLib;
using TaleWorlds.MountAndBlade;

namespace CNMultiplayer.Modes.Siege
{
    [HarmonyPatch(typeof(MissionMultiplayerGameModeBase), "AddCosmeticItemsToEquipment")]//禁用大厅自定义装备
    internal class Patch_MissionMultiplayerGameMode
    {
        public static bool Prefix()
        {
            return false;
        }
    }
}
