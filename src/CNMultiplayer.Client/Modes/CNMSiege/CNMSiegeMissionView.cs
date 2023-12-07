using TaleWorlds.MountAndBlade.Multiplayer.View.MissionViews;
using TaleWorlds.MountAndBlade.View.MissionViews;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade;

namespace CNMultiplayer.Client.Modes.CNMSiege
{
    [ViewCreatorModule]
    internal class CNMSiegeMissionView
    {
        [ViewMethod("CNMSiege")]
        public static MissionView[] OpenCNMSiege(Mission mission)
        {
            return new[]
            {
                MultiplayerViewCreator.CreateMissionServerStatusUIHandler(),
                MultiplayerViewCreator.CreateMissionMultiplayerPreloadView(mission),
                MultiplayerViewCreator.CreateMissionKillNotificationUIHandler(),
                ViewCreator.CreateMissionAgentStatusUIHandler(mission),
                ViewCreator.CreateMissionMainAgentEquipmentController(mission),
                ViewCreator.CreateMissionMainAgentCheerBarkControllerView(mission),
                MultiplayerViewCreator.CreateMissionMultiplayerEscapeMenu("Siege"),
                MultiplayerViewCreator.CreateMultiplayerEndOfBattleUIHandler(),
                ViewCreator.CreateMissionAgentLabelUIHandler(mission),
                MultiplayerViewCreator.CreateMultiplayerTeamSelectUIHandler(),
                MultiplayerViewCreator.CreateMissionScoreBoardUIHandler(mission, false),
                MultiplayerViewCreator.CreateMultiplayerEndOfRoundUIHandler(),
                MultiplayerViewCreator.CreateLobbyEquipmentUIHandler(),
                MultiplayerViewCreator.CreatePollProgressUIHandler(),
                MultiplayerViewCreator.CreateMultiplayerMissionHUDExtensionUIHandler(),
                MultiplayerViewCreator.CreateMultiplayerMissionDeathCardUIHandler(null),
                new MissionItemContourControllerView(),
                new MissionAgentContourControllerView(),
                MultiplayerViewCreator.CreateMissionFlagMarkerUIHandler(),
                ViewCreator.CreateOptionsUIHandler(),
                ViewCreator.CreateMissionMainAgentEquipDropView(mission),
                MultiplayerViewCreator.CreateMultiplayerAdminPanelUIHandler(), //管理UI
                ViewCreator.CreateMissionBoundaryCrossingView(),
                new MissionBoundaryWallView(),
                MultiplayerViewCreator.CreateMultiplayerMissionVoiceChatUIHandler(), //语音UI
            };
        }
    }
}
