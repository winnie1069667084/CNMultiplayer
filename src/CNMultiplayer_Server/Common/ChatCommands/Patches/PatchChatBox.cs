using HarmonyLib;
using TaleWorlds.MountAndBlade;

namespace ChatCommands.Patches
{
    [HarmonyPatch(typeof(ChatBox), "ServerPrepareAndSendMessage")]//ChatCommands @mentalrob
    class PatchChatBox
    {
        public static bool Prefix(ChatBox __instance, NetworkCommunicator fromPeer, bool toTeamOnly, string message)
        {
            return !message.StartsWith("!");
        }
    }
}
