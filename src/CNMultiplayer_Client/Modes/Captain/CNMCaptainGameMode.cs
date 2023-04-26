using CNMultiplayer.Modes.Warmup;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Source.Missions;
using CNMultiplayer.Common;

#if CLIENT
using TaleWorlds.MountAndBlade.View.MissionViews;
using TaleWorlds.MountAndBlade.View;
#endif

namespace CNMultiplayer.Modes.Captain
{

#if CLIENT
    [ViewCreatorModule] // Exposes methods with ViewMethod attribute.
#endif

    internal class CNMCaptainGameMode : MissionBasedMultiplayerGameMode
    {
        private const string GameName = "CNMCaptain";

        public CNMCaptainGameMode()
            : base(GameName)
        { }

#if CLIENT
        [ViewMethod(GameName)]
        public static MissionView[] OpenCNMCaptain(Mission mission)
        {
            return new[]
            {
                ViewCreator.CreateMissionServerStatusUIHandler(),
                ViewCreator.CreateMultiplayerFactionBanVoteUIHandler(), // None Native
                ViewCreator.CreateMissionMultiplayerPreloadView(mission),
                ViewCreator.CreateMissionKillNotificationUIHandler(),
                ViewCreator.CreateMissionAgentStatusUIHandler(mission),
                ViewCreator.CreateMissionMainAgentEquipmentController(mission),
                ViewCreator.CreateMultiplayerMissionOrderUIHandler(mission),
                ViewCreator.CreateMissionMainAgentCheerBarkControllerView(mission),
                ViewCreator.CreateMissionMultiplayerEscapeMenu("Captain"),
                ViewCreator.CreateOrderTroopPlacerView(mission),
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
            MultiplayerRoundController roundController = new MultiplayerRoundController();
            CNMWarmupComponent warmupComponent = new CNMWarmupComponent(() => (new FlagDominationSpawnFrameBehavior(), new CNMCaptainSpawningBehavior(roundController)));

            MissionState.OpenNew(GameName, new MissionInitializerRecord(scene)
            { SceneUpgradeLevel = 3, SceneLevels = string.Empty },
            _ => (GameNetwork.IsServer)
            ? new MissionBehavior[] // Server side behavior
            {
                MissionLobbyComponent.CreateBehavior(),
                new MissionMultiplayerFlagDomination(MissionLobbyComponent.MultiplayerGameType.Captain),
                roundController,
                new SpawnComponent(new FlagDominationSpawnFrameBehavior(), new CNMCaptainSpawningBehavior(roundController)),
                warmupComponent,
                new MissionMultiplayerGameModeFlagDominationClient(),
                new MultiplayerTimerComponent(),
                new MultiplayerMissionAgentVisualSpawnComponent(),
                new ConsoleMatchStartEndHandler(),
                new MissionLobbyEquipmentNetworkComponent(),
                new CNMTeamSelectComponent(warmupComponent, roundController),
                new MissionHardBorderPlacer(),
                new MissionBoundaryPlacer(),
                new MissionBoundaryCrossingHandler(),
                new MultiplayerPollComponent(),
                new MultiplayerAdminComponent(),
                new MultiplayerGameNotificationsComponent(),
                new MissionOptionsComponent(),
                new MissionScoreboardComponent(new CaptainScoreboardData()),
                new MissionAgentPanicHandler(),
                new AgentVictoryLogic(),
                new AgentHumanAILogic(),
                new EquipmentControllerLeaveLogic(),
                new MultiplayerPreloadHelper()
            }

            : new MissionBehavior[] // Client side behavior
            {
                MissionLobbyComponent.CreateBehavior(),
                new CNMMissionNetworkComponent(),
                warmupComponent,
                new MissionMultiplayerGameModeFlagDominationClient(),
                new MultiplayerAchievementComponent(),
                new MultiplayerTimerComponent(),
                new MultiplayerRoundComponent(),
                new MultiplayerMissionAgentVisualSpawnComponent(),
                new ConsoleMatchStartEndHandler(),
                new MissionLobbyEquipmentNetworkComponent(),
                new CNMTeamSelectComponent(warmupComponent, roundController),
                new MissionHardBorderPlacer(),
                new MissionBoundaryPlacer(),
                new MissionBoundaryCrossingHandler(),
                new MultiplayerPollComponent(),
                new MultiplayerGameNotificationsComponent(),
                new MissionOptionsComponent(),
                new MissionScoreboardComponent(new CaptainScoreboardData()),
                new MissionMatchHistoryComponent(),
                new EquipmentControllerLeaveLogic(),
                new MissionRecentPlayersComponent(),
                new AgentVictoryLogic(),
                new MultiplayerPreloadHelper()
            }
            );
        }
    }
}