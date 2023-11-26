using CNMultiplayer.Common;
using CNMultiplayer.Modes.Warmup;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Source.Missions;

#if CLIENT
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.MissionViews;
using TaleWorlds.MountAndBlade.Multiplayer;
using TaleWorlds.MountAndBlade.Multiplayer.View.MissionViews;
#endif

namespace CNMultiplayer.Modes.IndividualDeathMatch
{
#if CLIENT
    [ViewCreatorModule] // Exposes methods with ViewMethod attribute.
#endif
    internal class IDMGameMode : MissionBasedMultiplayerGameMode
    {
        private const string GameName = "IndividualDeathMatch";

        public IDMGameMode() : base(GameName) { }

#if CLIENT
        [ViewMethod(GameName)]
        public static MissionView[] OpenIDM(Mission mission)
        {
            return new[]
            {
                MultiplayerViewCreator.CreateMissionServerStatusUIHandler(),
                MultiplayerViewCreator.CreateMissionMultiplayerPreloadView(mission),
                MultiplayerViewCreator.CreateMissionMultiplayerFFAView(),
                MultiplayerViewCreator.CreateMissionKillNotificationUIHandler(),
                ViewCreator.CreateMissionAgentStatusUIHandler(mission),
                ViewCreator.CreateMissionMainAgentEquipmentController(mission),
                ViewCreator.CreateMissionMainAgentCheerBarkControllerView(mission),
                MultiplayerViewCreator.CreateMissionMultiplayerEscapeMenu("FreeForAll"),
                MultiplayerViewCreator.CreateMultiplayerEndOfBattleUIHandler(),
                MultiplayerViewCreator.CreateMissionScoreBoardUIHandler(mission, true),
                MultiplayerViewCreator.CreateLobbyEquipmentUIHandler(),
                MultiplayerViewCreator.CreatePollProgressUIHandler(),
                MultiplayerViewCreator.CreateMultiplayerMissionHUDExtensionUIHandler(),
                MultiplayerViewCreator.CreateMultiplayerMissionDeathCardUIHandler(null),
                ViewCreator.CreateOptionsUIHandler(),
                ViewCreator.CreateMissionMainAgentEquipDropView(mission),
                ViewCreator.CreateMissionBoundaryCrossingView(),
                new MissionBoundaryWallView()
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
                new IDMServer(),
                new MissionMultiplayerFFAClient(),
                new MultiplayerTimerComponent(),
                new MultiplayerMissionAgentVisualSpawnComponent(),
                new ConsoleMatchStartEndHandler(),
                new SpawnComponent(new FFASpawnFrameBehavior(), new CNMWarmupSpawningBehavior()),
                new MissionLobbyEquipmentNetworkComponent(),
                new CNMTeamSelectComponent(warmupComponent, roundController),
                new MissionHardBorderPlacer(),
                new MissionBoundaryPlacer(),
                new MissionBoundaryCrossingHandler(),
                new MultiplayerPollComponent(),
                new MultiplayerAdminComponent(),
                new MultiplayerGameNotificationsComponent(),
                new MissionOptionsComponent(),
                new MissionScoreboardComponent(new FFAScoreboardData()),
                new MissionAgentPanicHandler(),
                new AgentHumanAILogic(),
                new EquipmentControllerLeaveLogic(),
                new MultiplayerPreloadHelper()
            }

            : new MissionBehavior[] // Client side behavior
            {
                MissionLobbyComponent.CreateBehavior(),
                new MissionMultiplayerFFAClient(),
                new MultiplayerAchievementComponent(),
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
                new MissionScoreboardComponent(new FFAScoreboardData()),
                MissionMatchHistoryComponent.CreateIfConditionsAreMet(),
                new EquipmentControllerLeaveLogic(),
                new MissionRecentPlayersComponent(),
                new MultiplayerPreloadHelper()
            }
            );
        }
    }
}
