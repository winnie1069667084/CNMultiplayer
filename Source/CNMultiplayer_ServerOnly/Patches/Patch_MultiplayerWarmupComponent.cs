using HarmonyLib;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace Patches
{
    [HarmonyPatch(typeof(MultiplayerWarmupComponent), "CheckForWarmupProgressEnd")]//热身结束条件修改
    internal class Patch_CheckForWarmupProgressEnd
    {
        static bool Prefix(ref bool __result, MissionMultiplayerGameModeBase ____gameMode, MultiplayerTimerComponent ____timerComponent)
        {
            int num = GetCurrentMissionPlayersNum();
            __result = ____gameMode.CheckForWarmupEnd() || ____timerComponent.GetRemainingTime(false) <= 30f || num >= MultiplayerOptions.OptionType.MinNumberOfPlayersForMatchStart.GetIntValue(MultiplayerOptions.MultiplayerOptionsAccessMode.CurrentMapOptions);
            return false;
        }

        static public int GetCurrentMissionPlayersNum()
        {
            int num = 0;
            foreach (NetworkCommunicator networkPeer in GameNetwork.NetworkPeers)
            {
                MissionPeer component = networkPeer.GetComponent<MissionPeer>();
                if (((component != null) ? component.Team : null) != null && component.Team.Side != BattleSideEnum.None)
                    num++;
            }
            return num;
        }
    }

    [HarmonyPatch(typeof(MultiplayerWarmupComponent), "CanMatchStartAfterWarmup")]//热身结束条件修改
    internal class Patch_CanMatchStartAfterWarmup
    {
        static bool Prefix(ref bool __result)
        {
            int num = Patch_CheckForWarmupProgressEnd.GetCurrentMissionPlayersNum();
            bool[] array = new bool[2];
            foreach (NetworkCommunicator networkCommunicator in GameNetwork.NetworkPeers)
            {
                MissionPeer component = networkCommunicator.GetComponent<MissionPeer>();
                if (((component != null) ? component.Team : null) != null && component.Team.Side != BattleSideEnum.None)
                {
                    array[(int)component.Team.Side] = true;
                }
                if (array[1] && array[0] && num >= MultiplayerOptions.OptionType.MinNumberOfPlayersForMatchStart.GetIntValue(MultiplayerOptions.MultiplayerOptionsAccessMode.CurrentMapOptions))
                {
                    __result = true;
                    return false;
                }
            }
            __result = false;
            return false;
        }
    }
}
