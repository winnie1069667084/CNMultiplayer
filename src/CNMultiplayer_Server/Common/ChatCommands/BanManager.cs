using System;
using System.IO;
using TaleWorlds.Core;

namespace ChatCommands
{
    class BanManager
    {
        public static string BanListPath()
        {
            string basePath = Environment.CurrentDirectory + "/../../Modules/Native";
            string path = Path.Combine(basePath, "banlist.txt");
            return path;
        }

        public static string[] BanList()
        {
            if (!File.Exists(BanManager.BanListPath())) return new string[] { };
            return File.ReadAllLines(BanManager.BanListPath());
        }
        public static void UpdateList(string[] list)
        {
            if (!File.Exists(BanManager.BanListPath()))
            {
                File.Create(BanManager.BanListPath());
            }
            File.WriteAllLines(BanManager.BanListPath(), list);
        }
        public static bool IsPlayerBanned(VirtualPlayer player)
        {

            if (!File.Exists(BanManager.BanListPath())) return false;

            string[] lines = File.ReadAllLines(BanManager.BanListPath());

            foreach (string line in lines)
            {
                if (line.Trim().Equals("")) continue;
                string bannedId = line.Trim().Split('|')[1];
                if (bannedId.Equals(player.Id.ToString()))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
