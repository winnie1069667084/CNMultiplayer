using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Objects.Siege;

namespace Patches
{
    [HarmonyPatch(typeof(MultiplayerBatteringRamSpawner), "AssignParameters")]//修改攻城车血量
    internal class Patch_RamAssignParameters
    {
        public static void Postfix(SpawnerEntityMissionHelper _spawnerMissionHelper)
        {
            _spawnerMissionHelper.SpawnedEntity.GetFirstScriptOfType<DestructableComponent>().MaxHitPoint = 20000f;
        }
    }

    [HarmonyPatch(typeof(MultiplayerSiegeTowerSpawner), "AssignParameters")]//修改攻城塔血量
    internal class Patch_TowerAssignParameters
    {
        public static void Postfix(SpawnerEntityMissionHelper _spawnerMissionHelper)
        {
            _spawnerMissionHelper.SpawnedEntity.GetFirstScriptOfType<DestructableComponent>().MaxHitPoint = 20000f;
        }
    }
}
