using System.Reflection;
using TaleWorlds.MountAndBlade;

namespace ChatCommands.Patches
{
    class PatchMaxNumberOfPlayers
    {
        public static MultiplayerOptionsProperty Postfix(MultiplayerOptionsProperty returnedValue)
        {
            if (returnedValue.Description.Equals("Set the maximum amount of player allowed on the server."))
            {
                var field = typeof(MultiplayerOptionsProperty).GetField("BoundsMax", BindingFlags.Public | BindingFlags.Instance);
                field.SetValue(returnedValue, 500);
            }
            return returnedValue;
        }
    }
}
