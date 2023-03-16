using NetworkMessages.FromServer;
using System;
using System.Collections.Generic;
using System.Text;
using TaleWorlds.MountAndBlade;

namespace ChatCommands.Commands
{
    public class Me : Command
    {
        public string Command()
        {
            return "!me";
        }
        public bool CanUse(NetworkCommunicator networkPeer)
        {
            bool isAdmin = false;
            bool isExists = AdminManager.Admins.TryGetValue(networkPeer.VirtualPlayer.Id.ToString(), out isAdmin);
            return isExists && isAdmin;
        }



        public bool Execute(NetworkCommunicator networkPeer, string[] args)
        {
            GameNetwork.BeginBroadcastModuleEvent();
            GameNetwork.WriteMessage(new ServerMessage("[管理员]: " + string.Join(" ", args), false));
            GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None, null);
            return true;
        }

        public string Description()
        {
            return "The me command that everyone knows, Usage !me <Emote>";
        }
    }
}
