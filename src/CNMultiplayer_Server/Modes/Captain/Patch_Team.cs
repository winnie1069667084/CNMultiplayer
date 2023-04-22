using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace HarmonyPatches
{
    internal class Patch_Team
    {
        [HarmonyPatch(typeof(Team), "Initialize")]
        internal class Patch_Initialize
        {
            internal static bool Prefix(ref Team __instance, ref List<OrderController> ____orderControllers, ref MBList<Agent> ____activeAgents, ref MBList<Agent> ____teamAgents, ref MBList<ValueTuple<float, WorldPosition, int, Vec2, Vec2, bool>> ____cachedEnemyDataForFleeing)
            {
                ____activeAgents = new MBList<Agent>();
                ____teamAgents = new MBList<Agent>();
                ____cachedEnemyDataForFleeing = new MBList<ValueTuple<float, WorldPosition, int, Vec2, Vec2, bool>>();
                // Only modify fields if we are not in a replay
                if (!GameNetwork.IsReplay)
                {
                    AccessTools.PropertySetter(typeof(Team), "FormationsIncludingSpecialAndEmpty").Invoke(__instance, new object[] { new MBList<Formation>(100) });
                    AccessTools.PropertySetter(typeof(Team), "FormationsIncludingEmpty").Invoke(__instance, new object[] { new MBList<Formation>(100) });
                    for (int i = 0; i < 100; i++)
                    {
                        Formation formation = new Formation(__instance, i);
                        __instance.FormationsIncludingSpecialAndEmpty.Add(formation);

                        if (i < 100)
                        {
                            __instance.FormationsIncludingEmpty.Add(formation);
                        }

                        // Subscribe to active behavior changed event for each formation
                        EventInfo eventInfo = typeof(FormationAI).GetEvent("OnActiveBehaviorChanged", BindingFlags.Instance | BindingFlags.Public);
                        MethodInfo handlerMethod = typeof(Team).GetMethod("FormationAI_OnActiveBehaviorChanged", BindingFlags.Instance | BindingFlags.NonPublic);
                        Delegate handlerDelegate = Delegate.CreateDelegate(eventInfo.EventHandlerType, __instance, handlerMethod);
                        eventInfo.AddEventHandler(formation.AI, handlerDelegate);
                    }
                    if (__instance.Mission != null)
                    {
                        ____orderControllers = new List<OrderController>();
                        OrderController orderController = new OrderController(__instance.Mission, __instance, null);
                        ____orderControllers.Add(orderController);
                        EventInfo eventInfo = typeof(OrderController).GetEvent("OnOrderIssued", BindingFlags.Instance | BindingFlags.Public);
                        MethodInfo handlerMethod = typeof(Team).GetMethod("OrderController_OnOrderIssued", BindingFlags.Instance | BindingFlags.NonPublic);
                        Delegate handlerDelegate = Delegate.CreateDelegate(eventInfo.EventHandlerType, __instance, handlerMethod);
                        eventInfo.AddEventHandler(orderController, handlerDelegate);
                        OrderController orderController2 = new OrderController(__instance.Mission, __instance, null);
                        ____orderControllers.Add(orderController2);
                        eventInfo.AddEventHandler(orderController2, handlerDelegate);
                    }

                    AccessTools.PropertySetter(typeof(Team), "QuerySystem").Invoke(__instance, new object[] { new TeamQuerySystem(__instance) });
                    AccessTools.PropertySetter(typeof(Team), "DetachmentManager").Invoke(__instance, new object[] { new DetachmentManager(__instance) });
                }
                return false;
            }
        }
    }
}
