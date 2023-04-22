using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.DedicatedCustomServer;
using TaleWorlds.MountAndBlade.Diamond;
using TaleWorlds.PlayerServices;

namespace ServerPatches.Patches
{

    [HarmonyPatch(typeof(MissionLobbyComponent), "OnPlayerKills")]
    class MissionLobbyComponent_OnPlayerKills
    {
        [HarmonyReversePatch]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void OnPlayerKills(MissionLobbyComponent instance, MissionPeer killerPeer, Agent killedAgent, MissionPeer assistorPeer)
        {

        }
    }


    public class PatchMissionCustomGameServerComponent_OnPlayerKills
    {
        static bool hitOnce = false;
        public static bool Prefix(MissionCustomGameServerComponent __instance, MissionPeer killerPeer, Agent killedAgent, MissionPeer assistorPeer)
        {
            if (!hitOnce)
            {
                Logging.Instance.Info("PatchMissionCustomGameServerComponent_OnPlayerKills.Prefix has been hit once");
                hitOnce = true;
            }

            //base.OnPlayerKills(killerPeer, killedAgent, assistorPeer);
            MissionLobbyComponent_OnPlayerKills.OnPlayerKills(__instance, killerPeer, killedAgent, assistorPeer);

            PlayerId id = killerPeer.Peer.Id;
            //BattleResult currentBattleResult = this._battleResult.GetCurrentBattleResult();
            MultipleBattleResult _battleResult = Traverse.Create(__instance).Field("_battleResult").GetValue() as MultipleBattleResult;
            BattleResult currentBattleResult = _battleResult.GetCurrentBattleResult();

            //if (__instance._warmupEnded)            
            bool _warmupEnded = (bool)Traverse.Create(__instance).Field("_warmupEnded").GetValue();
            if (_warmupEnded)
            {
                currentBattleResult.PlayerEntries[id].PlayerStats.Kills = killerPeer.KillCount;
            }
            if (killerPeer != null && killedAgent != null && killedAgent.IsHuman)
            {
                GameLog gameLog = new GameLog(GameLogType.Kill, killerPeer.Peer.Id, MBCommon.GetTotalMissionTime());
                Dictionary<string, string> data = gameLog.Data;
                string key = "IsFriendly";
                Team team = killerPeer.Team;
                BattleSideEnum? battleSideEnum = (team != null) ? new BattleSideEnum?(team.Side) : null;
                BattleSideEnum? battleSideEnum2;
                if (killedAgent == null)
                {
                    battleSideEnum2 = null;
                }
                else
                {
                    Team team2 = killedAgent.Team;
                    battleSideEnum2 = ((team2 != null) ? new BattleSideEnum?(team2.Side) : null);
                }
                BattleSideEnum? battleSideEnum3 = battleSideEnum2;
                data.Add(key, (battleSideEnum.GetValueOrDefault() == battleSideEnum3.GetValueOrDefault() & battleSideEnum != null == (battleSideEnum3 != null)).ToString());
                if (killedAgent.MissionPeer != null)
                {
                    gameLog.Data.Add("Victim", killedAgent.MissionPeer.Peer.Id.ToString());
                }
                if (assistorPeer != null)
                {
                    gameLog.Data.Add("Assist", assistorPeer.Peer.Id.ToString());
                }
                //if (this._warmupEnded && this._gameMode.GetMissionType() == MissionLobbyComponent.MultiplayerGameType.Siege)
                _warmupEnded = (bool)Traverse.Create(__instance).Field("_warmupEnded").GetValue();
                MissionMultiplayerGameModeBase _gameMode = Traverse.Create(__instance).Field("_gameMode").GetValue() as MissionMultiplayerGameModeBase;
                if (_warmupEnded && _gameMode.GetMissionType() == MissionLobbyComponent.MultiplayerGameType.Siege)
                {
                    Agent controlledAgent = killerPeer.ControlledAgent;
                    //if (((controlledAgent != null) ? controlledAgent.CurrentlyUsedGameObject : null) != null)
                    if (controlledAgent != null && controlledAgent.CurrentlyUsedGameObject != null)
                    {
                        if (currentBattleResult.PlayerEntries != null)
                        {
                            if (killerPeer.Peer != null && killerPeer.Peer.Id != null)
                            {
                                if (currentBattleResult.PlayerEntries[killerPeer.Peer.Id] != null)
                                {
                                    BattlePlayerStatsSiege battlePlayerStatsSiege = currentBattleResult.PlayerEntries[killerPeer.Peer.Id].PlayerStats as BattlePlayerStatsSiege;
                                    if (battlePlayerStatsSiege != null)
                                    {
                                        int siegeEngineKills = battlePlayerStatsSiege.SiegeEngineKills;
                                        battlePlayerStatsSiege.SiegeEngineKills = siegeEngineKills + 1;
                                    }
                                    else
                                    {
                                        Logging.Instance.Error("PatchMissionCustomGameServerComponent_OnPlayerKills: battlePlayerStatsSiege was null!");
                                    }
                                }
                                else
                                {
                                    Logging.Instance.Error("PatchMissionCustomGameServerComponent_OnPlayerKills: currentBattleResult.PlayerEntries[killerPeer.Peer.Id] was null!");
                                }
                            }
                            else
                            {
                                Logging.Instance.Error("PatchMissionCustomGameServerComponent_OnPlayerKills: killerPeer.Peer/killerPeer.Peer.Id was null!");
                            }
                        }
                        else
                        {
                            Logging.Instance.Error("PatchMissionCustomGameServerComponent_OnPlayerKills: currentBattleResult.PlayerEntries was null!");
                        }
                    }
                    else
                    {
                        if (controlledAgent == null)
                        {
                            Logging.Instance.Error("PatchMissionCustomGameServerComponent_OnPlayerKills: killerPeer.ControlledAgent was null!");
                        }
                    }
                }
                //this._gameLogger.GameLogs.Add(gameLog);
                MultiplayerGameLogger _gameLogger = Traverse.Create(__instance).Field("_gameLogger").GetValue() as MultiplayerGameLogger;
                _gameLogger.GameLogs.Add(gameLog);
            }

            //this._customBattleServer.UpdateBattleStats(this._battleResult.GetCurrentBattleResult(), this._teamScores, this._playerScores);
            CustomBattleServer _customBattleServer = Traverse.Create(__instance).Field("_customBattleServer").GetValue() as CustomBattleServer;
            Dictionary<int, int> _teamScores = Traverse.Create(__instance).Field("_teamScores").GetValue() as Dictionary<int, int>;
            Dictionary<PlayerId, int> _playerScores = Traverse.Create(__instance).Field("_playerScores").GetValue() as Dictionary<PlayerId, int>;
            currentBattleResult = _battleResult.GetCurrentBattleResult();

            _customBattleServer.UpdateBattleStats(currentBattleResult, _teamScores, _playerScores);

            return false;
        }
    }

