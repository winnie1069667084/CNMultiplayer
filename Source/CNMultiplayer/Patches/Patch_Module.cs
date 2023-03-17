using HarmonyLib;
using TaleWorlds.ObjectSystem;

namespace Patches
{
    [HarmonyPatch(typeof(TaleWorlds.MountAndBlade.Module), "LoadSubModules")]//优先读取CNM的xml文件
    internal class Patch_LoadSubModules
    {
        public static bool Prefix()
        {
            XmlResource.GetXmlListAndApply("CNMultiplayer");
            return true;
        }
    }
}