using CNMultiplayer.Server.Modes.CNMSiege;
using HarmonyLib;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;
using CNMultiplayer.Common.XML;

namespace CNMultiplayer.Server
{
    public class CNMSubModule : MBSubModuleBase
    {
        protected override void OnSubModuleLoad()
        {
            base.OnSubModuleLoad();
            Harmony harmony = new Harmony("CNMultiplayer");
            harmony.PatchAll();
            Module.CurrentModule.AddMultiplayerGameMode(new CNMSiegeGameMode());
        }

        public override void OnBeforeMissionBehaviorInitialize(Mission mission)
        {
            // 根据不同的模式切换mpclassdivision，用于实现对原版的兼容
            MBObjectManager.Instance.ClearAllObjectsWithType(typeof(MultiplayerClassDivisions.MPHeroClass));
            new LoadXMLbyMode().ModeJudgment();
        }
    }
}