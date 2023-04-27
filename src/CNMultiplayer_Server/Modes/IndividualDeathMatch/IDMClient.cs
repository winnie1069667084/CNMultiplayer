using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace CNMultiplayer.Modes.IndividualDeathMatch
{
    public class IDMClient : MissionMultiplayerGameModeBaseClient
    {
        public override bool IsGameModeUsingGold => false;

        public override bool IsGameModeTactical => false;

        public override bool IsGameModeUsingRoundCountdown => false;

        public override MissionLobbyComponent.MultiplayerGameType GameType => MissionLobbyComponent.MultiplayerGameType.FreeForAll;

        public override int GetGoldAmount()
        {
            return 0;
        }

        public override void OnGoldAmountChangedForRepresentative(MissionRepresentativeBase representative, int goldAmount)
        {
        }

        public override void AfterStart()
        {
            Mission.SetMissionMode(MissionMode.Battle, atStart: true);
        }
    }
}
