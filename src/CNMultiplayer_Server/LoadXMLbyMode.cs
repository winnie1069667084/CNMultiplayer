﻿using System.IO;
using System.Linq;
using System.Xml;
using TaleWorlds.Engine;
using TaleWorlds.ModuleManager;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;

namespace CNMultiplayer
{
    internal class LoadXMLbyMode
    {

        private static readonly string modulename = Utilities.GetModulesNames().Where(name => name.Contains("CNMultiplayer")).ToArray()[0];

        public void ModeJudgment()
        {
            string gametype = MultiplayerOptions.OptionType.GameType.GetStrValue(MultiplayerOptions.MultiplayerOptionsAccessMode.CurrentMapOptions);
            switch (gametype)
            {
                case "CNMSiege":
                    LoadXMLs(modulename, gametype);
                    break;
                case "CNMCaptain":
                    LoadXMLs(modulename, gametype);
                    break;
                case "IndividualDeathMatch":
                    LoadXMLs(modulename, gametype);
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

        readonly string[] filelist = new string[]
        {
            "mpclassdivisions.xml",
        };
    }
}