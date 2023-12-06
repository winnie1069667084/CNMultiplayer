using CNMultiplayer.Client.Modes.Siege;
using HarmonyLib;
using TaleWorlds.MountAndBlade;

namespace CNMultiplayer.Client
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
    }
}