using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.DedicatedCustomServer;

namespace HarmonyPatches
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

        [HarmonyPatch(typeof(DedicatedCustomServerSubModule), "SelectRandomMap")]//循环地图池中的地图(随机初次启动服务器的地图)
        internal class Patch_SelectRandomMap
        {
            private static bool isRandom = true;
            private static int randomint = int.MaxValue;
            public static bool Prefix(List<string> ____automatedMapPool)
            {
                if (isRandom || randomint < 0)
                {
                    randomint = new Random().Next(1000*____automatedMapPool.Count, int.MaxValue);
                    isRandom = false;
                }
                int cycle = randomint % ____automatedMapPool.Count;
                string value = ____automatedMapPool[cycle];
                MultiplayerOptions.Instance.GetOptionFromOptionType(MultiplayerOptions.OptionType.Map).UpdateValue(value);
                randomint--;
                return false;
            }
        }
    }
}
