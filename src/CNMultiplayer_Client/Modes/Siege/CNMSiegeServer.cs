using System;
using System.Collections.Generic;
using System.Linq;
using NetworkMessages.FromServer;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.MissionRepresentatives;
using TaleWorlds.MountAndBlade.Objects;
using TaleWorlds.ObjectSystem;

namespace CNMultiplayer_Client.Modes.Siege
{
    public class CNMSiegeServer : MissionMultiplayerGameModeBase, IAnalyticsFlagInfo
    {
        public delegate void OnDestructableComponentDestroyedDelegate(DestructableComponent destructableComponent, ScriptComponentBehavior attackerScriptComponentBehaviour, MissionPeer[] contributors);

        public delegate void OnObjectiveGoldGainedDelegate(MissionPeer peer, int goldGain);

        public const int MaxMorale = 1440;

        public const int StartingMorale = 360; //双方初始士气

        public const int MaxMoraleGainPerFlag = 90;

        public const int MoraleGainPerFlag = 1; //被占领的旗帜提供的士气值

        public const int AttackerGoldBonusOnFlagRemoval = 75; //攻城方移除旗帜金币奖励

        public const int DefenderGoldBonusOnFlagRemoval = 150; //守城方移除旗帜金币补偿

        public const string MasterFlagTag = "keep_capture_point";

        public override bool IsGameModeHidingAllAgentVisuals => true;

        public override bool IsGameModeUsingOpposingTeams => true;

        public MBReadOnlyList<FlagCapturePoint> AllCapturePoints { get; private set; }

        public event OnDestructableComponentDestroyedDelegate OnDestructableComponentDestroyed;

        public event OnObjectiveGoldGainedDelegate OnObjectiveGoldGained;

        private const int FirstSpawnGold = 300; //初始金币

        private const int FirstSpawnGoldForEarlyJoin = 300; //初始金币

        private const int ChangeTeamGold = 150; //换边金币

        private const int RespawnGold = 100; //基础重生金币

        private const int AttackerRespawnGold = 50; //攻城方重生奖励金币

        private const int DefenderRespawnGold = 25; //守城方重生奖励金币

        private const int AttackerFlagGoldHoldMax = 400; //攻城方持有旗帜金币最大值

        private const int DefenderFlagGoldHoldMax = 200; //守城方持有旗帜金币最大值

        private const float radius = 20f; //定义旗帜半径

        private const float ObjectiveCheckPeriod = 0.25f;

        private const float MoraleTickTimeInSeconds = 3.5f; //士气Tick

        private const float HealTick = 1f; //旗帜回血Tick

        private const float AttackerFlagGoldTick = 0.75f; //攻城方旗帜金币Tick

        private const float DefenderFlagGoldTick = 1.5f; //守城方旗帜金币Tick

        private const float FlagSpeedPerPlayer = 0.15f; //每人提供的旗帜升降速度

        private const float FlagSpeedMax = 1f; //最大旗帜升降速度

        private const float FlagSpeedMin = 0.1f; //最小旗帜升降速度（不得低于0.1f）

        private const int FlagGoldGain = 1; //旗帜金币量

        private const int HealGain = 1; //旗帜回血量

        private const int MoraleBoostOnFlagRemoval = 0; //攻城方移除旗帜的士气奖励

        private const int MoraleDecayInTick = 1; //攻城方基础士气衰减

        private const int DefenderMoraleDecayInTick = -10; //守城方失去G的士气衰减

        private const int FlagLockNum = 3; //锁点数量

        private int[] _morales;

        private FlagCapturePoint _masterFlag;

        private Team[] _capturePointOwners;

        private int[] _capturePointRemainingMoraleGains;

        private float _dtSumCheckMorales;

        private float _dtSumObjectiveCheck;

        private ObjectiveSystem _objectiveSystem;

        private (IMoveableSiegeWeapon, Vec3)[] _movingObjectives;

        private (RangedSiegeWeapon, Agent)[] _lastReloadingAgentPerRangedSiegeMachine;

        private MissionMultiplayerSiegeClient _gameModeSiegeClient;

        private MultiplayerWarmupComponent _warmupComponent;

        private Dictionary<GameEntity, List<DestructableComponent>> _childDestructableComponents;

        private MissionScoreboardComponent _missionScoreboardComponent;

        private bool _firstTickDone;

        private class ObjectiveSystem
        {
            private class ObjectiveContributor
            {
                public readonly MissionPeer Peer;

                public float Contribution { get; private set; }

                public ObjectiveContributor(MissionPeer peer, float initialContribution)
                {
                    Peer = peer;
                    Contribution = initialContribution;
                }

                public void IncreaseAmount(float deltaContribution)
                {
                    Contribution += deltaContribution;
                }
            }

            private readonly Dictionary<GameEntity, List<ObjectiveContributor>[]> _objectiveContributorMap;

            public ObjectiveSystem()
            {
                _objectiveContributorMap = new Dictionary<GameEntity, List<ObjectiveContributor>[]>();
            }

            public bool RegisterObjective(GameEntity entity)
            {
                if (!_objectiveContributorMap.ContainsKey(entity))
                {
                    _objectiveContributorMap.Add(entity, new List<ObjectiveContributor>[2]);
                    for (int i = 0; i < 2; i++)
                    {
                        _objectiveContributorMap[entity][i] = new List<ObjectiveContributor>();
                    }
                    return true;
                }
                return false;
            }

