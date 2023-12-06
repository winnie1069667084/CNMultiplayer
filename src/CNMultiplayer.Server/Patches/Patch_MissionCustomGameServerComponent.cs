using HarmonyLib;
using NetworkMessages.FromServer;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.DedicatedCustomServer;

namespace CNMultiplayer.Server.Patches
{
    [HarmonyPatch(typeof(MissionCustomGameServerComponent), "HandleLateNewClientAfterSynchronized")] //玩家加入服务器提示
    internal class Patch_HandleLateNewClientAfterSynchronized
    {
        public static void Postfix(NetworkCommunicator networkPeer)
        {
            GameNetwork.BeginBroadcastModuleEvent();
            GameNetwork.WriteMessage(new ServerMessage("[服务器]: " + networkPeer.UserName.ToString() + " 加入游戏", false, true));
            GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None, null);
        }
    }
}
