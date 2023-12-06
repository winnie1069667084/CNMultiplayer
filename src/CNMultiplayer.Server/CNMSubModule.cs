using CNMultiplayer.Server.Modes.CNMSiege;
using CNMultiplayer.Server.Patches.Behaviors;
using HarmonyLib;
using TaleWorlds.MountAndBlade;

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
            // 修复“有玩家未准备好”bug，by mentalrob
            mission.AddMissionBehavior(new NotAllPlayersJoinFixBehavior());
        }
    }
}