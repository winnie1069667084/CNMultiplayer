using HarmonyLib;
using NetworkMessages.FromServer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Source.Missions;

namespace Patches
{
    [HarmonyPatch(typeof(MultiplayerMissions), "OpenSkirmishMission")]//移除遭遇战语音控件
    internal class Patch_OpenSkirmishMission
    {
        public static bool Prefix(string scene)
        {
            MissionState.OpenNew("MultiplayerSkirmish", new MissionInitializerRecord(scene), delegate (Mission missionController)
            {
                if (GameNetwork.IsServer)
                {
                    return new MissionBehavior[]
                    {
                        MissionLobbyComponent.CreateBehavior(),
                        new MissionMultiplayerFlagDomination(MissionLobbyComponent.MultiplayerGameType.Skirmish),
                        new MultiplayerRoundController(),
                        new MultiplayerWarmupComponent(),
                        new MissionMultiplayerGameModeFlagDominationClient(),
                        new MultiplayerTimerComponent(),
                        new MultiplayerMissionAgentVisualSpawnComponent(),
                        new ConsoleMatchStartEndHandler(),
                        new SpawnComponent(new FlagDominationSpawnFrameBehavior(), new FlagDominationSpawningBehavior()),
                        new MissionLobbyEquipmentNetworkComponent(),
                        new MultiplayerTeamSelectComponent(),
                        new MissionHardBorderPlacer(),
                        new MissionBoundaryPlacer(),
                        new AgentVictoryLogic(),
                        new MissionAgentPanicHandler(),
                        new AgentHumanAILogic(),
                        new MissionBoundaryCrossingHandler(),
                        new MultiplayerPollComponent(),
                        new MultiplayerAdminComponent(),
                        new MultiplayerGameNotificationsComponent(),
                        new MissionOptionsComponent(),
                        new MissionScoreboardComponent(new SkirmishScoreboardData()),
                        new EquipmentControllerLeaveLogic(),
                        new MultiplayerPreloadHelper()
                    };
                }
                return new MissionBehavior[]
                {
                    MissionLobbyComponent.CreateBehavior(),
                    new MultiplayerAchievementComponent(),
                    new MultiplayerWarmupComponent(),
                    new MissionMultiplayerGameModeFlagDominationClient(),
                    new MultiplayerRoundComponent(),
                    new MultiplayerTimerComponent(),
                    new MultiplayerMissionAgentVisualSpawnComponent(),
                    new ConsoleMatchStartEndHandler(),
                    new MissionLobbyEquipmentNetworkComponent(),
                    new MultiplayerTeamSelectComponent(),
                    new MissionHardBorderPlacer(),
                    new MissionBoundaryPlacer(),
                    new AgentVictoryLogic(),
                    new MissionBoundaryCrossingHandler(),
                    new MultiplayerPollComponent(),
                    new MultiplayerGameNotificationsComponent(),
                    new MissionOptionsComponent(),
                    new MissionScoreboardComponent(new SkirmishScoreboardData()),
                    new MissionMatchHistoryComponent(),
                    new EquipmentControllerLeaveLogic(),
                    new MissionRecentPlayersComponent(),
                    new MultiplayerPreloadHelper()
                };
            }, true, true);
            return false;
        }
    }

    //[HarmonyPatch(typeof(MultiplayerMissions), "OpenSiegeMission")]//为攻城模式添加语音控件
    internal class Patch_OpenSiegeMission
    {
        public static bool Prefix(string scene)
        {
            MissionState.OpenNew("MultiplayerSiege", new MissionInitializerRecord(scene)
            {
                SceneUpgradeLevel = 3,
                SceneLevels = ""
            }, delegate (Mission missionController)
            {
                if (GameNetwork.IsServer)
                {
                    return new MissionBehavior[]
                    {
                        MissionLobbyComponent.CreateBehavior(),
                        new MissionMultiplayerSiege(),
                        new MultiplayerWarmupComponent(),
                        new MissionMultiplayerSiegeClient(),
                        new MultiplayerTimerComponent(),
                        new MultiplayerMissionAgentVisualSpawnComponent(),
                        new ConsoleMatchStartEndHandler(),
                        new SpawnComponent(new SiegeSpawnFrameBehavior(), new SiegeSpawningBehavior()),
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
                        new VoiceChatHandler(),
                        new MultiplayerPreloadHelper()
                    };
                }
                return new MissionBehavior[]
                {
                    MissionLobbyComponent.CreateBehavior(),
                    new MultiplayerWarmupComponent(),
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
                    new VoiceChatHandler(),
                    new MultiplayerPreloadHelper()
                };
            }, true, true);
            return false;
        }
    }
}
