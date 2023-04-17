using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace Patches
{
    [HarmonyPatch(typeof(MissionPeer), nameof(MissionPeer.SelectedPerks), MethodType.Getter)]//使用Transpiler修复第三perk @星辰 - 陈小一
    class PatchFoo
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            bool willPatch = true;
            bool isLastStepPush3 = false;
            foreach (var instruction in instructions)
            {
                if (willPatch)
                {
                    if (instruction.opcode == OpCodes.Ldc_I4_3)
                    {
                        isLastStepPush3 = true;
                    }
                    else if (isLastStepPush3)
                    {
                        isLastStepPush3 = false;

                        if (instruction.opcode == OpCodes.Bge)
                        {
                            willPatch = false;
                            yield return new CodeInstruction(OpCodes.Bgt, instruction.operand);
                            continue;
                        }
                    }
                }
                yield return instruction;
            }
        }
    }

    //[HarmonyPatch(typeof(MissionPeer), "SelectedPerks", MethodType.Getter)]//使用Prefix修复第三perk @Winnie
    class SelectedPerksPatch
    {
        public static bool Prefix(MissionPeer __instance, ref IReadOnlyList<MPPerkObject> __result, ref List<int[]> ____perks, ValueTuple<int, List<MPPerkObject>> ____selectedPerks)
        {
            if (__instance.SelectedTroopIndex < 0 || __instance.Team == null || __instance.Team.Side == BattleSideEnum.None)
            {
                __result = new List<MPPerkObject>();
            }
            if (____selectedPerks.Item2 == null || __instance.SelectedTroopIndex != ____selectedPerks.Item1 || ____selectedPerks.Item2.Count <= 3)
            {
                List<MPPerkObject> list = new List<MPPerkObject>();
                List<List<IReadOnlyPerkObject>> availablePerksForPeer = MultiplayerClassDivisions.GetAvailablePerksForPeer(__instance);
                for (int i = 0; i < 3; i++)
                {
                    int num = ____perks[__instance.SelectedTroopIndex][i];
                    if (availablePerksForPeer[i].Count > 0)
                    {
                        list.Add(availablePerksForPeer[i][(num >= 0 && num < availablePerksForPeer[i].Count) ? num : 0].Clone(__instance));
                    }
                }
                ____selectedPerks = new ValueTuple<int, List<MPPerkObject>>(__instance.SelectedTroopIndex, list);
            }
            __result = ____selectedPerks.Item2;
            return false;
        }
    }
}