using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.MissionRepresentatives;
using TaleWorlds.ObjectSystem;

namespace CNMultiplayer.Modes.IndividualDeathMatch
{
    public class IDMServer : MissionMultiplayerGameModeBase
    {
        public override bool IsGameModeHidingAllAgentVisuals => false;

        public override bool IsGameModeUsingOpposingTeams => false;

        public override MissionLobbyComponent.MultiplayerGameType GetMissionType()
        {
            return MissionLobbyComponent.MultiplayerGameType.FreeForAll;
        }

        public override void OnRemoveBehavior()
        {
            base.OnRemoveBehavior();
        }

        public override void AfterStart()
        {
            BasicCultureObject gameculture = MBObjectManager.Instance.GetObject<BasicCultureObject>(MultiplayerOptions.OptionType.CultureTeam1.GetStrValue());
            Banner banner1 = new Banner(gameculture.BannerKey, gameculture.BackgroundColor1, gameculture.ForegroundColor1);
            Team team1 = Mission.Teams.Add(BattleSideEnum.Attacker, gameculture.BackgroundColor1, gameculture.ForegroundColor1, banner1, isPlayerGeneral: false);
            team1.SetIsEnemyOf(team1, isEnemyOf: true);
        }

        protected override void HandleEarlyNewClientAfterLoadingFinished(NetworkCommunicator networkPeer)
        {
            networkPeer.AddComponent<FFAMissionRepresentative>();
        }

        protected override void HandleNewClientAfterSynchronized(NetworkCommunicator networkPeer)
        {
            MissionPeer component = networkPeer.GetComponent<MissionPeer>();
            component.Team = Mission.AttackerTeam;
        }
    }

}