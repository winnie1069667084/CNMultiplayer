using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.DedicatedCustomServer;

namespace Patches
{
    [HarmonyPatch(typeof(DedicatedCustomServerSubModule), "TickAutomatedBattles")]//config中设置“disable_map_voting" 和 “disable_culture_voting”时启用随机阵营或地图
    internal class Patch_TickAutomatedBattles
    {
        public static bool Prefix(DedicatedCustomServerSubModule __instance, bool ____automatedBattleSwitchingEnabled, AutomatedBattleState ____automatedBattleState, ref float ____currentAutomatedBattleRemainingTime)
        {
            MethodInfo SelectRandomMap = AccessTools.Method(typeof(DedicatedCustomServerSubModule), "SelectRandomMap");
            MethodInfo SyncOptionsToClients = AccessTools.Method(typeof(DedicatedCustomServerSubModule), "SyncOptionsToClients");
            MethodInfo SelectRandomCultures = AccessTools.Method(typeof(DedicatedCustomServerSubModule), "SelectRandomCultures");
            if (____automatedBattleSwitchingEnabled)
            {
                switch (____automatedBattleState)
                {
                    case AutomatedBattleState.CountingForNextBattle:
                        if (____currentAutomatedBattleRemainingTime > 1f)
                        {
                            if (!MultiplayerIntermissionVotingManager.Instance.IsMapVoteEnabled)
                            {
                                SelectRandomMap.Invoke(__instance, new object[0]);
                            }
                            if (!MultiplayerIntermissionVotingManager.Instance.IsCultureVoteEnabled)
                            {
                                SelectRandomCultures.Invoke(__instance, new object[0]);
                            }
                            ____currentAutomatedBattleRemainingTime = 0.1f;
                            SyncOptionsToClients.Invoke(__instance, new object[0]);
                            __instance.StartMission();
                            return true;
                        }
                        break;
                }
            }
            return true;
        }

        [HarmonyPatch(typeof(DedicatedCustomServerSubModule), "SelectRandomMap")]//避免下次随机地图与当前地图重复
        internal class Patch_SelectRandomMap
        {
            public static bool Prefix(List<string> ____automatedMapPool)
            {
                var flag = true;
                while (flag)
                {
                    Random random = new Random();
                    string value = ____automatedMapPool[random.Next(0, ____automatedMapPool.Count)];
                    if (value != MultiplayerOptions.OptionType.Map.GetValueText((MultiplayerOptions.MultiplayerOptionsAccessMode.CurrentMapOptions)) || ____automatedMapPool.Count == 1)
                    {
                        MultiplayerOptions.Instance.GetOptionFromOptionType(MultiplayerOptions.OptionType.Map).UpdateValue(value);
                        flag = false;
                    }
                }
                return false;
            }
        }
    }
}
