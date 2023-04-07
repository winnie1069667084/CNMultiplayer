using System.Collections.Generic;
using System;
using System.IO;
using System.Xml;
using TaleWorlds.ModuleManager;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;
using TaleWorlds.Library;
using TaleWorlds.Core;

namespace CNMultiplayer
{
    internal class LoadXMLbyMode
    {
        public void ModeJudgment()
        {
            var List = new List<string>() { "MPCharacters", "Items", "MPClassDivisions", "Monsters" };
            bool isDevelopment = Game.Current.GameType.IsDevelopment;
            string gameType = Game.Current.GameType.GetType().Name;
            string gametype = MultiplayerOptions.OptionType.GameType.GetStrValue(MultiplayerOptions.MultiplayerOptionsAccessMode.CurrentMapOptions);
            switch (gametype)
            {
                case "Siege":
                    foreach (string id in List)
                    {
                        XmlDocument mergedXmlForManaged = LoadXMLbyMode.GetMergedXmlForManaged(id, false, gametype, isDevelopment, gameType);
                        MBObjectManager.Instance.LoadXml(mergedXmlForManaged, isDevelopment);
                    }
                    break;
                case "Captain":
                    foreach (string id in List)
                    {
                        XmlDocument mergedXmlForManaged = LoadXMLbyMode.GetMergedXmlForManaged(id, false, gametype, isDevelopment, gameType);
                        MBObjectManager.Instance.LoadXml(mergedXmlForManaged, isDevelopment);
                    }
                    break;
                default:
                    foreach (string id in List)
                    {
                        XmlDocument mergedXmlForManaged = LoadXMLbyMode.GetMergedXmlForManaged(id, false, "", isDevelopment, gameType);
                        MBObjectManager.Instance.LoadXml(mergedXmlForManaged, isDevelopment);
                    }
                    break;
            }
        }

        public static XmlDocument GetMergedXmlForManaged(string id, bool skipValidation, string modename, bool ignoreGameTypeInclusionCheck = true, string gameType = "")
        {
            List<Tuple<string, string>> list = new List<Tuple<string, string>>();
            List<string> list2 = new List<string>();
            foreach (MbObjectXmlInformation mbObjectXmlInformation in XmlResource.XmlInformationList)
            {
                if (mbObjectXmlInformation.Id == id && (ignoreGameTypeInclusionCheck || mbObjectXmlInformation.GameTypesIncluded.Count == 0 || mbObjectXmlInformation.GameTypesIncluded.Contains(gameType)))
                {
                    string xsdPath = ModuleHelper.GetXsdPath(mbObjectXmlInformation.Id);
                    string text = ModuleHelper.GetXmlPath(mbObjectXmlInformation.ModuleName, mbObjectXmlInformation.Name);
                    if (mbObjectXmlInformation.ModuleName == "CNMultiplayer")
                    {
                        text = GetXmlPath(mbObjectXmlInformation.ModuleName, mbObjectXmlInformation.Name, modename);
                    }
                    if (File.Exists(text))
                    {
                        if (mbObjectXmlInformation.ModuleName == "CNMultiplayer")
                        {
                            list.Add(Tuple.Create<string, string>(GetXmlPath(mbObjectXmlInformation.ModuleName, mbObjectXmlInformation.Name, modename), xsdPath));
                            HandleXsltList(GetXsltPath(mbObjectXmlInformation.ModuleName, mbObjectXmlInformation.Name, modename), ref list2);
                        }
                        else
                        {
                            list.Add(Tuple.Create<string, string>(ModuleHelper.GetXmlPath(mbObjectXmlInformation.ModuleName, mbObjectXmlInformation.Name), xsdPath));
                            HandleXsltList(ModuleHelper.GetXsltPath(mbObjectXmlInformation.ModuleName, mbObjectXmlInformation.Name), ref list2);
                        }
                    }
                    else
                    {
                        string text2 = text.Replace(".xml", "");
                        if (Directory.Exists(text2))
                        {
                            foreach (FileInfo fileInfo in new DirectoryInfo(text2).GetFiles("*.xml"))
                            {
                                text = text2 + "/" + fileInfo.Name;
                                list.Add(Tuple.Create<string, string>(text, xsdPath));
                                HandleXsltList(text.Replace(".xml", ".xsl"), ref list2);
                            }
                        }
                        else
                        {
                            list.Add(Tuple.Create<string, string>("", ""));
                            if (!HandleXsltList(ModuleHelper.GetXsltPath(mbObjectXmlInformation.ModuleName, mbObjectXmlInformation.Name), ref list2))
                            {
                                Debug.ShowError(string.Concat(new string[]
                                {
                                    "Unable to find xml or xslt file for the entry '",
                                    ModuleHelper.GetModuleFullPath(mbObjectXmlInformation.ModuleName),
                                    "ModuleData/",
                                    mbObjectXmlInformation.Name,
                                    "' in SubModule.xml."
                                }));
                            }
                        }
                    }
                }
            }
            return MBObjectManager.CreateMergedXmlFile(list, list2, skipValidation);
        }

        private static bool HandleXsltList(string xslPath, ref List<string> xsltList)
        {
            string text = xslPath + "t";
            if (File.Exists(xslPath))
            {
                xsltList.Add(xslPath);
                return true;
            }
            if (File.Exists(text))
            {
                xsltList.Add(text);
                return true;
            }
            xsltList.Add("");
            return false;
        }

        public static string GetXmlPath(string moduleId, string xmlName, string modename)
        {
            return ModuleHelper.GetModuleFullPath(moduleId) + modename + "ModuleData/" + xmlName + ".xml";
        }

        public static string GetXsltPath(string moduleId, string xmlName, string modename)
        {
            return ModuleHelper.GetModuleFullPath(moduleId) + modename + "ModuleData/" + xmlName + ".xsl";
        }
    }
}