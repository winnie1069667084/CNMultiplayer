using HarmonyLib;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace HarmonyPatches
{
    internal class Patch_DeploymentPlan
    {
        [HarmonyPatch(typeof(DeploymentPlan), "GetFormationPlan")]
        class Patch_GetFormationPlan
        {
            internal static bool Prefix(ref FormationDeploymentPlan __result, FormationClass fClass, FormationDeploymentPlan[] ____formationPlans)
            {
                __result = ____formationPlans[(int)fClass % (int)FormationClass.NumberOfAllFormations];
                return false;
            }
        }
    }
}
