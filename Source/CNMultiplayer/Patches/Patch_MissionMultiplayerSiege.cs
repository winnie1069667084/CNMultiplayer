using HarmonyLib;
using NetworkMessages.FromServer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;
using TaleWorlds.MountAndBlade.Objects;

namespace Patches
{

    [HarmonyPatch(typeof(MissionMultiplayerSiege), "CheckMorales")]//攻城模式士气Tick修改
    internal class Patch_CheckMorales
    {
        public static bool Prefix(float dt, MissionMultiplayerSiege __instance, ref int[] ____morales, ref float ____dtSumCheckMorales, int[] ____capturePointRemainingMoraleGains, MissionMultiplayerSiegeClient ____gameModeSiegeClient)//修改士气检查间隔
        {
            MethodInfo method = typeof(MissionMultiplayerSiege).GetMethod("GetMoraleGain", BindingFlags.Instance | BindingFlags.NonPublic);
            ____dtSumCheckMorales += dt;
            if (____dtSumCheckMorales >= 4f)
            {
                ____dtSumCheckMorales -= 4f;
                int num = MathF.Max(____morales[1] + (int)method.Invoke(__instance, new object[] { BattleSideEnum.Attacker }), 0);
                int num2 = MBMath.ClampInt(____morales[0] + (int)method.Invoke(__instance, new object[] { BattleSideEnum.Defender }), 0, 360);
                GameNetwork.BeginBroadcastModuleEvent();
                GameNetwork.WriteMessage(new SiegeMoraleChangeMessage(num, num2, ____capturePointRemainingMoraleGains));
                GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None, null);
                MissionMultiplayerSiegeClient gameModeSiegeClient = ____gameModeSiegeClient;
                if (gameModeSiegeClient != null)
                {
                    gameModeSiegeClient.OnMoraleChanged(num, num2, ____capturePointRemainingMoraleGains);
                }
                ____morales[1] = num;
                ____morales[0] = num2;
            }
            return false;
        }
    }

    [HarmonyPatch(typeof(MissionMultiplayerSiege), "GetMoraleGain")]//攻城模式士气获取逻辑修改
    internal class Patch_GetMoraleGain
    {
        public static bool Prefix(BattleSideEnum side, ref int __result, MissionMultiplayerSiege __instance, Agent ____masterFlagBestAgent, FlagCapturePoint ____masterFlag, ref int[] ____capturePointRemainingMoraleGains, ref MissionMultiplayerSiegeClient ____gameModeSiegeClient, ref MultiplayerGameNotificationsComponent ___NotificationsComponent)
        {
            int num = 0;
            List<KeyValuePair<ushort, int>> list = new List<KeyValuePair<ushort, int>>();
            bool flag2 = ____masterFlagBestAgent != null && ____masterFlagBestAgent.Team.Side == side;
            if (side == BattleSideEnum.Attacker)
            {
                if (____masterFlag.IsFullyRaised && __instance.GetFlagOwnerTeam(____masterFlag).Side == BattleSideEnum.Defender && !flag2)
                {
                    num += -1;
                }
                foreach (FlagCapturePoint item in __instance.AllCapturePoints.Where((FlagCapturePoint flag) => flag != ____masterFlag && !flag.IsDeactivated && __instance.GetFlagOwnerTeam(flag).Side == BattleSideEnum.Attacker))
                {
                    ____capturePointRemainingMoraleGains[item.FlagIndex]--;
                    num++;
                    if (____capturePointRemainingMoraleGains[item.FlagIndex] != 0)
                    {
                        continue;
                    }

                    //num += 90;//此处可以修改旗帜移除后攻城方获得的士气
                    foreach (NetworkCommunicator networkPeer in GameNetwork.NetworkPeers)
                    {
                        MissionPeer component = networkPeer.GetComponent<MissionPeer>();
                        if (component != null && component.Team?.Side == BattleSideEnum.Attacker)
                        {
                            __instance.ChangeCurrentGoldForPeer(component, __instance.GetCurrentGoldForPeer(component) + 35);//移除旗帜的金币数(进攻方)
                        }
                        if (component != null && component.Team?.Side == BattleSideEnum.Defender)
                        {
                            __instance.ChangeCurrentGoldForPeer(component, __instance.GetCurrentGoldForPeer(component) + 60);//移除旗帜的金币数(防守方)
                            list.Add(new KeyValuePair<ushort, int>(512, 60));
                            if (!component.Peer.Communicator.IsServerPeer && component.Peer.Communicator.IsConnectionActive)
                            {
                                GameNetwork.BeginModuleEventAsServer(component.Peer);
                                GameNetwork.WriteMessage(new GoldGain(list));
                                GameNetwork.EndModuleEventAsServer();
                            }
                            list.Clear();
                        }
                    }
                    item.RemovePointAsServer();
                    (__instance.SpawnComponent.SpawnFrameBehavior as SiegeSpawnFrameBehavior).OnFlagDeactivated(item);
                    ____gameModeSiegeClient.OnNumberOfFlagsChanged();
                    GameNetwork.BeginBroadcastModuleEvent();
                    GameNetwork.WriteMessage(new FlagDominationFlagsRemovedMessage());
                    GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None);
                    ___NotificationsComponent.FlagsXRemoved(item);
                }
                __result = num;
                return false;
            }

            if (__instance.GetFlagOwnerTeam(____masterFlag).Side == BattleSideEnum.Attacker && !____masterFlag.IsContested)
            {
                bool lockG = true;
                for (int i = 0; i < __instance.AllCapturePoints.Count; i++)
                {
                    if (__instance.AllCapturePoints[i] != ____masterFlag && !__instance.AllCapturePoints[i].IsDeactivated)
                    {
                        lockG = false;//锁G点
                    }
                }
                if (lockG)
                    num = -7;//守城方失去G点的士气下降速度
            }
            else
            {
                num++;
            }
            __result = num;
            return false;
        }
    }


    [HarmonyPatch(typeof(MissionMultiplayerSiege), "TickFlags")]//攻城模式旗帜占领半径修改、旗帜升降规则修改、占旗回血系统、降旗获取金币系统
    internal class Patch_TickFlags
    {
        public static bool Prefix(float dt, MissionMultiplayerSiege __instance, FlagCapturePoint ____masterFlag, ref Agent ____masterFlagBestAgent, ref Team[] ____capturePointOwners, ref MissionMultiplayerSiegeClient ____gameModeSiegeClient, ref MultiplayerGameNotificationsComponent ___NotificationsComponent, float ____dtSumCheckMorales)
        {

            foreach (FlagCapturePoint flagCapturePoint in __instance.AllCapturePoints)
            {
                if (!flagCapturePoint.IsDeactivated)
                {
                    Team flagOwnerTeam = __instance.GetFlagOwnerTeam(flagCapturePoint);
                    Agent agent = null;
                    float num = float.MaxValue;
                    int count1 = 0, count2 = 0;
                    List<KeyValuePair<ushort, int>> list = new List<KeyValuePair<ushort, int>>();
                    AgentProximityMap.ProximityMapSearchStruct proximityMapSearchStruct = AgentProximityMap.BeginSearch(Mission.Current, flagCapturePoint.Position.AsVec2, 6f, false);
                    while (proximityMapSearchStruct.LastFoundAgent != null)
                    {
                        Agent lastFoundAgent = proximityMapSearchStruct.LastFoundAgent;
                        if (!lastFoundAgent.IsMount && lastFoundAgent.IsActive())
                        {
                            if (flagCapturePoint.IsFullyRaised && lastFoundAgent.Team == flagOwnerTeam && (____dtSumCheckMorales % 0.5f < 0.25f))
                            {
                                lastFoundAgent.Health = Math.Min(lastFoundAgent.Health + 1f, lastFoundAgent.HealthLimit);//设定占旗回血量
                            }

                            if (flagCapturePoint.IsContested && !lastFoundAgent.IsAIControlled && lastFoundAgent.MissionPeer.Representative.Gold < 200 && (____dtSumCheckMorales % 0.25f < 0.25f))
                            {
                                __instance.ChangeCurrentGoldForPeer(lastFoundAgent.MissionPeer, lastFoundAgent.MissionPeer.Representative.Gold + 1);//设定占旗获取金币数
                                list.Add(new KeyValuePair<ushort, int>(512, 1));
                                if (!lastFoundAgent.MissionPeer.Peer.Communicator.IsServerPeer && lastFoundAgent.MissionPeer.Peer.Communicator.IsConnectionActive)
                                {
                                    GameNetwork.BeginModuleEventAsServer(lastFoundAgent.MissionPeer.Peer);
                                    GameNetwork.WriteMessage(new GoldGain(list));
                                    GameNetwork.EndModuleEventAsServer();
                                }
                                list.Clear();
                            }
                            if (lastFoundAgent.Team.IsAttacker && !lastFoundAgent.IsAIControlled)//计算旗帜内双方人数(机器人不算在内)
                                count1++;
                            if (lastFoundAgent.Team.IsDefender && !lastFoundAgent.IsAIControlled)
                                count2++;
                            float num2 = lastFoundAgent.Position.DistanceSquared(flagCapturePoint.Position);
                            if (num2 <= 36f && num2 < num)
                            {
                                agent = lastFoundAgent;
                                num = num2;
                            }
                        }
                        AgentProximityMap.FindNext(Mission.Current, ref proximityMapSearchStruct);
                    }
                    if (flagCapturePoint == ____masterFlag)
                    {
                        ____masterFlagBestAgent = agent;
                    }
                    CaptureTheFlagFlagDirection captureTheFlagFlagDirection = CaptureTheFlagFlagDirection.None;
                    bool isContested = flagCapturePoint.IsContested;
                    if ((count1 != 0 || count2 != 0) && ((flagOwnerTeam.IsDefender && count1 > count2) || (flagOwnerTeam.IsAttacker && count2 > count1)))//旗帜升降逻辑
                        captureTheFlagFlagDirection = CaptureTheFlagFlagDirection.Down;
                    if ((count1 == 0 && count2 == 0 && !flagCapturePoint.IsFullyRaised) || (isContested && ((flagOwnerTeam.IsDefender && count2 >= count1) || (flagOwnerTeam.IsAttacker && count1 >= count2))))
                        captureTheFlagFlagDirection = CaptureTheFlagFlagDirection.Up;
                    if (captureTheFlagFlagDirection != CaptureTheFlagFlagDirection.None)
                    {
                        flagCapturePoint.SetMoveFlag(captureTheFlagFlagDirection, 0.5f);//旗帜升降速度
                    }
                    bool flag;
                    flagCapturePoint.OnAfterTick(agent != null, out flag);
                    if (flag)
                    {
                        Team team = agent.Team;
                        uint color = (team != null) ? team.Color : 4284111450U;
                        uint color2 = (team != null) ? team.Color2 : uint.MaxValue;
                        flagCapturePoint.SetTeamColorsSynched(color, color2);
                        ____capturePointOwners[flagCapturePoint.FlagIndex] = team;
                        GameNetwork.BeginBroadcastModuleEvent();
                        GameNetwork.WriteMessage(new FlagDominationCapturePointMessage(flagCapturePoint.FlagIndex, team));
                        GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None, null);
                        MissionMultiplayerSiegeClient gameModeSiegeClient = ____gameModeSiegeClient;
                        if (gameModeSiegeClient != null)
                        {
                            gameModeSiegeClient.OnCapturePointOwnerChanged(flagCapturePoint, team);
                        }
                        ___NotificationsComponent.FlagXCapturedByTeamX(flagCapturePoint, agent.Team);
                    }
                }
            }
            return false;
        }
    }

    [HarmonyPatch(typeof(MissionMultiplayerSiege), "HandleNewClientAfterSynchronized")]//攻城模式初始资金修改
    internal class Patch_HandleNewClientAfterSynchronized
    {
        public static bool Prefix(NetworkCommunicator networkPeer, MissionMultiplayerSiege __instance, MultiplayerWarmupComponent ____warmupComponent, MissionMultiplayerSiegeClient ____gameModeSiegeClient, Team[] ____capturePointOwners)
        {
            MissionPeer missionpeer = networkPeer.GetComponent<MissionPeer>();
            int num = 200;
            if (____warmupComponent != null && ____warmupComponent.IsInWarmup)
            {
                num = 200;
            }
            __instance.ChangeCurrentGoldForPeer(missionpeer, num);
            MissionMultiplayerSiegeClient gameModeSiegeClient = ____gameModeSiegeClient;
            if (gameModeSiegeClient != null)
            {
                gameModeSiegeClient.OnGoldAmountChangedForRepresentative(networkPeer.GetComponent<TaleWorlds.MountAndBlade.MissionRepresentatives.SiegeMissionRepresentative>(), num);
            }
            if (__instance.AllCapturePoints != null && !networkPeer.IsServerPeer)
            {
                foreach (FlagCapturePoint flagCapturePoint in from cp in __instance.AllCapturePoints
                                                              where !cp.IsDeactivated
                                                              select cp)
                {
                    GameNetwork.BeginModuleEventAsServer(networkPeer);
                    GameNetwork.WriteMessage(new FlagDominationCapturePointMessage(flagCapturePoint.FlagIndex, ____capturePointOwners[flagCapturePoint.FlagIndex]));
                    GameNetwork.EndModuleEventAsServer();
                }
            }
            return false;
        }
    }

    [HarmonyPatch(typeof(MissionMultiplayerSiege), "OnPeerChangedTeam")]//攻城模式换边资金修改
    internal class Patch_OnPeerChangedTeam
    {
        public static bool Prefix(NetworkCommunicator peer, Team oldTeam, Team newTeam, MissionMultiplayerSiege __instance, MissionLobbyComponent ___MissionLobbyComponent)
        {
            if (___MissionLobbyComponent.CurrentMultiplayerState == MissionLobbyComponent.MultiplayerGameState.Playing && oldTeam != null && oldTeam != newTeam)
            {
                __instance.ChangeCurrentGoldForPeer(peer.GetComponent<MissionPeer>(), 150);
            }
            return false;
        }
    }

    [HarmonyPatch(typeof(MissionMultiplayerSiege), "OnAgentRemoved")]//攻城模式重生资金修改
    internal class Patch_OnAgentRemoved
    {
        public static bool Prefix(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow blow, MissionMultiplayerSiege __instance, MissionLobbyComponent ___MissionLobbyComponent)
        {
            if (___MissionLobbyComponent.CurrentMultiplayerState == MissionLobbyComponent.MultiplayerGameState.Playing && blow.DamageType != DamageTypes.Invalid && (agentState == AgentState.Unconscious || agentState == AgentState.Killed) && affectedAgent.IsHuman)
            {
                MissionPeer missionPeer = affectedAgent.MissionPeer;
                if (missionPeer != null)
                {
                    int num = 100;
                    if (affectorAgent != affectedAgent)
                    {
                        List<MissionPeer>[] array = new List<MissionPeer>[2];
                        for (int i = 0; i < array.Length; i++)
                        {
                            array[i] = new List<MissionPeer>();
                        }
                        foreach (NetworkCommunicator networkPeer in GameNetwork.NetworkPeers)
                        {
                            MissionPeer component = networkPeer.GetComponent<MissionPeer>();
                            if (component != null && component.Team != null && component.Team.Side != BattleSideEnum.None)
                            {
                                array[(int)component.Team.Side].Add(component);
                            }
                        }
                        int num2 = array[1].Count - array[0].Count;
                        BattleSideEnum battleSideEnum = (num2 == 0) ? BattleSideEnum.None : ((num2 < 0) ? BattleSideEnum.Attacker : BattleSideEnum.Defender);
                        if (battleSideEnum != BattleSideEnum.None && battleSideEnum == missionPeer.Team.Side)
                        {
                            num2 = MathF.Abs(num2);
                            int count = array[(int)battleSideEnum].Count;
                            if (count > 0)
                            {
                                int num3 = num * num2 / 10 / count * 10;
                                num += num3;
                            }
                        }
                    }
                    if (missionPeer.Team.Side == BattleSideEnum.Defender)
                    {
                        __instance.ChangeCurrentGoldForPeer(missionPeer, missionPeer.Representative.Gold + num);//守城方重生金币100
                    }
                    else
                    {
                        __instance.ChangeCurrentGoldForPeer(missionPeer, missionPeer.Representative.Gold + num + 50);//攻城方重生金币150
                    }
                }
                bool isFriendly = ((affectorAgent != null) ? affectorAgent.Team : null) != null && affectedAgent.Team != null && affectorAgent.Team.Side == affectedAgent.Team.Side;
                MultiplayerClassDivisions.MPHeroClass mpheroClassForCharacter = MultiplayerClassDivisions.GetMPHeroClassForCharacter(affectedAgent.Character);
                Agent.Hitter assistingHitter = affectedAgent.GetAssistingHitter((affectorAgent != null) ? affectorAgent.MissionPeer : null);
                if (((affectorAgent != null) ? affectorAgent.MissionPeer : null) != null && affectorAgent != affectedAgent && affectedAgent.Team != affectorAgent.Team)
                {
                    TaleWorlds.MountAndBlade.MissionRepresentatives.SiegeMissionRepresentative siegeMissionRepresentative = affectorAgent.MissionPeer.Representative as TaleWorlds.MountAndBlade.MissionRepresentatives.SiegeMissionRepresentative;
                    int goldGainsFromKillDataAndUpdateFlags = siegeMissionRepresentative.GetGoldGainsFromKillDataAndUpdateFlags(MPPerkObject.GetPerkHandler(affectorAgent.MissionPeer), MPPerkObject.GetPerkHandler((assistingHitter != null) ? assistingHitter.HitterPeer : null), mpheroClassForCharacter, false, blow.IsMissile, isFriendly);
                    __instance.ChangeCurrentGoldForPeer(affectorAgent.MissionPeer, siegeMissionRepresentative.Gold + goldGainsFromKillDataAndUpdateFlags);
                }
                if (((assistingHitter != null) ? assistingHitter.HitterPeer : null) != null && !assistingHitter.IsFriendlyHit)
                {
                    TaleWorlds.MountAndBlade.MissionRepresentatives.SiegeMissionRepresentative siegeMissionRepresentative2 = assistingHitter.HitterPeer.Representative as TaleWorlds.MountAndBlade.MissionRepresentatives.SiegeMissionRepresentative;
                    int goldGainsFromKillDataAndUpdateFlags2 = siegeMissionRepresentative2.GetGoldGainsFromKillDataAndUpdateFlags(MPPerkObject.GetPerkHandler((affectorAgent != null) ? affectorAgent.MissionPeer : null), MPPerkObject.GetPerkHandler(assistingHitter.HitterPeer), mpheroClassForCharacter, true, blow.IsMissile, isFriendly);
                    __instance.ChangeCurrentGoldForPeer(assistingHitter.HitterPeer, siegeMissionRepresentative2.Gold + goldGainsFromKillDataAndUpdateFlags2);
                }
                if (((missionPeer != null) ? missionPeer.Team : null) != null)
                {
                    MPPerkObject.MPPerkHandler perkHandler = MPPerkObject.GetPerkHandler(missionPeer);
                    IEnumerable<ValueTuple<MissionPeer, int>> enumerable = (perkHandler != null) ? perkHandler.GetTeamGoldRewardsOnDeath() : null;
                    if (enumerable != null)
                    {
                        foreach (ValueTuple<MissionPeer, int> valueTuple in enumerable)
                        {
                            MissionPeer item = valueTuple.Item1;
                            int item2 = valueTuple.Item2;
                            if (item2 > 0)
                            {
                                TaleWorlds.MountAndBlade.MissionRepresentatives.SiegeMissionRepresentative siegeMissionRepresentative3 = ((item != null) ? item.Representative : null) as TaleWorlds.MountAndBlade.MissionRepresentatives.SiegeMissionRepresentative;
                                if (siegeMissionRepresentative3 != null)
                                {
                                    int goldGainsFromAllyDeathReward = siegeMissionRepresentative3.GetGoldGainsFromAllyDeathReward(item2);
                                    if (goldGainsFromAllyDeathReward > 0)
                                    {
                                        __instance.ChangeCurrentGoldForPeer(item, siegeMissionRepresentative3.Gold + goldGainsFromAllyDeathReward);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return false;
        }
    }
}