            public void AddContributionForObjective(GameEntity objectiveEntity, MissionPeer contributorPeer, float contribution)
            {
                string text = objectiveEntity.Tags.FirstOrDefault((string x) => x.StartsWith("mp_siege_objective_")) ?? "";
                bool flag = false;
                for (int i = 0; i < 2; i++)
                {
                    foreach (ObjectiveContributor item in _objectiveContributorMap[objectiveEntity][i])
                    {
                        if (item.Peer == contributorPeer)
                        {
                            Debug.Print($"[CONT > {text}] Increased contribution for {contributorPeer.Name}({contributorPeer.Team.Side}) by {contribution}.", 0, Debug.DebugColor.White, 17179869184uL);
                            item.IncreaseAmount(contribution);
                            flag = true;
                            break;
                        }
                    }
                    if (flag)
                    {
                        break;
                    }
                }
                if (!flag)
                {
                    Debug.Print($"[CONT > {text}] Adding {contribution} contribution for {contributorPeer.Name}({contributorPeer.Team.Side}).", 0, Debug.DebugColor.White, 17179869184uL);
                    _objectiveContributorMap[objectiveEntity][(int)contributorPeer.Team.Side].Add(new ObjectiveContributor(contributorPeer, contribution));
                }
            }

            public List<KeyValuePair<MissionPeer, float>> GetAllContributorsForSideAndClear(GameEntity objectiveEntity, BattleSideEnum side)
            {
                List<KeyValuePair<MissionPeer, float>> list = new List<KeyValuePair<MissionPeer, float>>();
                string text = objectiveEntity.Tags.FirstOrDefault((string x) => x.StartsWith("mp_siege_objective_")) ?? "";
                foreach (ObjectiveContributor item in _objectiveContributorMap[objectiveEntity][(int)side])
                {
                    Debug.Print($"[CONT > {text}] Rewarding {item.Contribution} contribution for {item.Peer.Name}({side}).", 0, Debug.DebugColor.White, 17179869184uL);
                    list.Add(new KeyValuePair<MissionPeer, float>(item.Peer, item.Contribution));
                }
                _objectiveContributorMap[objectiveEntity][(int)side].Clear();
                return list;
            }
        }

        public override void OnBehaviorInitialize()
        {
            base.OnBehaviorInitialize();
            _objectiveSystem = new ObjectiveSystem();
            _childDestructableComponents = new Dictionary<GameEntity, List<DestructableComponent>>();
            _gameModeSiegeClient = Mission.Current.GetMissionBehavior<MissionMultiplayerSiegeClient>();
            _warmupComponent = Mission.Current.GetMissionBehavior<MultiplayerWarmupComponent>();
            _missionScoreboardComponent = Mission.Current.GetMissionBehavior<MissionScoreboardComponent>();
            InitializeMorales();
            InitializeFlags();
            foreach (DestructableComponent item2 in Mission.Current.MissionObjects.FindAllWithType<DestructableComponent>())
            {
                if (item2.BattleSide != BattleSideEnum.None)
                {
                    GameEntity root = item2.GameEntity.Root;
                    if (_objectiveSystem.RegisterObjective(root))
                    {
                        _childDestructableComponents.Add(root, new List<DestructableComponent>());
                        GetDestructableCompoenentClosestToTheRoot(root).OnDestroyed += DestructableComponentOnDestroyed;
                    }
                    _childDestructableComponents[root].Add(item2);
                    item2.OnHitTaken += DestructableComponentOnHitTaken;
                }
            }
            List<RangedSiegeWeapon> list = new List<RangedSiegeWeapon>();
            List<IMoveableSiegeWeapon> list2 = new List<IMoveableSiegeWeapon>();
            foreach (UsableMachine item3 in Mission.Current.MissionObjects.FindAllWithType<UsableMachine>())
            {
                if (item3 is RangedSiegeWeapon rangedSiegeWeapon)
                {
                    list.Add(rangedSiegeWeapon);
                    rangedSiegeWeapon.OnAgentLoadsMachine += RangedSiegeMachineOnAgentLoadsMachine;
                }
                else if (item3 is IMoveableSiegeWeapon item)
                {
                    list2.Add(item);
                    _objectiveSystem.RegisterObjective(item3.GameEntity.Root);
                }
            }
            _lastReloadingAgentPerRangedSiegeMachine = new (RangedSiegeWeapon, Agent)[list.Count];
            for (int i = 0; i < _lastReloadingAgentPerRangedSiegeMachine.Length; i++)
            {
                _lastReloadingAgentPerRangedSiegeMachine[i] = ValueTuple.Create<RangedSiegeWeapon, Agent>(list[i], null);
            }
            _movingObjectives = new (IMoveableSiegeWeapon, Vec3)[list2.Count];
            for (int j = 0; j < _movingObjectives.Length; j++)
            {
                SiegeWeapon siegeWeapon = list2[j] as SiegeWeapon;
                _movingObjectives[j] = ValueTuple.Create(list2[j], siegeWeapon.GameEntity.GlobalPosition);
            }
        }

        private static DestructableComponent GetDestructableCompoenentClosestToTheRoot(GameEntity entity)
        {
            DestructableComponent destructableComponent = entity.GetFirstScriptOfType<DestructableComponent>();
            while (destructableComponent == null && entity.ChildCount != 0)
            {
                for (int i = 0; i < entity.ChildCount; i++)
                {
                    destructableComponent = GetDestructableCompoenentClosestToTheRoot(entity.GetChild(i));
                    if (destructableComponent != null)
                    {
                        break;
                    }
                }
            }
            return destructableComponent;
        }

