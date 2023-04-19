using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Source.Missions;
using CNMultiplayer.Modes.Warmup;

#if CLIENT
using TaleWorlds.MountAndBlade.View.MissionViews;
using TaleWorlds.MountAndBlade.View;
#endif

namespace CNMultiplayer.Modes.Siege
{
#if CLIENT
    [ViewCreatorModule] // Exposes methods with ViewMethod attribute.
#endif
    internal class CNMSiegeGameMode : MissionBasedMultiplayerGameMode
    {
        private const string GameName = "CNMSiege";

        public CNMSiegeGameMode()
            : base(GameName)
        { }
#if CLIENT
        [ViewMethod(GameName)]
        public static MissionView[] OpenCNMSiege(Mission mission)
        {
            return new[]
            {
                ViewCreator.CreateMissionServerStatusUIHandler(),
                ViewCreator.CreateMultiplayerFactionBanVoteUIHandler(), // None Native
                ViewCreator.CreateMissionMultiplayerPreloadView(mission),
                ViewCreator.CreateMissionKillNotificationUIHandler(),
                ViewCreator.CreateMissionAgentStatusUIHandler(mission),
                ViewCreator.CreateMissionMainAgentEquipmentController(mission),
                ViewCreator.CreateMissionMainAgentCheerBarkControllerView(mission),
                ViewCreator.CreateMissionMultiplayerEscapeMenu("Siege"),
                ViewCreator.CreateMultiplayerEndOfBattleUIHandler(),
                ViewCreator.CreateMissionAgentLabelUIHandler(mission),
                ViewCreator.CreateMultiplayerTeamSelectUIHandler(),
                ViewCreator.CreateMissionScoreBoardUIHandler(mission, isSingleTeam: false),
                ViewCreator.CreateMultiplayerEndOfRoundUIHandler(),
                ViewCreator.CreateLobbyEquipmentUIHandler(),
                ViewCreator.CreatePollProgressUIHandler(),
                ViewCreator.CreateMultiplayerMissionHUDExtensionUIHandler(),
                ViewCreator.CreateMultiplayerMissionDeathCardUIHandler(),
                ViewCreator.CreateMissionFlagMarkerUIHandler(), // Draw flags when pressing ALT.
                ViewCreator.CreateOptionsUIHandler(),
                ViewCreator.CreateMissionMainAgentEquipDropView(mission),
                ViewCreator.CreateMissionBoundaryCrossingView(),
                new MissionItemContourControllerView(), // Draw contour of item on the ground when pressing ALT.
                new MissionAgentContourControllerView(),
                new MissionBoundaryWallView(),
                new SpectatorCameraView(), // None Native
            };
        }
#endif

        public override void StartMultiplayerGame(string scene)
        {
            MissionState.OpenNew(GameName, new MissionInitializerRecord(scene)
            { SceneUpgradeLevel = 3, SceneLevels = string.Empty },
            _ => (GameNetwork.IsServer)
            ? new MissionBehavior[] // Server side behavior
            {
                MissionLobbyComponent.CreateBehavior(),
                new CNMSiegeServer(),
                new SpawnComponent(new SiegeSpawnFrameBehavior(), new CNMSiegeSpawningBehavior()),
                new CNMWarmupComponent(() => (new SiegeSpawnFrameBehavior(), new CNMSiegeSpawningBehavior())),
                new MissionMultiplayerSiegeClient(),
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
                new MissionScoreboardComponent(new SiegeScoreboardData()),
                new MissionAgentPanicHandler(),
                new AgentHumanAILogic(),
                new EquipmentControllerLeaveLogic(),
                new MultiplayerPreloadHelper()
            }

            : new MissionBehavior[] // Client side behavior
            {
                MissionLobbyComponent.CreateBehavior(),
                new CNMWarmupComponent(() => (new SiegeSpawnFrameBehavior(), new CNMSiegeSpawningBehavior())),
                new MissionMultiplayerSiegeClient(),
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
                new MultiplayerGameNotificationsComponent(),
                new MissionOptionsComponent(),
                new MissionScoreboardComponent(new SiegeScoreboardData()),
                new MissionMatchHistoryComponent(),
                new EquipmentControllerLeaveLogic(),
                new MissionRecentPlayersComponent(),
                new MultiplayerPreloadHelper()
            });
        }
    }
}
