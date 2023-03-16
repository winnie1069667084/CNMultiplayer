using HarmonyLib;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace Patches
{
    [HarmonyPatch(typeof(MultiplayerWarmupComponent), "CheckForWarmupProgressEnd")]//热身结束条件修改
    internal class Patch_CheckForWarmupProgressEnd
    {
        static void Postfix(ref bool __result, MissionMultiplayerGameModeBase ____gameMode, MultiplayerTimerComponent ____timerComponent)
        {
            int num = 0;
            foreach (NetworkCommunicator networkPeer in GameNetwork.NetworkPeers)
            {
                MissionPeer component = networkPeer.GetComponent<MissionPeer>();
                if (((component != null) ? component.Team : null) != null && component.Team.Side != BattleSideEnum.None)
                    num++;
            }
            __result = ____gameMode.CheckForWarmupEnd() || ____timerComponent.GetRemainingTime(false) <= 30f || num >= MultiplayerOptions.OptionType.MinNumberOfPlayersForMatchStart.GetIntValue(MultiplayerOptions.MultiplayerOptionsAccessMode.CurrentMapOptions);
        }
    }
}
