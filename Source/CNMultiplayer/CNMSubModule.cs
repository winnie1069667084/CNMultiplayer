using HarmonyLib;
using TaleWorlds.MountAndBlade;

namespace CNMultiplayer
{
    public class CNMSubModule : MBSubModuleBase
    {
        protected override void OnSubModuleLoad()
        {
            base.OnSubModuleLoad();
            Harmony harmony = new Harmony("CNMultiplayer");
            harmony.PatchAll();
            //待学习调整：*地图投票界面、WelcomeMessage、*不同模式加载不同XML
        }
    }
}