        private void RangedSiegeMachineOnAgentLoadsMachine(RangedSiegeWeapon siegeWeapon, Agent reloadingAgent)
        {
            for (int i = 0; i < _lastReloadingAgentPerRangedSiegeMachine.Length; i++)
            {
                if (_lastReloadingAgentPerRangedSiegeMachine[i].Item1 == siegeWeapon)
                {
                    _lastReloadingAgentPerRangedSiegeMachine[i].Item2 = reloadingAgent;
                }
            }
        }

        private void DestructableComponentOnHitTaken(DestructableComponent destructableComponent, Agent attackerAgent, in MissionWeapon weapon, ScriptComponentBehavior attackerScriptComponentBehavior, int inflictedDamage)
        {
            if (WarmupComponent.IsInWarmup)
            {
                return;
            }
            GameEntity root = destructableComponent.GameEntity.Root;
            if (attackerScriptComponentBehavior is BatteringRam batteringRam)
            {
                int userCountNotInStruckAction = batteringRam.UserCountNotInStruckAction;
                if (userCountNotInStruckAction > 0)
                {
                    float contribution = (float)inflictedDamage / (float)userCountNotInStruckAction;
                    foreach (StandingPoint standingPoint2 in batteringRam.StandingPoints)
                    {
                        Agent userAgent = standingPoint2.UserAgent;
                        if (userAgent?.MissionPeer != null && !userAgent.IsInBeingStruckAction && userAgent.MissionPeer.Team.Side == destructableComponent.BattleSide.GetOppositeSide())
                        {
                            _objectiveSystem.AddContributionForObjective(root, userAgent.MissionPeer, contribution);
                        }
                    }
                }
            }
            else if (attackerAgent?.MissionPeer?.Team != null && attackerAgent.MissionPeer.Team.Side == destructableComponent.BattleSide.GetOppositeSide())
            {
                if (attackerAgent.CurrentlyUsedGameObject != null && attackerAgent.CurrentlyUsedGameObject is StandingPoint standingPoint)
                {
                    RangedSiegeWeapon firstScriptOfTypeInFamily = standingPoint.GameEntity.GetFirstScriptOfTypeInFamily<RangedSiegeWeapon>();
                    if (firstScriptOfTypeInFamily != null)
                    {
                        for (int i = 0; i < _lastReloadingAgentPerRangedSiegeMachine.Length; i++)
                        {
                            if (_lastReloadingAgentPerRangedSiegeMachine[i].Item1 == firstScriptOfTypeInFamily && _lastReloadingAgentPerRangedSiegeMachine[i].Item2?.MissionPeer != null && _lastReloadingAgentPerRangedSiegeMachine[i].Item2?.MissionPeer.Team.Side == destructableComponent.BattleSide.GetOppositeSide())
                            {
                                _objectiveSystem.AddContributionForObjective(root, _lastReloadingAgentPerRangedSiegeMachine[i].Item2.MissionPeer, (float)inflictedDamage * 0.33f);
                            }
                        }
                    }
                }
                _objectiveSystem.AddContributionForObjective(root, attackerAgent.MissionPeer, inflictedDamage);
            }
            if (destructableComponent.IsDestroyed)
            {
                destructableComponent.OnHitTaken -= DestructableComponentOnHitTaken;
                _childDestructableComponents[root].Remove(destructableComponent);
            }
        }

        private void DestructableComponentOnDestroyed(DestructableComponent destructableComponent, Agent attackerAgent, in MissionWeapon weapon, ScriptComponentBehavior attackerScriptComponentBehavior, int inflictedDamage)
        {
            GameEntity root = destructableComponent.GameEntity.Root;
            List<KeyValuePair<MissionPeer, float>> allContributorsForSideAndClear = _objectiveSystem.GetAllContributorsForSideAndClear(root, destructableComponent.BattleSide.GetOppositeSide());
            float num = allContributorsForSideAndClear.Sum((KeyValuePair<MissionPeer, float> ac) => ac.Value);
            List<MissionPeer> list = new List<MissionPeer>();
            foreach (KeyValuePair<MissionPeer, float> item in allContributorsForSideAndClear)
            {
                int goldGainsFromObjectiveAssist = (item.Key.Representative as SiegeMissionRepresentative).GetGoldGainsFromObjectiveAssist(root, item.Value / num, isCompleted: false);
                if (goldGainsFromObjectiveAssist > 0)
                {
                    ChangeCurrentGoldForPeer(item.Key, item.Key.Representative.Gold + goldGainsFromObjectiveAssist);
                    list.Add(item.Key);
                    this.OnObjectiveGoldGained?.Invoke(item.Key, goldGainsFromObjectiveAssist);
                }
            }
            destructableComponent.OnDestroyed -= DestructableComponentOnDestroyed;
            foreach (DestructableComponent item2 in _childDestructableComponents[root])
            {
                item2.OnHitTaken -= DestructableComponentOnHitTaken;
            }
            _childDestructableComponents.Remove(root);
            this.OnDestructableComponentDestroyed?.Invoke(destructableComponent, attackerScriptComponentBehavior, list.ToArray());
        }

        public override MissionLobbyComponent.MultiplayerGameType GetMissionType()
            => MissionLobbyComponent.MultiplayerGameType.Siege; // Helps to avoid a few crashes.

        public override bool UseRoundController() => false;

