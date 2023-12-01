using CNMultiplayer.Common;
using CNMultiplayer.Modes.Captain;
using CNMultiplayer.Modes.IndividualDeathMatch;
using CNMultiplayer.Modes.Siege;
using HarmonyLib;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;

namespace CNMultiplayer
{
    public class CNMSubModule : MBSubModuleBase
    {
        protected override void OnSubModuleLoad()
        {
            base.OnSubModuleLoad();
            Harmony harmony = new Harmony("CNMultiplayer");
            harmony.PatchAll();
            Module.CurrentModule.AddMultiplayerGameMode(new CNMSiegeGameMode());
            Module.CurrentModule.AddMultiplayerGameMode(new CNMCaptainGameMode());
            Module.CurrentModule.AddMultiplayerGameMode(new IDMGameMode());
        }

        public override void OnBeforeMissionBehaviorInitialize(Mission mission)
        {
            mission.AddMissionBehavior(new NotAllPlayersJoinFixBehavior());
            MBObjectManager.Instance.ClearAllObjectsWithType(typeof(MultiplayerClassDivisions.MPHeroClass)); //目前只实现了根据游戏模式加载MPClassDivisions
            new LoadXMLbyMode().ModeJudgment();
        }
    }
}