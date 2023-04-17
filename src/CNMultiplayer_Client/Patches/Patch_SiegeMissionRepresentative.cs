using HarmonyLib;
using NetworkMessages.FromServer;
using System.Collections.Generic;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.MissionRepresentatives;

namespace Patches
{
    [HarmonyPatch(typeof(SiegeMissionRepresentative), "GetGoldGainsFromKillDataAndUpdateFlags")]//攻城模式击杀金币系统修改
    internal class Patch_GetGoldGainsFromKillDataAndUpdateFlags
    {
        public static bool Prefix(MPPerkObject.MPPerkHandler killerPerkHandler, MPPerkObject.MPPerkHandler assistingHitterPerkHandler, MultiplayerClassDivisions.MPHeroClass victimClass, bool isAssist, bool isRanged, bool isFriendly, SiegeMissionRepresentative __instance, ref GoldGainFlags ____currentGoldGains, int ____assistCountOnSpawn, int ____killCountOnSpawn, ref int __result)
        {
            int num = 0;
            List<KeyValuePair<ushort, int>> list = new List<KeyValuePair<ushort, int>>();
            if (isAssist)
            {
                int num2 = 1;
                if (!isFriendly)
                {
                    int num3 = ((killerPerkHandler != null) ? killerPerkHandler.GetRewardedGoldOnAssist() : 0) + ((assistingHitterPerkHandler != null) ? assistingHitterPerkHandler.GetGoldOnAssist() : 0);
                    if (num3 > 0)
                    {
                        num += num3;
                        ____currentGoldGains |= GoldGainFlags.PerkBonus;
                        list.Add(new KeyValuePair<ushort, int>(2048, num3));
                    }
                }
                switch (__instance.MissionPeer.AssistCount - ____assistCountOnSpawn)
                {
                    case 1:
                        num += 1;
                        ____currentGoldGains |= GoldGainFlags.FirstAssist;
                        list.Add(new KeyValuePair<ushort, int>(4, 1));
                        break;
                    case 2:
                        num++;
                        ____currentGoldGains |= GoldGainFlags.SecondAssist;
                        list.Add(new KeyValuePair<ushort, int>(8, 1));
                        break;
                    case 3:
                        num++;
                        ____currentGoldGains |= GoldGainFlags.ThirdAssist;
                        list.Add(new KeyValuePair<ushort, int>(16, 1));
                        break;
                    default:
                        num += num2;
                        list.Add(new KeyValuePair<ushort, int>(256, num2));
                        break;
                }
            }
            else
            {
                int num4 = 0;
                if (__instance.ControlledAgent != null)
                {
                    num4 = MultiplayerClassDivisions.GetMPHeroClassForCharacter(__instance.ControlledAgent.Character).TroopCasualCost;
                    int num5 = victimClass.TroopCasualCost - num4;
                    int num6 = MBMath.ClampInt(num5 / 10, 2, 20);
                    num += num6;
                    list.Add(new KeyValuePair<ushort, int>(128, num6));
                }
                int num7 = (killerPerkHandler != null) ? killerPerkHandler.GetGoldOnKill((float)num4, (float)victimClass.TroopCasualCost) : 0;
                if (num7 > 0)
                {
                    num += num7;
                    ____currentGoldGains |= GoldGainFlags.PerkBonus;
                    list.Add(new KeyValuePair<ushort, int>(2048, num7));
                }
                int num8 = __instance.MissionPeer.KillCount - ____killCountOnSpawn;
                if (num8 != 5)
                {
                    if (num8 == 10)
                    {
                        num += 2;
                        ____currentGoldGains |= GoldGainFlags.TenthKill;
                        list.Add(new KeyValuePair<ushort, int>(64, 2));
                    }
                }
                else
                {
                    num += 2;
                    ____currentGoldGains |= GoldGainFlags.FifthKill;
                    list.Add(new KeyValuePair<ushort, int>(32, 2));
                }
                if (isRanged && !____currentGoldGains.HasAnyFlag(GoldGainFlags.FirstRangedKill))
                {
                    num += 2;
                    ____currentGoldGains |= GoldGainFlags.FirstRangedKill;
                    list.Add(new KeyValuePair<ushort, int>(1, 2));
                }
                if (!isRanged && !____currentGoldGains.HasAnyFlag(GoldGainFlags.FirstMeleeKill))
                {
                    num += 2;
                    ____currentGoldGains |= GoldGainFlags.FirstMeleeKill;
                    list.Add(new KeyValuePair<ushort, int>(2, 2));
                }
            }
            int num9 = 0;
            if (__instance.MissionPeer.Team == Mission.Current.Teams.Attacker)
            {
                num9 = MultiplayerOptions.OptionType.GoldGainChangePercentageTeam1.GetIntValue(MultiplayerOptions.MultiplayerOptionsAccessMode.CurrentMapOptions);
            }
            else if (__instance.MissionPeer.Team == Mission.Current.Teams.Defender)
            {
                num9 = MultiplayerOptions.OptionType.GoldGainChangePercentageTeam2.GetIntValue(MultiplayerOptions.MultiplayerOptionsAccessMode.CurrentMapOptions);
            }
            if (num9 != 0 && (num > 0 || list.Count > 0))
            {
                num = 0;
                float num10 = 1f + (float)num9 * 0.01f;
                for (int i = 0; i < list.Count; i++)
                {
                    int num11 = (int)((float)list[i].Value * num10);
                    list[i] = new KeyValuePair<ushort, int>(list[i].Key, num11);
                    num += num11;
                }
            }
            if (list.Count > 0 && !__instance.Peer.Communicator.IsServerPeer && __instance.Peer.Communicator.IsConnectionActive)
            {
                GameNetwork.BeginModuleEventAsServer(__instance.Peer);
                GameNetwork.WriteMessage(new GoldGain(list));
                GameNetwork.EndModuleEventAsServer();
            }
            __result = num;
            return false;
        }
    }

    [HarmonyPatch(typeof(SiegeMissionRepresentative), "GetTotalGoldDistributionForDestructable")]//攻城模式器械摧毁&贡献度金币系统修改
    internal class Patch_GetTotalGoldDistributionForDestructable
    {
        public static bool Prefix(GameEntity objectiveMostParentEntity, ref int __result)
        {
            string text = null;
            foreach (string text2 in objectiveMostParentEntity.Tags)
            {
                if (text2.StartsWith("mp_siege_objective_"))
                {
                    text = text2;
                    break;
                }
            }
            if (text == null)
            {
                __result = 50;
                return false;
            }
            string a = text.Replace("mp_siege_objective_", "");
            if (a == "wall_breach" || a == "castle_gate")
            {
                __result = 300;
                return false;
            }
            if (!(a == "battering_ram") && !(a == "siege_tower"))
            {
                __result = 50;
                return false;
            }
            __result = 300;
            return false;
        }
    }
}
