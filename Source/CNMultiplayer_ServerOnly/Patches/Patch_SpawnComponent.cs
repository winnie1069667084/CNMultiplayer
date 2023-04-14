using HarmonyLib;
using TaleWorlds.MountAndBlade;
using CNMultiplayer;

namespace Patches
{
    [HarmonyPatch(typeof(SpawnComponent), "SetSiegeSpawningBehavior")]//调用CNM_SiegeSpawningBehavior
    internal class Patch_SetSiegeSpawningBehavior
    {
        public static bool Prefix()
        {
            Mission.Current.GetMissionBehavior<SpawnComponent>().SetNewSpawnFrameBehavior(new SiegeSpawnFrameBehavior());
            Mission.Current.GetMissionBehavior<SpawnComponent>().SetNewSpawningBehavior(new CNM_SiegeSpawningBehavior());
            return false;
        }
    }
}
