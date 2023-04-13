using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.MountAndBlade.Objects.Siege;
using TaleWorlds.MountAndBlade;
using CNMultiplayer;

namespace Patches
{
    [HarmonyPatch(typeof(SpawnComponent), "SetSiegeSpawningBehavior")]//修改攻城车血量
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
