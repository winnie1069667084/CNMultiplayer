using HarmonyLib;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using ChatCommands;

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

    //[HarmonyPatch(typeof(MultiplayerWarmupComponent), "AfterStart")]//热身阶段加入大量bot（仅限攻城模式）
    internal class Patch_AfterStart
    {
        public static int NumberOfBotsTeam1;
        public static int NumberOfBotsTeam2;
        static void Postfix()
        {
            NumberOfBotsTeam1 = MultiplayerOptions.OptionType.NumberOfBotsTeam1.GetIntValue(MultiplayerOptions.MultiplayerOptionsAccessMode.CurrentMapOptions);
            NumberOfBotsTeam2 = MultiplayerOptions.OptionType.NumberOfBotsTeam2.GetIntValue(MultiplayerOptions.MultiplayerOptionsAccessMode.CurrentMapOptions);
            if (MultiplayerOptions.OptionType.GameType.GetStrValue(MultiplayerOptions.MultiplayerOptionsAccessMode.CurrentMapOptions) == "Siege" && NumberOfBotsTeam1 != 0 && NumberOfBotsTeam2 != 0)
            {
                MultiplayerOptions.Instance.GetOptionFromOptionType(MultiplayerOptions.OptionType.NumberOfBotsTeam1).UpdateValue(5);
                MultiplayerOptions.Instance.GetOptionFromOptionType(MultiplayerOptions.OptionType.NumberOfBotsTeam2).UpdateValue(5);
            }
        }
    }

    //[HarmonyPatch(typeof(MultiplayerWarmupComponent), "EndWarmup")]//热身结束后恢复bot数量（仅限攻城模式）
    internal class Patch_EndWarmup
    {
        static void Postfix()
        {
            if (MultiplayerOptions.OptionType.GameType.GetStrValue(MultiplayerOptions.MultiplayerOptionsAccessMode.CurrentMapOptions) == "Siege")
            {
                MultiplayerOptions.Instance.GetOptionFromOptionType(MultiplayerOptions.OptionType.NumberOfBotsTeam1).UpdateValue(Patch_AfterStart.NumberOfBotsTeam1);
                MultiplayerOptions.Instance.GetOptionFromOptionType(MultiplayerOptions.OptionType.NumberOfBotsTeam2).UpdateValue(Patch_AfterStart.NumberOfBotsTeam2);
            }
        }
    }
}
