using CNMultiplayer.Common;
using CNMultiplayer.Modes.Warmup;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Source.Missions;
using TaleWorlds.MountAndBlade.Multiplayer;

namespace CNMultiplayer.Modes.Siege
{

    internal class CNMSiegeGameMode : MissionBasedMultiplayerGameMode
    {
        private const string GameName = "CNMSiege";

        public CNMSiegeGameMode()
            : base(GameName)
        { }

        public override void StartMultiplayerGame(string scene)
        {
            CNMWarmupComponent warmupComponent = new CNMWarmupComponent(() => (new SiegeSpawnFrameBehavior(), new CNMSiegeSpawningBehavior()));

            MissionState.OpenNew(GameName, new MissionInitializerRecord(scene)
            { SceneUpgradeLevel = 3, SceneLevels = string.Empty },
            _ => new MissionBehavior[]
            {
                MissionLobbyComponent.CreateBehavior(),
                new CNMSiegeServer(),
                warmupComponent,
                new MissionMultiplayerSiegeClient(),
                new MultiplayerTimerComponent(),
                new SpawnComponent(new SiegeSpawnFrameBehavior(), new CNMSiegeSpawningBehavior()),
                new MissionLobbyEquipmentNetworkComponent(),
                new CNMTeamSelectComponent(warmupComponent, null),
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
                new VoiceChatHandler(),
                new MultiplayerPreloadHelper()
            });
        }
    }
}
