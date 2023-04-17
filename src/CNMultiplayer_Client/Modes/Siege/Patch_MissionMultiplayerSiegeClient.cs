#if CLIENT
using CNMultiplayer_Client.Modes.Siege;
using HarmonyLib;
using NetworkMessages.FromServer;
using System;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.MissionRepresentatives;

namespace HarmonyPatches
{
    internal class Patch_MissionMultiplayerSiegeClient
    {
        [HarmonyPatch(typeof(MissionMultiplayerSiegeClient), "OnNumberOfFlagsChanged")]
        private class Patch_OnNumberOfFlagsChanged //重写一个SiegeClient工作量有点大，暂时用HarmonyPatch代替，能减小服务器运算量
        {
            internal static bool Prefix(Action ___OnFlagNumberChangedEvent, SiegeMissionRepresentative ____myRepresentative, Action<GoldGain> ___OnGoldGainEvent)
            {
                ___OnFlagNumberChangedEvent?.Invoke();
                SiegeMissionRepresentative myRepresentative = ____myRepresentative;
                if (myRepresentative != null && myRepresentative.MissionPeer.Team?.Side == BattleSideEnum.Attacker)
                {
                    var list = new List<KeyValuePair<ushort, int>> { new KeyValuePair<ushort, int>(512, CNMSiegeServer.AttackerGoldBonusOnFlagRemoval) };
                    ___OnGoldGainEvent?.Invoke(new GoldGain(list));
                }
                else if (myRepresentative != null && myRepresentative.MissionPeer.Team?.Side == BattleSideEnum.Defender)
                {
                    var list = new List<KeyValuePair<ushort, int>> { new KeyValuePair<ushort, int>(512, CNMSiegeServer.DefenderGoldBonusOnFlagRemoval) };
                    ___OnGoldGainEvent?.Invoke(new GoldGain(list));
                }
                return false;
            }
        }
    }
}
#endif