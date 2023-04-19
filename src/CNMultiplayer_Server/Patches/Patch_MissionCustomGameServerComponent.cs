using HarmonyLib;
using NetworkMessages.FromServer;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.DedicatedCustomServer;

namespace HarmonyPatches
{
    [HarmonyPatch(typeof(MissionCustomGameServerComponent), "HandleLateNewClientAfterSynchronized")] //玩家加入服务器提示
    internal class Patch_HandleLateNewClientAfterSynchronized
    {
        public static void Postfix(NetworkCommunicator networkPeer)
        {
            GameNetwork.BeginBroadcastModuleEvent();
            GameNetwork.WriteMessage(new ServerMessage("[服务器]: " + networkPeer.UserName.ToString() + " 加入游戏", false));
            GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None, null);
        }
    }

    [HarmonyPatch(typeof(MissionCustomGameServerComponent), "OnEndMission")] //End_Mission不与大厅沟通玩家分数
    internal class Patch_OnEndMission
    {
        public static bool Prefix()
        {
            return false;
        }
    }

    [HarmonyPatch(typeof(MissionCustomGameServerComponent), "OnDuelEnded")] //决斗结束后不与大厅沟通玩家分数
    internal class Patch_OnDuelEnded
    {
        public static bool Prefix()
        {
            return false;
        }
    }
}
