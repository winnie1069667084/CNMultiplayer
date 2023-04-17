using System.Collections.Generic;

namespace ChatCommands
{
    public class AdminManager
    {
        public static Dictionary<string, bool> Admins = new Dictionary<string, bool>();

        public static bool PlayerIsAdmin(string peerId)
        {
            if (ConfigManager.GetConfig().Admins != null)
            {
                foreach (var adminInfo in ConfigManager.GetConfig().Admins)
                {
                    string currentId = adminInfo.Split('|')[1];
                    if (peerId == currentId)
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
