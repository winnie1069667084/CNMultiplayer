using NetworkMessages.FromServer;
using System.Linq;
using TaleWorlds.MountAndBlade;

namespace ChatCommands.Commands
{
    class Gold : Command
    {
        public bool CanUse(NetworkCommunicator networkPeer)
        {
            bool isAdmin = false;
            bool isExists = AdminManager.Admins.TryGetValue(networkPeer.VirtualPlayer.Id.ToString(), out isAdmin);
            return isExists && isAdmin;
        }

        public string Command()
        {
            return "!gold";
        }

        public string Description()
        {
            return "Set gold to a player. Usage !gold <Player Name> <amount>";
        }
       
        public bool Execute(NetworkCommunicator networkPeer, string[] args)
        {
            if (args.Length < 2) {
                GameNetwork.BeginModuleEventAsServer(networkPeer);
                GameNetwork.WriteMessage(new ServerMessage("Please provide a username an a amount."));
                GameNetwork.EndModuleEventAsServer();
                return true;
            }

            NetworkCommunicator targetPeer = null;
            foreach (NetworkCommunicator peer in GameNetwork.NetworkPeers) {
                if(peer.UserName.Contains(string.Join(" ", args.Skip(0)
                    .Take(args.Length - 1)
                    .ToArray()  ))) {
                    targetPeer = peer;
                    break;
                }
            }
            if (targetPeer == null) {
                GameNetwork.BeginModuleEventAsServer(networkPeer);
                GameNetwork.WriteMessage(new ServerMessage("Target player not found"));
                GameNetwork.EndModuleEventAsServer();
                return true;
            }
            int goldAmount;
            if(int.TryParse(args[1], out goldAmount))
            {
                MissionPeer mp = targetPeer.GetComponent<MissionPeer>();
                mp.Representative.UpdateGold(goldAmount);
            }
            
            return true;
        }
    }
}
