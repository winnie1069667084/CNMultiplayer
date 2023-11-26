using CNMultiplayer.Modes.Warmup;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Source.Missions;
using CNMultiplayer.Common;
using TaleWorlds.MountAndBlade.Multiplayer;

#if CLIENT
using TaleWorlds.MountAndBlade.View.MissionViews;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.Multiplayer.View.MissionViews;
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
                MultiplayerViewCreator.CreateLobbyEquipmentUIHandler(),
                MultiplayerViewCreator.CreateMissionServerStatusUIHandler(),
                MultiplayerViewCreator.CreateMultiplayerFactionBanVoteUIHandler(),
                MultiplayerViewCreator.CreateMissionMultiplayerPreloadView(mission),
                MultiplayerViewCreator.CreateMissionKillNotificationUIHandler(),
                ViewCreator.CreateMissionAgentStatusUIHandler(mission),
                ViewCreator.CreateMissionMainAgentEquipmentController(mission),
                ViewCreator.CreateMissionMainAgentCheerBarkControllerView(mission),
                MultiplayerViewCreator.CreateMissionMultiplayerEscapeMenu("Captain"),
                MultiplayerViewCreator.CreateMultiplayerMissionOrderUIHandler(mission),
                ViewCreator.CreateMissionAgentLabelUIHandler(mission),
                ViewCreator.CreateOrderTroopPlacerView(mission),
                MultiplayerViewCreator.CreateMultiplayerTeamSelectUIHandler(),
                MultiplayerViewCreator.CreateMissionScoreBoardUIHandler(mission, false),
                MultiplayerViewCreator.CreateMultiplayerEndOfRoundUIHandler(),
                MultiplayerViewCreator.CreateMultiplayerEndOfBattleUIHandler(),
                MultiplayerViewCreator.CreatePollProgressUIHandler(),
                new MissionItemContourControllerView(),
                new MissionAgentContourControllerView(),
                MultiplayerViewCreator.CreateMultiplayerMissionHUDExtensionUIHandler(),
                MultiplayerViewCreator.CreateMultiplayerMissionDeathCardUIHandler(null),
                MultiplayerViewCreator.CreateMissionFlagMarkerUIHandler(),
                ViewCreator.CreateOptionsUIHandler(),
                ViewCreator.CreateMissionMainAgentEquipDropView(mission),
                ViewCreator.CreateMissionBoundaryCrossingView(),
                new MissionBoundaryWallView(),
                new SpectatorCameraView(),
                MultiplayerViewCreator.CreateMultiplayerFactionBanVoteUIHandler(), // None Native
                MultiplayerViewCreator.CreateMultiplayerMissionVoiceChatUIHandler(),
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
                new MissionMultiplayerFlagDomination(MultiplayerGameType.Captain),
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
                new VoiceChatHandler(),
                new MultiplayerPreloadHelper()
            }

            : new MissionBehavior[] // Client side behavior
            {
                MissionLobbyComponent.CreateBehavior(),
                warmupComponent,
                new MultiplayerAchievementComponent(),
                new MissionMultiplayerGameModeFlagDominationClient(),
                new MultiplayerRoundComponent(),
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
                MissionMatchHistoryComponent.CreateIfConditionsAreMet(),
                new EquipmentControllerLeaveLogic(),
                new MissionRecentPlayersComponent(),
                new MultiplayerPreloadHelper()
            }
            );
        }
    }
}