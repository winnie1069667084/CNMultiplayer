using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Source.Missions;
using TaleWorlds.MountAndBlade.Multiplayer;
using CNMultiplayer.Common.Modes.Warmup;
using CNMultiplayer.Common.Modes.CNMSiege;

namespace CNMultiplayer.Server.Modes.CNMSiege
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

            MissionState.OpenNew(GameName, new MissionInitializerRecord(scene),
            _ => new MissionBehavior[]
            {
                MissionLobbyComponent.CreateBehavior(),
                new CNMSiegeServer(),
                warmupComponent,
                new CNMSiegeClient(),
                new MultiplayerTimerComponent(),
                new SpawnComponent(new SiegeSpawnFrameBehavior(), new CNMSiegeSpawningBehavior()),
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
                new MissionAgentPanicHandler(),
                new AgentHumanAILogic(),
                new EquipmentControllerLeaveLogic(),
                new VoiceChatHandler(),
                new MultiplayerPreloadHelper()
            });
        }
    }
}
