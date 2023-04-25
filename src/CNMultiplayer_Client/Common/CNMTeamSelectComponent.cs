using NetworkMessages.FromServer;
using TaleWorlds.MountAndBlade;

namespace CNMultiplayer.Common
{
    internal class CNMTeamSelectComponent : MultiplayerTeamSelectComponent
    {
        public CNMTeamSelectComponent(MultiplayerWarmupComponent warmupComponent, MultiplayerRoundController roundController)
        {
            _roundController = roundController;
            _warmupComponent = warmupComponent;
        }

        private readonly MultiplayerRoundController _roundController;
        private readonly MultiplayerWarmupComponent _warmupComponent;

#if SERVER
        public override void OnRemoveBehavior()
        {
            base.OnRemoveBehavior();
            if (_roundController is null)
            {
                OnUpdateTeams -= BalanceTeams;
            }
            else 
            {
                _warmupComponent.OnWarmupEnded -= BalanceTeams;
                _roundController.OnPostRoundEnded -= OnRoundEnded;
            }
        }

        public override void AfterStart()
        {
            base.AfterStart();
            if (_roundController is null && !_warmupComponent.IsInWarmup)
            {
                OnUpdateTeams += BalanceTeams;
            }
            else
            {
                _warmupComponent.OnWarmupEnded += BalanceTeams;
                _roundController.OnPostRoundEnded += OnRoundEnded;
            }
        }

        //游戏将要结束的回合尾不再触发自动平衡
        private void OnRoundEnded()
        {
            if (!_roundController.IsMatchEnding)
            {
                BalanceTeams();
            }
        }

        private new void BalanceTeams()
        {
            int autoTeamBalanceThreshold = MultiplayerOptions.OptionType.AutoTeamBalanceThreshold.GetIntValue(MultiplayerOptions.MultiplayerOptionsAccessMode.CurrentMapOptions);
            if (autoTeamBalanceThreshold != 0)
            {
                int iscore = GetScoreForTeam(Mission.AttackerTeam);
                int jscore = GetScoreForTeam(Mission.DefenderTeam);
                int i = GetPlayerCountForTeam(Mission.AttackerTeam);
                int j = GetPlayerCountForTeam(Mission.DefenderTeam);
                while (i > j + 1 + autoTeamBalanceThreshold && iscore >= jscore)
                {
                    MissionPeer missionPeer = null;
                    foreach (NetworkCommunicator networkPeer in GameNetwork.NetworkPeers)
                    {
                        if (!networkPeer.IsSynchronized)
                        {
                            continue;
                        }
                        MissionPeer component = networkPeer.GetComponent<MissionPeer>();
                        if ((component?.Team) != null && component.Team == Mission.AttackerTeam && (missionPeer == null || component.Score >= missionPeer.Score))
                        {
                            missionPeer = component;
                        }
                    }
                    ChangeTeamServer(missionPeer.GetNetworkPeer(), Mission.DefenderTeam);
                    GameNetwork.BeginBroadcastModuleEvent();
                    GameNetwork.WriteMessage(new ServerMessage("[服务器]: 进攻方玩家 " + missionPeer.Name + " 被自动平衡！", false));
                    GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None, null);
                    i--;
                    j++;
                }
                while (j > i + 1 + autoTeamBalanceThreshold && jscore >= iscore)
                {
                    MissionPeer missionPeer2 = null;
                    foreach (NetworkCommunicator networkPeer2 in GameNetwork.NetworkPeers)
                    {
                        if (!networkPeer2.IsSynchronized)
                        {
                            continue;
                        }
                        MissionPeer component2 = networkPeer2.GetComponent<MissionPeer>();
                        if ((component2?.Team) != null && component2.Team == Mission.DefenderTeam && (missionPeer2 == null || component2.Score >= missionPeer2.Score))
                        {
                            missionPeer2 = component2;
                        }
                    }
                    ChangeTeamServer(missionPeer2.GetNetworkPeer(), Mission.AttackerTeam);
                    GameNetwork.BeginBroadcastModuleEvent();
                    GameNetwork.WriteMessage(new ServerMessage("[服务器]: 防守方玩家 " + missionPeer2.Name + " 被自动平衡！", false));
                    GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None, null);
                    i++;
                    j--;
                }
            }
        }

        public static int GetScoreForTeam(Team team)
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
#endif
    }
}