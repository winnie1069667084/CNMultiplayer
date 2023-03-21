using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View.MissionViews;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.MissionViews.Multiplayer;

namespace Patches
{
    //[HarmonyPatch(typeof(MultiplayerMissionViews), "OpenSiegeMission")]//为攻城模式添加语音UI
    internal class Patch_MultiplayerMissionViews
    {
        public static bool Prefix(Mission mission, ref MissionView[] __result)
        {
            List<MissionView> list = new List<MissionView>();
            list.Add(ViewCreator.CreateMissionServerStatusUIHandler());
            list.Add(ViewCreator.CreateMissionMultiplayerPreloadView(mission));
            list.Add(ViewCreator.CreateMissionKillNotificationUIHandler());
            list.Add(ViewCreator.CreateMissionAgentStatusUIHandler(mission));
            list.Add(ViewCreator.CreateMissionMainAgentEquipmentController(mission));
            list.Add(ViewCreator.CreateMissionMainAgentCheerBarkControllerView(mission));
            list.Add(ViewCreator.CreateMissionMultiplayerEscapeMenu("Siege"));
            list.Add(ViewCreator.CreateMultiplayerEndOfBattleUIHandler());
            list.Add(ViewCreator.CreateMissionAgentLabelUIHandler(mission));
            list.Add(ViewCreator.CreateMultiplayerTeamSelectUIHandler());
            list.Add(ViewCreator.CreateMissionScoreBoardUIHandler(mission, false));
            list.Add(ViewCreator.CreateMultiplayerEndOfRoundUIHandler());
            list.Add(ViewCreator.CreateLobbyEquipmentUIHandler());
            list.Add(ViewCreator.CreatePollProgressUIHandler());
            list.Add(ViewCreator.CreateMultiplayerMissionHUDExtensionUIHandler());
            list.Add(ViewCreator.CreateMultiplayerMissionDeathCardUIHandler(null));
            list.Add(ViewCreator.CreateMultiplayerMissionVoiceChatUIHandler());
            list.Add(new MissionItemContourControllerView());
            list.Add(new MissionAgentContourControllerView());
            list.Add(ViewCreator.CreateMissionFlagMarkerUIHandler());
            list.Add(ViewCreator.CreateOptionsUIHandler());
            list.Add(ViewCreator.CreateMissionMainAgentEquipDropView(mission));
            if (!GameNetwork.IsClient)
            {
                list.Add(ViewCreator.CreateMultiplayerAdminPanelUIHandler());
            }
            list.Add(ViewCreator.CreateMissionBoundaryCrossingView());
            list.Add(new MissionBoundaryWallView());
            __result = list.ToArray();
            return false;
        }
    }
}
