using HarmonyLib;
using NetworkMessages.FromServer;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.DedicatedCustomServer;

namespace Patches
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

    [HarmonyPatch(typeof(MissionCustomGameServerComponent), "AddScoresToStats")] //End_Mission后不统计大厅分数
    internal class Patch_AddScoresToStats
    {
        public static bool Prefix()
        {
            return false;
        }
    }
}
