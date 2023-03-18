using System.IO;
using System.Xml;
using TaleWorlds.ModuleManager;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;

namespace CNMultiplayer
{
    internal class LoadXMLbyMode
    {
        public void ModeJudgment()
        {
            string gametype = MultiplayerOptions.OptionType.GameType.GetStrValue(MultiplayerOptions.MultiplayerOptionsAccessMode.CurrentMapOptions);
            switch (gametype)
            {
                case "Siege":
                    LoadXMLs("CNMultiplayer", "Siege");
                    break;
                case "Captain":
                    LoadXMLs("CNMultiplayer", "Captain");
                    break;
                default:
                    LoadXMLs("Native", "");
                    break;
            }
        }

        public void LoadXMLs(string modulename, string modename)
        {
            string path = ModuleHelper.GetModuleFullPath(modulename) + modename + "ModuleData/";
            DirectoryInfo d = new DirectoryInfo(path);
            foreach (string filename in filelist)
            {
                FileInfo[] files = d.GetFiles(filename);
                foreach (FileInfo file in files)
                {
                    XmlDocument xmlDocument = new XmlDocument();
                    xmlDocument.Load(file.FullName);
                    MBObjectManager.Instance.LoadXml(xmlDocument);
                }
            }
        }

        string[] filelist = new string[]
        {
            "monster.xml",
            "mp_crafting_pieces.xml",
            "mpcharacters.xml",
            "mpclassdivisions.xml",
            "mpitems.xml"
        };
    }
}