        public override void AfterStart()
        {
            AddTeams();
            InitialLockFlags(AllCapturePoints, FlagLockNum);
            _warmupComponent.OnWarmupEnding += OnWarmupEnding;
        }

        public override void OnMissionTick(float dt)
        {
            base.OnMissionTick(dt);
            if (!_firstTickDone)
            {
                foreach (CastleGate gate in Mission.MissionObjects.FindAllWithType<CastleGate>())
                {
                    gate.OpenDoor();
                    foreach (StandingPoint standingPoint in gate.StandingPoints)
                    {
                        standingPoint.SetIsDeactivatedSynched(true);
                    }
                }
                _firstTickDone = true;
            }
            if (MissionLobbyComponent.CurrentMultiplayerState == MissionLobbyComponent.MultiplayerGameState.Playing && !WarmupComponent.IsInWarmup)
            {
                CheckMorales(dt);
                if (CheckObjectives(dt))
                {
                    TickFlags();
                    TickObjectives();
                }
            }
        }

        private void CheckMorales(float dt)
        {
            _dtSumCheckMorales += dt;
            if (_dtSumCheckMorales >= MoraleTickTimeInSeconds)
            {
                _dtSumCheckMorales -= MoraleTickTimeInSeconds;
                int attackerMorale = MathF.Max(_morales[(int)BattleSideEnum.Attacker] + GetMoraleGain(BattleSideEnum.Attacker), 0);
                int defenderMorale = MBMath.ClampInt(_morales[(int)BattleSideEnum.Defender] + GetMoraleGain(BattleSideEnum.Defender), 0, StartingMorale);
                GameNetwork.BeginBroadcastModuleEvent();
                GameNetwork.WriteMessage(new SiegeMoraleChangeMessage(attackerMorale, defenderMorale, _capturePointRemainingMoraleGains));
                GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None);
                _gameModeSiegeClient?.OnMoraleChanged(attackerMorale, defenderMorale, _capturePointRemainingMoraleGains);
                _morales[(int)BattleSideEnum.Attacker] = attackerMorale;
                _morales[(int)BattleSideEnum.Defender] = defenderMorale;
            }
        }

        public override bool CheckForMatchEnd()
        {
            return _morales.Any((int morale) => morale == 0);
        }

        public override Team GetWinnerTeam()
        {
            Team winnerteam = null;
            if (_morales[(int)BattleSideEnum.Attacker] <= 0 && _morales[(int)BattleSideEnum.Defender] > 0)
                winnerteam = Mission.Teams.Defender;
            else if (_morales[(int)BattleSideEnum.Defender] <= 0 && _morales[(int)BattleSideEnum.Attacker] > 0)
                winnerteam = Mission.Teams.Attacker;
            winnerteam = winnerteam ?? Mission.Teams.Defender;
            _missionScoreboardComponent.ChangeTeamScore(winnerteam, 1);
            return winnerteam;
        }

