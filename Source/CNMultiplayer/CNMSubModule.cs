using HarmonyLib;
using TaleWorlds.Core;
using TaleWorlds.ModuleManager;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Diamond.MultiplayerBadges;

namespace CNMultiplayer
{
    public class CNMSubModule : MBSubModuleBase
    {
        protected override void OnSubModuleLoad()
        {
            base.OnSubModuleLoad();
            Harmony harmony = new Harmony("CNMultiplayer");
            harmony.PatchAll();
            //待学习调整：*本地化路径修改、*地图投票界面、WelcomeMessage、领军超8人炸服
        }
    }
}