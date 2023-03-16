using HarmonyLib;
using NetworkMessages.FromServer;
using System.Diagnostics;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;
using Debug = TaleWorlds.Library.Debug;

namespace ServerPatches
{
    [HarmonyPatch(typeof(MissionLobbyComponent), "SendPeerInformationsToPeer")]//ServerPatch @HornsGuy
    class PatchMissionLobbyComponent_SendPeerInformationsToPeer
    {
        static bool hitOnce = false;
        public static bool Prefix(MissionLobbyComponent __instance, NetworkCommunicator peer)
        {
            if (!hitOnce)
            {
                Logging.Instance.Info("PatchMissionLobbyComponent_SendPeerInformationsToPeer.Prefix has been hit once");
                hitOnce = true;
            }

            foreach (NetworkCommunicator disconnectedPeer in GameNetwork.NetworkPeersIncludingDisconnectedPeers)
            {
                if (disconnectedPeer != null)
                {
                    bool flag = disconnectedPeer.VirtualPlayer != MBNetwork.VirtualPlayers[disconnectedPeer.VirtualPlayer.Index];
                    if (flag || disconnectedPeer.IsSynchronized || disconnectedPeer.JustReconnecting)
                    {
                        if (peer != null)
                        {
                            MissionPeer component = disconnectedPeer.GetComponent<MissionPeer>();
                            if (component != null)
                            {
                                GameNetwork.BeginModuleEventAsServer(peer);
                                GameNetwork.WriteMessage((GameNetworkMessage)new KillDeathCountChange(component.GetNetworkPeer(), (NetworkCommunicator)null, component.KillCount, component.AssistCount, component.DeathCount, component.Score));
                                GameNetwork.EndModuleEventAsServer();
                                if (component.BotsUnderControlAlive != 0 || component.BotsUnderControlTotal != 0)
                                {
                                    GameNetwork.BeginModuleEventAsServer(peer);
                                    GameNetwork.WriteMessage((GameNetworkMessage)new BotsControlledChange(component.GetNetworkPeer(), component.BotsUnderControlAlive, component.BotsUnderControlTotal));
                                    GameNetwork.EndModuleEventAsServer();
                                }
                            }
                            else
                            {
                                Logging.Instance.Error("component was null in PatchMissionLobbyComponent_SendPeerInformationsToPeer!");
                            }
                        }
                        else
                        {
                            Logging.Instance.Error("peer was null in PatchMissionLobbyComponent_SendPeerInformationsToPeer!");
                        }
                    }
                    else
                    {
                        Debug.Print(">#< Can't send the info of " + disconnectedPeer.UserName + " to " + peer.UserName + ".", color: Debug.DebugColor.BrightWhite, debugFilter: 17179869184UL);
                        Debug.Print(string.Format("isDisconnectedPeer: {0}", (object)flag), color: Debug.DebugColor.BrightWhite, debugFilter: 17179869184UL);
                        Debug.Print(string.Format("networkPeer.IsSynchronized: {0}", (object)disconnectedPeer.IsSynchronized), color: Debug.DebugColor.BrightWhite, debugFilter: 17179869184UL);
                        Debug.Print(string.Format("peer == networkPeer: {0}", (object)(peer == disconnectedPeer)), color: Debug.DebugColor.BrightWhite, debugFilter: 17179869184UL);
                        Debug.Print(string.Format("networkPeer.JustReconnecting: {0}", (object)disconnectedPeer.JustReconnecting), color: Debug.DebugColor.BrightWhite, debugFilter: 17179869184UL);
                    }
                }
                else
                {
                    Logging.Instance.Error("disconnectedPeer was null in PatchMissionLobbyComponent_SendPeerInformationsToPeer!");
                }
            }
            return false;
        }

    }

}