        private int GetMoraleGain(BattleSideEnum side)
        {
            int moraleGain = 0;
            if (side == BattleSideEnum.Attacker)
            {
                if (_masterFlag.IsFullyRaised && GetFlagOwnerTeam(_masterFlag).Side != BattleSideEnum.Attacker)
                {
                    moraleGain -= MoraleDecayInTick;
                }
                foreach (FlagCapturePoint flag in AllCapturePoints)
                {
                    if (flag == _masterFlag || flag.IsDeactivated || GetFlagOwnerTeam(flag).Side != BattleSideEnum.Attacker)
                        continue;

                    _capturePointRemainingMoraleGains[flag.FlagIndex] -= MoraleGainPerFlag;
                    moraleGain += MoraleGainPerFlag;
                    if (_capturePointRemainingMoraleGains[flag.FlagIndex] != 0)
                        continue;

                    moraleGain += MoraleBoostOnFlagRemoval;
                    UnlockFlag(flag);
                    GainGoldForPlayers();
                    flag.RemovePointAsServer();
                    (SpawnComponent.SpawnFrameBehavior as SiegeSpawnFrameBehavior).OnFlagDeactivated(flag);
                    _gameModeSiegeClient.OnNumberOfFlagsChanged();
                    GameNetwork.BeginBroadcastModuleEvent();
                    GameNetwork.WriteMessage(new FlagDominationFlagsRemovedMessage());
                    GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None);
                    NotificationsComponent.FlagsXRemoved(flag);
                }
            }
            else if (GetFlagOwnerTeam(_masterFlag).Side == BattleSideEnum.Attacker && !_masterFlag.IsContested)
            {
                moraleGain = DefenderMoraleDecayInTick;
            }
            else
            {
                moraleGain++;
            }
            return moraleGain;
        }

        public Team GetFlagOwnerTeam(FlagCapturePoint flag)
        {
            return _capturePointOwners[flag.FlagIndex];
        }

        private bool CheckObjectives(float dt)
        {
            _dtSumObjectiveCheck += dt;
            if (_dtSumObjectiveCheck >= ObjectiveCheckPeriod)
            {
                _dtSumObjectiveCheck -= ObjectiveCheckPeriod;
                return true;
            }
            return false;
        }

        private void TickFlags()
        {
            foreach (FlagCapturePoint flag in AllCapturePoints)
            {
                Team flagOwnerTeam = GetFlagOwnerTeam(flag);
                if (flag.IsDeactivated || flagOwnerTeam == Team.Invalid)
                    continue;

                int attackerCount = 0, defenderCount = 0;
                AgentProximityMap.ProximityMapSearchStruct proximitySearch = AgentProximityMap.BeginSearch(Mission, flag.Position.AsVec2, radius, false);
                for (; proximitySearch.LastFoundAgent != null; AgentProximityMap.FindNext(Mission, ref proximitySearch))
                {
                    Agent lastFoundAgent = proximitySearch.LastFoundAgent;
                    float num2 = lastFoundAgent.Position.DistanceSquared(flag.Position);
                    if (lastFoundAgent.IsMount || !lastFoundAgent.IsActive() || num2 > radius * radius || lastFoundAgent.IsAIControlled)
                        continue;

                    HealInFlagRange(flag, lastFoundAgent);
                    GainGoldInFlagRange(lastFoundAgent);
                    if (lastFoundAgent.Team.IsAttacker) //计算旗帜内双方人数
                        attackerCount++;
                    else if (lastFoundAgent.Team.IsDefender)
                        defenderCount++;
                }

                CaptureTheFlagFlagDirection flagDirection = ComputeFlagDirection(flag, attackerCount, defenderCount, out bool canOwnershipChange, out Team newFlagTeam);
                SetFlagMoveSpeed(flag, flagDirection, attackerCount, defenderCount);

                flag.OnAfterTick(canOwnershipChange, out bool ownerTeamChanged);
                if (ownerTeamChanged)
                {
                    flag.SetTeamColorsSynched(newFlagTeam.Color, newFlagTeam.Color2);
                    _capturePointOwners[flag.FlagIndex] = newFlagTeam;
                    GameNetwork.BeginBroadcastModuleEvent();
                    GameNetwork.WriteMessage(new FlagDominationCapturePointMessage(flag.FlagIndex, newFlagTeam));
                    GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None);
                    _gameModeSiegeClient?.OnCapturePointOwnerChanged(flag, newFlagTeam);
                    NotificationsComponent.FlagXCapturedByTeamX(flag, newFlagTeam);
                }
            }
        }

        private void TickObjectives()
        {
            for (int num = _movingObjectives.Length - 1; num >= 0; num--)
            {
                IMoveableSiegeWeapon item = _movingObjectives[num].Item1;
                if (item != null)
                {
                    SiegeWeapon siegeWeapon = item as SiegeWeapon;
                    if (siegeWeapon.IsDeactivated || siegeWeapon.IsDestroyed || siegeWeapon.IsDisabled)
                    {
                        _movingObjectives[num].Item1 = null;
                    }
                    else if (item.MovementComponent.HasArrivedAtTarget)
                    {
                        _movingObjectives[num].Item1 = null;
                        GameEntity root = siegeWeapon.GameEntity.Root;
                        List<KeyValuePair<MissionPeer, float>> allContributorsForSideAndClear = _objectiveSystem.GetAllContributorsForSideAndClear(root, BattleSideEnum.Attacker);
                        float num2 = allContributorsForSideAndClear.Sum((KeyValuePair<MissionPeer, float> ac) => ac.Value);
                        foreach (KeyValuePair<MissionPeer, float> item3 in allContributorsForSideAndClear)
                        {
                            int goldGainsFromObjectiveAssist = (item3.Key.Representative as SiegeMissionRepresentative).GetGoldGainsFromObjectiveAssist(root, item3.Value / num2, isCompleted: true);
                            if (goldGainsFromObjectiveAssist > 0)
                            {
                                ChangeCurrentGoldForPeer(item3.Key, item3.Key.Representative.Gold + goldGainsFromObjectiveAssist);
                                this.OnObjectiveGoldGained?.Invoke(item3.Key, goldGainsFromObjectiveAssist);
                            }
                        }
                    }
                    else
                    {
                        GameEntity gameEntity = siegeWeapon.GameEntity;
                        Vec3 item2 = _movingObjectives[num].Item2;
                        Vec3 globalPosition = gameEntity.GlobalPosition;
                        float lengthSquared = (globalPosition - item2).LengthSquared;
                        if (lengthSquared > 1f)
                        {
                            _movingObjectives[num].Item2 = globalPosition;
                            foreach (StandingPoint standingPoint in siegeWeapon.StandingPoints)
                            {
                                Agent userAgent = standingPoint.UserAgent;
                                if (userAgent?.MissionPeer != null && userAgent.MissionPeer.Team.Side == siegeWeapon.Side)
                                {
                                    _objectiveSystem.AddContributionForObjective(gameEntity.Root, userAgent.MissionPeer, lengthSquared);
                                }
                            }
                        }
                    }
                }
            }
        }

        private void OnWarmupEnding()
        {
            NotificationsComponent.WarmupEnding();
        }

        public override bool CheckForWarmupEnd() //修正了一个Native错误，现在服务器可以正确比较当前玩家数与 MinNumberOfPlayersForMatchStart
        {
            int num = 0;
            foreach (NetworkCommunicator networkPeer in GameNetwork.NetworkPeers)
            {
                MissionPeer component = networkPeer.GetComponent<MissionPeer>();
                if (networkPeer.IsSynchronized && component?.Team != null && component.Team.Side != BattleSideEnum.None)
                {
                    num++;
                }
            }
            return num >= MultiplayerOptions.OptionType.MinNumberOfPlayersForMatchStart.GetIntValue();
        }

        protected override void HandleEarlyNewClientAfterLoadingFinished(NetworkCommunicator networkPeer)
        {
            networkPeer.AddComponent<SiegeMissionRepresentative>();
        }

        protected override void HandleNewClientAfterSynchronized(NetworkCommunicator networkPeer)
        {
            int num = FirstSpawnGold;
            if (_warmupComponent != null && _warmupComponent.IsInWarmup)
            {
                num = FirstSpawnGoldForEarlyJoin;
            }
            ChangeCurrentGoldForPeer(networkPeer.GetComponent<MissionPeer>(), num);
            _gameModeSiegeClient?.OnGoldAmountChangedForRepresentative(networkPeer.GetComponent<SiegeMissionRepresentative>(), num);
            if (AllCapturePoints == null || networkPeer.IsServerPeer)
            {
                return;
            }
            foreach (FlagCapturePoint flag in AllCapturePoints.Where((FlagCapturePoint cp) => !cp.IsDeactivated))
            {
                GameNetwork.BeginModuleEventAsServer(networkPeer);
                GameNetwork.WriteMessage(new FlagDominationCapturePointMessage(flag.FlagIndex, _capturePointOwners[flag.FlagIndex]));
                GameNetwork.EndModuleEventAsServer();
            }
        }

        public override void OnPeerChangedTeam(NetworkCommunicator peer, Team oldTeam, Team newTeam)
        {
            if (MissionLobbyComponent.CurrentMultiplayerState == MissionLobbyComponent.MultiplayerGameState.Playing && oldTeam != null && oldTeam != newTeam)
            {
                ChangeCurrentGoldForPeer(peer.GetComponent<MissionPeer>(), ChangeTeamGold);
            }
        }

        public override void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow blow)
        {
            if (MissionLobbyComponent.CurrentMultiplayerState != MissionLobbyComponent.MultiplayerGameState.Playing || blow.DamageType == DamageTypes.Invalid || (agentState != AgentState.Unconscious && agentState != AgentState.Killed) || !affectedAgent.IsHuman)
            {
                return;
            }
            MissionPeer missionPeer = affectedAgent.MissionPeer;
            if (missionPeer != null)
            {
                int num = RespawnGold;
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
                    BattleSideEnum battleSideEnum = ((num2 == 0) ? BattleSideEnum.None : ((num2 < 0) ? BattleSideEnum.Attacker : BattleSideEnum.Defender));
                    if (battleSideEnum != BattleSideEnum.None && battleSideEnum == missionPeer.Team.Side)
                    {
                        num2 = TaleWorlds.Library.MathF.Abs(num2);
                        int count = array[(int)battleSideEnum].Count;
                        if (count > 0)
                        {
                            int num3 = num * num2 / 10 / count * 10;
                            num += num3;
                        }
                    }
                }
                ChangeRespawnGold(missionPeer, num);
            }
            bool isFriendly = affectorAgent?.Team != null && affectedAgent.Team != null && affectorAgent.Team.Side == affectedAgent.Team.Side;
            MultiplayerClassDivisions.MPHeroClass mPHeroClassForCharacter = MultiplayerClassDivisions.GetMPHeroClassForCharacter(affectedAgent.Character);
            Agent.Hitter assistingHitter = affectedAgent.GetAssistingHitter(affectorAgent?.MissionPeer);
            if (affectorAgent?.MissionPeer != null && affectorAgent != affectedAgent && affectedAgent.Team != affectorAgent.Team)
            {
                SiegeMissionRepresentative siegeMissionRepresentative = affectorAgent.MissionPeer.Representative as SiegeMissionRepresentative;
                int goldGainsFromKillDataAndUpdateFlags = siegeMissionRepresentative.GetGoldGainsFromKillDataAndUpdateFlags(MPPerkObject.GetPerkHandler(affectorAgent.MissionPeer), MPPerkObject.GetPerkHandler(assistingHitter?.HitterPeer), mPHeroClassForCharacter, isAssist: false, blow.IsMissile, isFriendly);
                ChangeCurrentGoldForPeer(affectorAgent.MissionPeer, siegeMissionRepresentative.Gold + goldGainsFromKillDataAndUpdateFlags);
            }
            if (assistingHitter?.HitterPeer != null && !assistingHitter.IsFriendlyHit)
            {
                SiegeMissionRepresentative siegeMissionRepresentative2 = assistingHitter.HitterPeer.Representative as SiegeMissionRepresentative;
                int goldGainsFromKillDataAndUpdateFlags2 = siegeMissionRepresentative2.GetGoldGainsFromKillDataAndUpdateFlags(MPPerkObject.GetPerkHandler(affectorAgent?.MissionPeer), MPPerkObject.GetPerkHandler(assistingHitter.HitterPeer), mPHeroClassForCharacter, isAssist: true, blow.IsMissile, isFriendly);
                ChangeCurrentGoldForPeer(assistingHitter.HitterPeer, siegeMissionRepresentative2.Gold + goldGainsFromKillDataAndUpdateFlags2);
            }
            if (missionPeer?.Team == null)
            {
                return;
            }
            IEnumerable<(MissionPeer, int)> enumerable = MPPerkObject.GetPerkHandler(missionPeer)?.GetTeamGoldRewardsOnDeath();
            if (enumerable == null)
            {
                return;
            }
            foreach (var (missionPeer2, num4) in enumerable)
            {
                if (num4 > 0 && missionPeer2?.Representative is SiegeMissionRepresentative siegeMissionRepresentative3)
                {
                    int goldGainsFromAllyDeathReward = siegeMissionRepresentative3.GetGoldGainsFromAllyDeathReward(num4);
                    if (goldGainsFromAllyDeathReward > 0)
                    {
                        ChangeCurrentGoldForPeer(missionPeer2, siegeMissionRepresentative3.Gold + goldGainsFromAllyDeathReward);
                    }
                }
            }
        }

        protected override void HandleNewClientAfterLoadingFinished(NetworkCommunicator networkPeer)
        {
            GameNetwork.BeginBroadcastModuleEvent();
            GameNetwork.WriteMessage(new SiegeMoraleChangeMessage(_morales[(int)BattleSideEnum.Attacker], _morales[(int)BattleSideEnum.Defender], _capturePointRemainingMoraleGains));
            GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None);
        }

        public override void OnRemoveBehavior()
        {
            base.OnRemoveBehavior();
            _warmupComponent.OnWarmupEnding -= OnWarmupEnding;
        }

        public override void OnClearScene()
        {
            base.OnClearScene();
            ClearPeerCounts();
            foreach (CastleGate gate in Mission.Current.MissionObjects.FindAllWithType<CastleGate>())
            {
                foreach (StandingPoint standingPoint in gate.StandingPoints)
                {
                    standingPoint.SetIsDeactivatedSynched(false);
                }
            }
        }

        private void InitializeMorales()
        {
            _morales = new int[(int)BattleSideEnum.NumSides];
            for (int i = 0; i < _morales.Length; i++)
            {
                _morales[i] = StartingMorale;
            }
        }

        private void InitializeFlags()
        {
            AllCapturePoints = Mission.Current.MissionObjects.FindAllWithType<FlagCapturePoint>().ToMBList();
            _capturePointOwners = new Team[AllCapturePoints.Count];
            _capturePointRemainingMoraleGains = new int[AllCapturePoints.Count];

            for (int i = 0; i < AllCapturePoints.Count - 1; i++) //依照FlagIndex对AllCapturePoints进行交换排序
            {
                for (int j = i + 1; j < AllCapturePoints.Count; j++)
                {
                    if (AllCapturePoints[i].FlagIndex > AllCapturePoints[j].FlagIndex)
                        (AllCapturePoints[j], AllCapturePoints[i]) = (AllCapturePoints[i], AllCapturePoints[j]);
                }
            }

            foreach (FlagCapturePoint allCapturePoint in AllCapturePoints)
            {
                allCapturePoint.SetTeamColorsSynched(TeammateColorsExtensions.NEUTRAL_COLOR, TeammateColorsExtensions.NEUTRAL_COLOR2);
                _capturePointOwners[allCapturePoint.FlagIndex] = null;
                _capturePointRemainingMoraleGains[allCapturePoint.FlagIndex] = MaxMoraleGainPerFlag;
                if (allCapturePoint.GameEntity.HasTag(MasterFlagTag))
                {
                    _masterFlag = allCapturePoint;
                }
            }
        }

        private void AddTeams()
        {
            BasicCultureObject cultureTeam1 = MBObjectManager.Instance.GetObject<BasicCultureObject>(MultiplayerOptions.OptionType.CultureTeam1.GetStrValue());
            BasicCultureObject cultureTeam2 = MBObjectManager.Instance.GetObject<BasicCultureObject>(MultiplayerOptions.OptionType.CultureTeam2.GetStrValue());
            Banner bannerTeam1 = new Banner(cultureTeam1.BannerKey, cultureTeam1.BackgroundColor1, cultureTeam1.ForegroundColor1);
            Banner bannerTeam2 = new Banner(cultureTeam2.BannerKey, cultureTeam2.BackgroundColor2, cultureTeam2.ForegroundColor2);
            Mission.Teams.Add(BattleSideEnum.Attacker, cultureTeam1.BackgroundColor1, cultureTeam1.ForegroundColor1, bannerTeam1, false, true);
            Mission.Teams.Add(BattleSideEnum.Defender, cultureTeam2.BackgroundColor2, cultureTeam2.ForegroundColor2, bannerTeam2, false, true);
        }

        private void InitialLockFlags(MBReadOnlyList<FlagCapturePoint> AllCapturePoints, int FlagLockNum)
        {
            foreach (FlagCapturePoint flag in AllCapturePoints)
            {
                if (flag.FlagIndex >= FlagLockNum) //开局启用FlagLockNum个旗帜
                {
                    _capturePointOwners[flag.FlagIndex] = Team.Invalid;
                    flag.SetTeamColors(TeammateColorsExtensions.NEUTRAL_COLOR, TeammateColorsExtensions.NEUTRAL_COLOR2);
                    _gameModeSiegeClient?.OnCapturePointOwnerChanged(flag, Team.Invalid);
                }
                else
                {
                    _capturePointOwners[flag.FlagIndex] = Mission.Teams.Defender;
                    flag.SetTeamColors(Mission.Teams.Defender.Color, Mission.Teams.Defender.Color2);
                    _gameModeSiegeClient?.OnCapturePointOwnerChanged(flag, Mission.Teams.Defender);
                }
            }
        }

        private void UnlockFlag(FlagCapturePoint flag) //旗帜移除后解锁后续旗帜
        {
            for (int i = flag.FlagIndex + 1; i < AllCapturePoints.Count; i++)
            {
                if (GetFlagOwnerTeam(AllCapturePoints[i]) == Team.Invalid)
                {
                    AllCapturePoints[i].SetTeamColorsSynched(Mission.DefenderTeam.Color, Mission.DefenderTeam.Color2);
                    _capturePointOwners[i] = Mission.DefenderTeam;
                    GameNetwork.BeginBroadcastModuleEvent();
                    GameNetwork.WriteMessage(new FlagDominationCapturePointMessage(i, Mission.DefenderTeam));
                    GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None);
                    _gameModeSiegeClient?.OnCapturePointOwnerChanged(AllCapturePoints[i], Mission.DefenderTeam);
                    break;
                }
            }
        }

        private void GainGoldForPlayers() //移除旗帜给予玩家金币奖励
        {
            foreach (NetworkCommunicator networkPeer in GameNetwork.NetworkPeers)
            {
                MissionPeer component = networkPeer.GetComponent<MissionPeer>();
                if (component?.Team?.Side == BattleSideEnum.Defender)
                {
                    ChangeCurrentGoldForPeer(component, GetCurrentGoldForPeer(component) + DefenderGoldBonusOnFlagRemoval);
                }
                else if (component?.Team?.Side == BattleSideEnum.Attacker)
                    ChangeCurrentGoldForPeer(component, GetCurrentGoldForPeer(component) + AttackerGoldBonusOnFlagRemoval);
            }
        }

        private void HealInFlagRange(FlagCapturePoint flag, Agent agent) //旗帜范围回血
        {
            if (flag.IsFullyRaised && agent.Health < agent.HealthLimit && agent.Team == GetFlagOwnerTeam(flag) && (_dtSumCheckMorales % HealTick < ObjectiveCheckPeriod))
            {
                agent.Health = Math.Min(agent.Health + HealGain, agent.HealthLimit);
            }
        }

        private void GainGoldInFlagRange(Agent agent) //旗帜范围获取金币
        {
            if ((agent.MissionPeer.Representative.Gold < AttackerFlagGoldHoldMax && agent.Team.IsAttacker && _dtSumCheckMorales % AttackerFlagGoldTick < ObjectiveCheckPeriod) || (agent.MissionPeer.Representative.Gold < DefenderFlagGoldHoldMax && agent.Team.IsDefender && _dtSumCheckMorales % DefenderFlagGoldTick < ObjectiveCheckPeriod))
            {
                List<KeyValuePair<ushort, int>> list = new List<KeyValuePair<ushort, int>>();
                ChangeCurrentGoldForPeer(agent.MissionPeer, agent.MissionPeer.Representative.Gold + FlagGoldGain);//设定占旗获取金币数
                list.Add(new KeyValuePair<ushort, int>(512, FlagGoldGain));
                GameNetwork.BeginModuleEventAsServer(agent.MissionPeer.Peer);
                GameNetwork.WriteMessage(new GoldGain(list));
                GameNetwork.EndModuleEventAsServer();
                list.Clear();
            }
        }

        private CaptureTheFlagFlagDirection ComputeFlagDirection(FlagCapturePoint flag, int attackerCount, int defenderCount, out bool canOwnershipChange, out Team newFlagTeam)
        {
            canOwnershipChange = false;
            newFlagTeam = null;
            Team flagOwnerTeam = GetFlagOwnerTeam(flag);
            if (flagOwnerTeam == Mission.DefenderTeam)
            {
                if (attackerCount > defenderCount)
                {
                    canOwnershipChange = true;
                    newFlagTeam = Mission.AttackerTeam;
                    return CaptureTheFlagFlagDirection.Down;
                }
                else if (flag.IsContested && defenderCount >= attackerCount)
                    return CaptureTheFlagFlagDirection.Up;
            }
            else if (flagOwnerTeam == Mission.AttackerTeam)
            {
                if (defenderCount > attackerCount)
                {
                    canOwnershipChange = true;
                    newFlagTeam = Mission.DefenderTeam;
                    return CaptureTheFlagFlagDirection.Down;
                }
                else if (flag.IsContested && attackerCount >= defenderCount)
                    return CaptureTheFlagFlagDirection.Up;
            }
            return CaptureTheFlagFlagDirection.None;
        }

        private void SetFlagMoveSpeed(FlagCapturePoint flag, CaptureTheFlagFlagDirection flagDirection, int attackerCount, int defenderCount)
        {
            if (flagDirection != CaptureTheFlagFlagDirection.None)
            {
                float flagv = MathF.Abs(attackerCount - defenderCount) * FlagSpeedPerPlayer; //定义旗帜升降速度
                flag.SetMoveFlag(flagDirection, MBMath.ClampFloat(flagv, FlagSpeedMin, FlagSpeedMax));
            }
        }

        private void ChangeRespawnGold(MissionPeer missionPeer, int num)
        {
            if ((missionPeer.Team.Side == BattleSideEnum.Defender && missionPeer.Representative.Gold > DefenderFlagGoldHoldMax) ||
                (missionPeer.Team.Side == BattleSideEnum.Attacker && missionPeer.Representative.Gold > AttackerFlagGoldHoldMax))
            {
                ChangeCurrentGoldForPeer(missionPeer, missionPeer.Representative.Gold + num);
            }
            else if (missionPeer.Team.Side == BattleSideEnum.Attacker)
            {
                ChangeCurrentGoldForPeer(missionPeer, MBMath.ClampInt(missionPeer.Representative.Gold + num + AttackerRespawnGold, num, num + AttackerFlagGoldHoldMax));
            }
            else if (missionPeer.Team.Side == BattleSideEnum.Defender)
            {
                ChangeCurrentGoldForPeer(missionPeer, MBMath.ClampInt(missionPeer.Representative.Gold + num + DefenderRespawnGold, num, num + DefenderFlagGoldHoldMax));
            }
        }
    }
}