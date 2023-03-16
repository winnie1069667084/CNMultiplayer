using HarmonyLib;
using NetworkMessages.FromServer;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.DedicatedCustomServer;

namespace Patches
{
    [HarmonyPatch(typeof(MissionCustomGameServerComponent), "HandleLateNewClientAfterSynchronized")]//玩家加入服务器提示
    internal class Patch_HandleLateNewClientAfterSynchronized
    {
        public static void Postfix(ref NetworkCommunicator networkPeer)
        {
            GameNetwork.BeginBroadcastModuleEvent();
            GameNetwork.WriteMessage(new ServerMessage("[服务器]: " + networkPeer.UserName.ToString() + " 加入游戏", false));
            GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None, null);
        }
    }
}
