using HarmonyLib;
using NetworkMessages.FromServer;
using TaleWorlds.MountAndBlade;

namespace HarmonyPatches
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

    [HarmonyPatch(typeof(MultiplayerTeamSelectComponent), "UpdateTeams")]
    internal class Patch_UpdateTeams//调用自动平衡系统
    {
        public static bool Prefix(MultiplayerTeamSelectComponent __instance)
        {
            if (MultiplayerOptions.OptionType.GameType.GetStrValue(MultiplayerOptions.MultiplayerOptionsAccessMode.CurrentMapOptions) != "Captain")
            {
                __instance.BalanceTeams();
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(MultiplayerTeamSelectComponent), "BalanceTeams")]
    internal class Patch_BalanceTeams//自动平衡系统
    {
        public static bool Prefix(MultiplayerTeamSelectComponent __instance)
        {

            if (MultiplayerOptions.OptionType.AutoTeamBalanceThreshold.GetIntValue(MultiplayerOptions.MultiplayerOptionsAccessMode.CurrentMapOptions) != 0)
            {
                var sc = new Patch_BalanceTeams();
                int iscore = sc.GetScoreForTeam(Mission.Current.AttackerTeam);
                int jscore = sc.GetScoreForTeam(Mission.Current.DefenderTeam);
                int i = __instance.GetPlayerCountForTeam(Mission.Current.AttackerTeam);
                int j = __instance.GetPlayerCountForTeam(Mission.Current.DefenderTeam);
                while (i > j + 1 + MultiplayerTeamSelectComponent.GetAutoTeamBalanceDifference((AutoTeamBalanceLimits)MultiplayerOptions.OptionType.AutoTeamBalanceThreshold.GetIntValue(MultiplayerOptions.MultiplayerOptionsAccessMode.CurrentMapOptions)) && iscore >= jscore)
                {
                    MissionPeer missionPeer = null;
                    foreach (NetworkCommunicator networkCommunicator in GameNetwork.NetworkPeers)
                    {
                        if (networkCommunicator.IsSynchronized)
                        {
                            MissionPeer component = networkCommunicator.GetComponent<MissionPeer>();
                            if (((component != null) ? component.Team : null) != null && component.Team == __instance.Mission.AttackerTeam && (missionPeer == null || component.Score >= missionPeer.Score))
                            {
                                missionPeer = component;
                            }
                        }
                    }
                    __instance.ChangeTeamServer(missionPeer.GetNetworkPeer(), Mission.Current.DefenderTeam);
                    GameNetwork.BeginBroadcastModuleEvent();
                    GameNetwork.WriteMessage(new ServerMessage("[服务器]: 进攻方玩家 " + missionPeer.Name + " 被自动平衡！", false));
                    GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None, null);
                    i--;
                    j++;
                }
                while (j > i + 1 + MultiplayerTeamSelectComponent.GetAutoTeamBalanceDifference((AutoTeamBalanceLimits)MultiplayerOptions.OptionType.AutoTeamBalanceThreshold.GetIntValue(MultiplayerOptions.MultiplayerOptionsAccessMode.CurrentMapOptions)) && jscore >= iscore)
                {
                    MissionPeer missionPeer2 = null;
                    foreach (NetworkCommunicator networkCommunicator2 in GameNetwork.NetworkPeers)
                    {
                        if (networkCommunicator2.IsSynchronized)
                        {
                            MissionPeer component2 = networkCommunicator2.GetComponent<MissionPeer>();
                            if (((component2 != null) ? component2.Team : null) != null && component2.Team == __instance.Mission.DefenderTeam && (missionPeer2 == null || component2.Score >= missionPeer2.Score))
                            {
                                missionPeer2 = component2;
                            }
                        }
                    }
                    __instance.ChangeTeamServer(missionPeer2.GetNetworkPeer(), Mission.Current.AttackerTeam);
                    GameNetwork.BeginBroadcastModuleEvent();
                    GameNetwork.WriteMessage(new ServerMessage("[服务器]: 防守方玩家 " + missionPeer2.Name + " 被自动平衡！", false));
                    GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None, null);
                    i++;
                    j--;
                }
            }
            return false;
        }

        public int GetScoreForTeam(Team team)
        {
            int num = 0;
            foreach (NetworkCommunicator networkPeer in GameNetwork.NetworkPeers)
            {
                MissionPeer component = networkPeer.GetComponent<MissionPeer>();
                if (component?.Team != null && component.Team == team)
                {
                    num += component.Score;
                }
            }
            return num;
        }
    }
}