    public class PatchMissionCustomGameServerComponent_OnObjectiveGoldGained
    {
        static bool hitOnce = false;
        public static bool Prefix(MissionCustomGameServerComponent __instance, MissionPeer peer, int goldGain)
        {
            if (!hitOnce)
            {
                Logging.Instance.Info("PatchMissionCustomGameServerComponent_OnObjectiveGoldGained.Prefix has been hit once");
                hitOnce = true;
            }

            // if (this._warmupEnded)
            bool _warmupEnded = (bool)Traverse.Create(__instance).Field("_warmupEnded").GetValue();
            if (_warmupEnded)
            {
                if (peer != null)
                {
                    if (peer.Peer != null)
                    {
                        if (peer.Peer.Id != null)
                        {
                            //(this._battleResult.GetCurrentBattleResult().PlayerEntries[peer.Peer.Id].PlayerStats as BattlePlayerStatsSiege).ObjectiveGoldGained += goldGain;
                            MultipleBattleResult _battleResult = Traverse.Create(__instance).Field("_battleResult").GetValue() as MultipleBattleResult;
                            BattleResult currentBattleResult = _battleResult.GetCurrentBattleResult();
                            if (currentBattleResult != null)
                            {
                                BattlePlayerStatsSiege battlePlayerStatsSiege = currentBattleResult.PlayerEntries[peer.Peer.Id].PlayerStats as BattlePlayerStatsSiege;
                                if (battlePlayerStatsSiege != null)
                                {
                                    battlePlayerStatsSiege.ObjectiveGoldGained += goldGain;

                                    //this._customBattleServer.UpdateBattleStats(this._battleResult.GetCurrentBattleResult(), this._teamScores, this._playerScores);
                                    CustomBattleServer _customBattleServer = Traverse.Create(__instance).Field("_customBattleServer").GetValue() as CustomBattleServer;
                                    Dictionary<int, int> _teamScores = Traverse.Create(__instance).Field("_teamScores").GetValue() as Dictionary<int, int>;
                                    Dictionary<PlayerId, int> _playerScores = Traverse.Create(__instance).Field("_playerScores").GetValue() as Dictionary<PlayerId, int>;
                                    currentBattleResult = _battleResult.GetCurrentBattleResult();

                                    _customBattleServer.UpdateBattleStats(currentBattleResult, _teamScores, _playerScores);
                                }
                                else
                                {
                                    Logging.Instance.Error("PatchMissionCustomGameServerComponent_OnObjectiveGoldGained: battlePlayerStatsSiege was null!");
                                }

                            }
                            else
                            {
                                Logging.Instance.Error("PatchMissionCustomGameServerComponent_OnObjectiveGoldGained: currentBattleResult was null!");
                            }

                        }
                        else
                        {
                            Logging.Instance.Error("PatchMissionCustomGameServerComponent_OnObjectiveGoldGained: peer.Peer.Id was null!");
                        }
                    }
                    else
                    {
                        Logging.Instance.Error("PatchMissionCustomGameServerComponent_OnObjectiveGoldGained: peer.Peer was null!");
                    }

                }
                else
                {
                    Logging.Instance.Error("PatchMissionCustomGameServerComponent_OnObjectiveGoldGained: peer was null!");
                }
            }

            return false;
        }
    }

