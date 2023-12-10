using CNMultiplayer.Modes.Warmup;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Source.Missions;
using TaleWorlds.MountAndBlade.Multiplayer;
using CNMultiplayer.Common.Modes.CNMSiege;

namespace CNMultiplayer.Client.Modes.CNMSiege
{
    internal class CNMSiegeGameMode : MissionBasedMultiplayerGameMode
    {
        private const string GameName = "CNMSiege";

        public CNMSiegeGameMode() : base(GameName) { }

        [MissionMethod]
        public override void StartMultiplayerGame(string scene)
        {
            CNMWarmupComponent warmupComponent = new CNMWarmupComponent(() => (new SiegeSpawnFrameBehavior(), new CNMSiegeSpawningBehavior()));

            MissionState.OpenNew(GameName, new MissionInitializerRecord(scene),
            _ => new MissionBehavior[]
            {
                MissionLobbyComponent.CreateBehavior(),
                warmupComponent,
                new CNMSiegeClient(),
                new MultiplayerAchievementComponent(),
                new MultiplayerTimerComponent(),
                new MultiplayerMissionAgentVisualSpawnComponent(),
                new ConsoleMatchStartEndHandler(),
                new MissionLobbyEquipmentNetworkComponent(),
                new MultiplayerTeamSelectComponent(),
                new MissionHardBorderPlacer(),
                new MissionBoundaryPlacer(),
                new MissionBoundaryCrossingHandler(),
                new MultiplayerPollComponent(),
                new MultiplayerAdminComponent(),
                new MultiplayerGameNotificationsComponent(),
                new MissionOptionsComponent(),
                new MissionScoreboardComponent(new CNMSiegeScoreboardData()),
                MissionMatchHistoryComponent.CreateIfConditionsAreMet(),
                new EquipmentControllerLeaveLogic(),
                new MissionRecentPlayersComponent(),
                new VoiceChatHandler(),
                new MultiplayerPreloadHelper()
            });
        }
    }
}
