using System.IO;
using System.Xml;
using TaleWorlds.ModuleManager;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;

namespace CNMultiplayer.Common.XML
{
    //这个类是为了兼容原版而写，用于根据不同的模式加载不同的mpclassdivision
    public class LoadXMLbyMode
    {
        public void ModeJudgment()
        {
            string gametype = MultiplayerOptions.OptionType.GameType.GetStrValue(MultiplayerOptions.MultiplayerOptionsAccessMode.CurrentMapOptions);
            switch (gametype)
            {
                case "CNMSiege":
                    LoadXMLs("CNMultiplayer", "CNMSiege");
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
            "mpclassdivisions.xml",
        };
    }
}