using System.Reflection;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.DedicatedCustomServer;

namespace Patches
{
    internal class FixBattleIndexBehavior : MissionBehavior
    {
        public override void OnBehaviorInitialize()
        {
            Debug.Print("Preventing map change crash fix behavior initialized!", 0, Debug.DebugColor.Green, 17592186044416UL);
        }

        protected override void OnEndMission()
        {
            Debug.Print("OnEndMission called...", 0, Debug.DebugColor.Green, 17592186044416UL);
            BaseNetworkComponent networkComponent = GameNetwork.GetNetworkComponent<BaseNetworkComponent>();
            bool flag = networkComponent != null;
            if (flag)
            {
                FieldInfo field = typeof(DedicatedCustomServerSubModule).GetField("_currentAutomatedBattleIndex", BindingFlags.Instance | BindingFlags.NonPublic);
                Debug.Print("bnc.CurrentBattleIndex:", networkComponent.CurrentBattleIndex, Debug.DebugColor.Green, 17592186044416UL);
                string str = "_currentAutomatedBattleIndex:";
                object obj = (field != null) ? field.GetValue(DedicatedCustomServerSubModule.Instance) : null;
                Debug.Print(str + ((obj != null) ? obj.ToString() : null), 0, Debug.DebugColor.Green, 17592186044416UL);
                bool flag2 = networkComponent.CurrentBattleIndex > 2;
                if (flag2)
                {
                    Debug.Print("bnc.CurrentBattleIndex > 2! Setting it to 1.", 0, Debug.DebugColor.Green, 17592186044416UL);
                    Debug.Print("bnc._currentAutomatedBattleIndex > 2! Setting it to 1.", 0, Debug.DebugColor.Green, 17592186044416UL);
                    networkComponent.UpdateCurrentBattleIndex(1);
                    if (field != null)
                    {
                        field.SetValue(DedicatedCustomServerSubModule.Instance, 1);
                    }
                }
            }
        }

        public override MissionBehaviorType BehaviorType
        {
            get
            {
                return MissionBehaviorType.Other;
            }
        }
    }
}