    public class PatchMissionCustomGameServerComponent_OnDestructableComponentDestroyed
    {
        static bool hitOnce = false;
        public static bool Prefix(MissionCustomGameServerComponent __instance, DestructableComponent destructableComponent, ScriptComponentBehavior attackerScriptComponentBehaviour, MissionPeer[] contributors)
        {
            if (!hitOnce)
            {
                Logging.Instance.Info("PatchMissionCustomGameServerComponent_OnDestructableComponentDestroyed.Prefix has been hit once");
                hitOnce = true;
            }

            bool _warmupEnded = (bool)Traverse.Create(__instance).Field("_warmupEnded").GetValue();
            if (_warmupEnded)
            {

                foreach (MissionPeer missionPeer in contributors)
                {

                    MultipleBattleResult _battleResult = Traverse.Create(__instance).Field("_battleResult").GetValue() as MultipleBattleResult;
                    MethodInfo dynMethod = typeof(MissionCustomGameServerComponent).GetMethod("CheckForComponent", BindingFlags.NonPublic | BindingFlags.Instance);
                    bool checkForComponent = (bool)dynMethod.Invoke(__instance, new object[] { destructableComponent, typeof(SiegeWeapon) });
                    if (missionPeer != null)
                    {
                        if (missionPeer.Peer != null)
                        {
                            if (missionPeer.Peer.Id != null)
                            {
                                BattlePlayerEntry battlePlayerEntry;
                                if (_battleResult.GetCurrentBattleResult().TryGetPlayerEntry(missionPeer.Peer.Id, out battlePlayerEntry) && checkForComponent)
                                {

                                    BattlePlayerStatsSiege battlePlayerStatsSiege = battlePlayerEntry.PlayerStats as BattlePlayerStatsSiege;
                                    if (battlePlayerStatsSiege != null)
                                    {
                                        int siegeEnginesDestroyed = battlePlayerStatsSiege.SiegeEnginesDestroyed;
                                        battlePlayerStatsSiege.SiegeEnginesDestroyed = siegeEnginesDestroyed + 1;
                                    }
                                    else
                                    {
                                        Logging.Instance.Error("PatchMissionCustomGameServerComponent_OnDestructableComponentDestroyed: battlePlayerStatsSiege was null");
                                    }

                                }
                            }
                            else
                            {
                                Logging.Instance.Error("PatchMissionCustomGameServerComponent_OnDestructableComponentDestroyed: missionPeer.Peer.Id was null");
                            }
                        }
                        else
                        {
                            Logging.Instance.Error("PatchMissionCustomGameServerComponent_OnDestructableComponentDestroyed: missionPeer.Peer was null");
                        }
                    }
                    else
                    {
                        Logging.Instance.Error("PatchMissionCustomGameServerComponent_OnDestructableComponentDestroyed: missionPeer in contributors was null");
                    }


                    CustomBattleServer _customBattleServer = Traverse.Create(__instance).Field("_customBattleServer").GetValue() as CustomBattleServer;
                    Dictionary<int, int> _teamScores = Traverse.Create(__instance).Field("_teamScores").GetValue() as Dictionary<int, int>;
                    Dictionary<PlayerId, int> _playerScores = Traverse.Create(__instance).Field("_playerScores").GetValue() as Dictionary<PlayerId, int>;
                    BattleResult currentBattleResult = _battleResult.GetCurrentBattleResult();

                    _customBattleServer.UpdateBattleStats(currentBattleResult, _teamScores, _playerScores);
                }
            }

            return false;
        }
    }
}