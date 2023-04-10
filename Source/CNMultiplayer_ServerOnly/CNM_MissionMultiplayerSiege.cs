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

namespace CNMultiplayer
{
    public class CNM_MissionMultiplayerSiege : MissionMultiplayerGameModeBase, IAnalyticsFlagInfo, IMissionBehavior
    {
        public delegate void OnDestructableComponentDestroyedDelegate(DestructableComponent destructableComponent, ScriptComponentBehavior attackerScriptComponentBehaviour, MissionPeer[] contributors);

        public delegate void OnObjectiveGoldGainedDelegate(MissionPeer peer, int goldGain);

        public const int NumberOfFlagsInGame = 7;

        public const int NumberOfFlagsAffectingMoraleInGame = 6;

        public const int MaxMorale = 1440;

        public const int StartingMorale = 360; //双方初始士气

        public const int MaxMoraleGainPerFlag = 90;

        public const int MoraleGainPerFlag = 1;

        public const int GoldBonusOnFlagRemoval = 35; //攻城方移除旗帜金币奖励

        public const int DefenderGoldBonusOnFlagRemoval = 150; //守城方移除旗帜金币补偿

        public const string MasterFlagTag = "keep_capture_point";

        public override bool IsGameModeHidingAllAgentVisuals => true;

        public override bool IsGameModeUsingOpposingTeams => true;

        public MBReadOnlyList<FlagCapturePoint> AllCapturePoints { get; private set; }

        public event OnDestructableComponentDestroyedDelegate OnDestructableComponentDestroyed;

        public event OnObjectiveGoldGainedDelegate OnObjectiveGoldGained;

        private const int FirstSpawnGold = 180; //初始金币

        private const int FirstSpawnGoldForEarlyJoin = 180; //初始金币

        private const int ChangeTeamGold = 150; //换边金币

        private const int RespawnGold = 100; //基础重生金币

        private const int AttackerRespawnGoldLowest = 150; //攻城方保底金币

        private const int DefenderRespawnGoldLowest = 125; //守城方保底金币

        private const int AttackerFlagGoldHoldMax = 250; //攻城方持有旗帜金币最大值

        private const int DefenderFlagGoldHoldMax = 150; //守城方持有旗帜金币最大值

        private const float radius = 20f; //定义旗帜半径

        private const float ObjectiveCheckPeriod = 0.25f;

        private const float MoraleTickTimeInSeconds = 3f; //士气Tick

        private const int MoraleBoostOnFlagRemoval = 0; //攻城方移除旗帜的士气奖励

        private const int MoraleDecayInTick = -1; //攻城方基础士气衰减

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
            _capturePointOwners = new Team[NumberOfFlagsInGame];
            _capturePointRemainingMoraleGains = new int[NumberOfFlagsInGame];
            _morales = new int[2];
            _morales[1] = StartingMorale;
            _morales[0] = StartingMorale;
            AllCapturePoints = Mission.Current.MissionObjects.FindAllWithType<FlagCapturePoint>().ToMBList();
            for (int i = 0; i < AllCapturePoints.Count - 1; i++)//依照FlagIndex对AllCapturePoints进行交换排序
            {
                for (int j = i + 1; j < AllCapturePoints.Count; j++)
                {
                    if (AllCapturePoints[i].FlagIndex > AllCapturePoints[j].FlagIndex)
                    {
                        var temp = AllCapturePoints[i];
                        AllCapturePoints[i] = AllCapturePoints[j];
                        AllCapturePoints[j] = temp;
                    }
                }
            }
            foreach (FlagCapturePoint allCapturePoint in AllCapturePoints)
            {
                allCapturePoint.SetTeamColorsSynched(4284111450u, uint.MaxValue);
                _capturePointOwners[allCapturePoint.FlagIndex] = null;
                _capturePointRemainingMoraleGains[allCapturePoint.FlagIndex] = MaxMoraleGainPerFlag;
                if (allCapturePoint.GameEntity.HasTag(MasterFlagTag))
                {
                    _masterFlag = allCapturePoint;
                }
            }
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
        {
            return MissionLobbyComponent.MultiplayerGameType.Siege;
        }

        public override bool UseRoundController()
        {
            return false;
        }

        public override void AfterStart()
        {
            BasicCultureObject @object = MBObjectManager.Instance.GetObject<BasicCultureObject>(MultiplayerOptions.OptionType.CultureTeam1.GetStrValue());
            BasicCultureObject object2 = MBObjectManager.Instance.GetObject<BasicCultureObject>(MultiplayerOptions.OptionType.CultureTeam2.GetStrValue());
            Banner banner = new Banner(@object.BannerKey, @object.BackgroundColor1, @object.ForegroundColor1);
            Banner banner2 = new Banner(object2.BannerKey, object2.BackgroundColor2, object2.ForegroundColor2);
            base.Mission.Teams.Add(BattleSideEnum.Attacker, @object.BackgroundColor1, @object.ForegroundColor1, banner);
            base.Mission.Teams.Add(BattleSideEnum.Defender, object2.BackgroundColor2, object2.ForegroundColor2, banner2);
            foreach (FlagCapturePoint allCapturePoint in AllCapturePoints)
            {
                if (allCapturePoint.FlagIndex >= FlagLockNum) //开局启用FlagLockNum个旗帜
                {
                    _capturePointOwners[allCapturePoint.FlagIndex] = Team.Invalid;
                    allCapturePoint.SetTeamColors(4284111450U, uint.MaxValue);
                    _gameModeSiegeClient?.OnCapturePointOwnerChanged(allCapturePoint, Team.Invalid);
                }
                else
                {
                    _capturePointOwners[allCapturePoint.FlagIndex] = base.Mission.Teams.Defender;
                    allCapturePoint.SetTeamColors(base.Mission.Teams.Defender.Color, base.Mission.Teams.Defender.Color2);
                    _gameModeSiegeClient?.OnCapturePointOwnerChanged(allCapturePoint, base.Mission.Teams.Defender);
                }
            }
            _warmupComponent.OnWarmupEnding += OnWarmupEnding;
        }

        public override void OnMissionTick(float dt)
        {
            base.OnMissionTick(dt);
            if (!_firstTickDone)
            {
                foreach (CastleGate item in Mission.Current.MissionObjects.FindAllWithType<CastleGate>())
                {
                    item.OpenDoor();
                    foreach (StandingPoint standingPoint in item.StandingPoints)
                    {
                        standingPoint.SetIsDeactivatedSynched(value: true);
                    }
                }
                _firstTickDone = true;
            }
            if (MissionLobbyComponent.CurrentMultiplayerState == MissionLobbyComponent.MultiplayerGameState.Playing && !WarmupComponent.IsInWarmup)
            {
                CheckMorales(dt);
                if (CheckObjectives(dt))
                {
                    TickFlags(dt);
                    TickObjectives(dt);
                }
            }
        }

        private void CheckMorales(float dt)
        {
            _dtSumCheckMorales += dt;
            if (_dtSumCheckMorales >= MoraleTickTimeInSeconds)
            {
                _dtSumCheckMorales -= MoraleTickTimeInSeconds;
                int num = TaleWorlds.Library.MathF.Max(_morales[1] + GetMoraleGain(BattleSideEnum.Attacker), 0);
                int num2 = MBMath.ClampInt(_morales[0] + GetMoraleGain(BattleSideEnum.Defender), 0, 360);
                GameNetwork.BeginBroadcastModuleEvent();
                GameNetwork.WriteMessage(new SiegeMoraleChangeMessage(num, num2, _capturePointRemainingMoraleGains));
                GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None);
                _gameModeSiegeClient?.OnMoraleChanged(num, num2, _capturePointRemainingMoraleGains);
                _morales[1] = num;
                _morales[0] = num2;
            }
        }

        public override bool CheckForMatchEnd()
        {
            return _morales.Any((int morale) => morale == 0);
        }

        public override Team GetWinnerTeam()
        {
            Team team = null;
            if (_morales[1] <= 0 && _morales[0] > 0)
            {
                team = base.Mission.Teams.Defender;
            }
            if (_morales[0] <= 0 && _morales[1] > 0)
            {
                team = base.Mission.Teams.Attacker;
            }
            team = team ?? base.Mission.Teams.Defender;
            base.Mission.GetMissionBehavior<MissionScoreboardComponent>().ChangeTeamScore(team, 1);
            return team;
        }

        private int GetMoraleGain(BattleSideEnum side)
        {
            int num = 0;
            int FlagInvalidNum = 0;
            for (int i = 0; i < AllCapturePoints.Count; i++)//计算当前战场上的剩余中立旗帜数量
            {
                if (!AllCapturePoints[i].IsDeactivated && GetFlagOwnerTeam(AllCapturePoints[i]) == Team.Invalid)
                {
                    FlagInvalidNum++;
                }
            }
            List<KeyValuePair<ushort, int>> list = new List<KeyValuePair<ushort, int>>();
            if (side == BattleSideEnum.Attacker)
            {
                if (_masterFlag.IsFullyRaised && GetFlagOwnerTeam(_masterFlag).Side != BattleSideEnum.Attacker)
                {
                    num += MoraleDecayInTick;
                }
                foreach (FlagCapturePoint item in AllCapturePoints.Where((FlagCapturePoint flag) => flag != _masterFlag && !flag.IsDeactivated && GetFlagOwnerTeam(flag).Side == BattleSideEnum.Attacker))
                {
                    _capturePointRemainingMoraleGains[item.FlagIndex]--;
                    num += MoraleGainPerFlag;
                    if (_capturePointRemainingMoraleGains[item.FlagIndex] != 0)
                    {
                        continue;
                    }
                    num += MoraleBoostOnFlagRemoval;
                    if (FlagInvalidNum != 0)//旗帜移除后解锁后续旗帜
                    {
                        for (int i = item.FlagIndex+1; i < AllCapturePoints.Count; i++)
                        {
                            if (GetFlagOwnerTeam(AllCapturePoints[i]) == Team.Invalid)
                            {
                                AllCapturePoints[i].SetTeamColorsSynched(Mission.Current.DefenderTeam.Color, Mission.Current.DefenderTeam.Color2);
                                _capturePointOwners[i] = Mission.Current.DefenderTeam;
                                GameNetwork.BeginBroadcastModuleEvent();
                                GameNetwork.WriteMessage(new FlagDominationCapturePointMessage(i, Mission.Current.DefenderTeam));
                                GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None);
                                _gameModeSiegeClient?.OnCapturePointOwnerChanged(AllCapturePoints[i], Mission.Current.DefenderTeam);
                                break;
                            }
                        }
                    }
                    foreach (NetworkCommunicator networkPeer in GameNetwork.NetworkPeers)
                    {
                        MissionPeer component = networkPeer.GetComponent<MissionPeer>();
                        if (component != null && component.Team?.Side == BattleSideEnum.Defender)
                        {
                            ChangeCurrentGoldForPeer(component, GetCurrentGoldForPeer(component) + DefenderGoldBonusOnFlagRemoval);//移除旗帜的金币补偿(防守方)
                            list.Add(new KeyValuePair<ushort, int>(512, DefenderGoldBonusOnFlagRemoval));
                            if (!component.Peer.Communicator.IsServerPeer && component.Peer.Communicator.IsConnectionActive)
                            {
                                GameNetwork.BeginModuleEventAsServer(component.Peer);
                                GameNetwork.WriteMessage(new GoldGain(list));
                                GameNetwork.EndModuleEventAsServer();
                            }
                            list.Clear();
                        }
                        else if (component != null && component.Team?.Side == BattleSideEnum.Attacker)
                            ChangeCurrentGoldForPeer(component, GetCurrentGoldForPeer(component) + GoldBonusOnFlagRemoval);//移除旗帜的金币数(进攻方)
                    }
                    item.RemovePointAsServer();
                    (SpawnComponent.SpawnFrameBehavior as SiegeSpawnFrameBehavior).OnFlagDeactivated(item);
                    _gameModeSiegeClient.OnNumberOfFlagsChanged();
                    GameNetwork.BeginBroadcastModuleEvent();
                    GameNetwork.WriteMessage(new FlagDominationFlagsRemovedMessage());
                    GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None);
                    NotificationsComponent.FlagsXRemoved(item);
                }
                return num;
            }

            if (GetFlagOwnerTeam(_masterFlag).Side == BattleSideEnum.Attacker && !_masterFlag.IsContested)
            {
                num = DefenderMoraleDecayInTick;
            }
            else
            {
                num++;
            }
            return num;
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

        private void TickFlags(float dt)
        {
            foreach (FlagCapturePoint flagCapturePoint in AllCapturePoints)
            {
                Team flagOwnerTeam = GetFlagOwnerTeam(flagCapturePoint);
                if (!flagCapturePoint.IsDeactivated && flagOwnerTeam != Team.Invalid)
                {
                    Agent agent = null;
                    float num = float.MaxValue;
                    int count1 = 0, count2 = 0;
                    List<KeyValuePair<ushort, int>> list = new List<KeyValuePair<ushort, int>>();
                    AgentProximityMap.ProximityMapSearchStruct proximityMapSearchStruct = AgentProximityMap.BeginSearch(Mission.Current, flagCapturePoint.Position.AsVec2, radius, false);
                    while (proximityMapSearchStruct.LastFoundAgent != null)
                    {
                        Agent lastFoundAgent = proximityMapSearchStruct.LastFoundAgent;
                        float num2 = lastFoundAgent.Position.DistanceSquared(flagCapturePoint.Position);
                        if (!lastFoundAgent.IsMount && lastFoundAgent.IsActive() && num2 <= radius * radius && !lastFoundAgent.IsAIControlled)
                        {
                            if (flagCapturePoint.IsFullyRaised && lastFoundAgent.Team == flagOwnerTeam && (_dtSumCheckMorales % 1f < ObjectiveCheckPeriod))//设定占旗回血速率
                            {
                                lastFoundAgent.Health = Math.Min(lastFoundAgent.Health + 1f, lastFoundAgent.HealthLimit);//设定占旗回血量
                            }

                            if (((lastFoundAgent.MissionPeer.Representative.Gold < AttackerFlagGoldHoldMax && lastFoundAgent.Team.IsAttacker) || (lastFoundAgent.MissionPeer.Representative.Gold < DefenderFlagGoldHoldMax && lastFoundAgent.Team.IsDefender)) && (_dtSumCheckMorales % 0.66f < ObjectiveCheckPeriod))//设定占旗获取金币速率
                            {
                                ChangeCurrentGoldForPeer(lastFoundAgent.MissionPeer, lastFoundAgent.MissionPeer.Representative.Gold + 1);//设定占旗获取金币数
                                list.Add(new KeyValuePair<ushort, int>(512, 1));
                                if (!lastFoundAgent.MissionPeer.Peer.Communicator.IsServerPeer && lastFoundAgent.MissionPeer.Peer.Communicator.IsConnectionActive)
                                {
                                    GameNetwork.BeginModuleEventAsServer(lastFoundAgent.MissionPeer.Peer);
                                    GameNetwork.WriteMessage(new GoldGain(list));
                                    GameNetwork.EndModuleEventAsServer();
                                }
                                list.Clear();
                            }
                            if (lastFoundAgent.Team.IsAttacker)//计算旗帜内双方人数
                                count1++;
                            else if (lastFoundAgent.Team.IsDefender)
                                count2++;
                            if (num2 < num)
                            {
                                agent = lastFoundAgent;
                                num = num2;
                            }
                        }
                        AgentProximityMap.FindNext(Mission.Current, ref proximityMapSearchStruct);
                    }
                    CaptureTheFlagFlagDirection captureTheFlagFlagDirection = CaptureTheFlagFlagDirection.None;
                    bool isContested = flagCapturePoint.IsContested;
                    if ((count1 != 0 || count2 != 0) && ((flagOwnerTeam.IsDefender && count1 > count2) || (flagOwnerTeam.IsAttacker && count2 > count1)))//旗帜升降逻辑
                        captureTheFlagFlagDirection = CaptureTheFlagFlagDirection.Down;
                    if ((!flagCapturePoint.IsFullyRaised && count1 == 0 && count2 == 0) || (isContested && ((flagOwnerTeam.IsDefender && count2 >= count1) || (flagOwnerTeam.IsAttacker && count1 >= count2))))
                        captureTheFlagFlagDirection = CaptureTheFlagFlagDirection.Up;
                    if (captureTheFlagFlagDirection != CaptureTheFlagFlagDirection.None)
                    {
                        float flagv = MathF.Abs(count1 - count2) * 0.15f;//定义旗帜升降速度
                        flagCapturePoint.SetMoveFlag(captureTheFlagFlagDirection, MBMath.ClampFloat(flagv, 0.1f, 1f));
                    }
                    flagCapturePoint.OnAfterTick(agent != null, out var ownerTeamChanged);
                    if (ownerTeamChanged)
                    {
                        Team team = agent.Team;
                        uint color = (uint)(((int?)team?.Color) ?? (-10855846));
                        uint color2 = (uint)(((int?)team?.Color2) ?? (-1));
                        flagCapturePoint.SetTeamColorsSynched(color, color2);
                        _capturePointOwners[flagCapturePoint.FlagIndex] = team;
                        GameNetwork.BeginBroadcastModuleEvent();
                        GameNetwork.WriteMessage(new FlagDominationCapturePointMessage(flagCapturePoint.FlagIndex, team));
                        GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None);
                        _gameModeSiegeClient?.OnCapturePointOwnerChanged(flagCapturePoint, team);
                        NotificationsComponent.FlagXCapturedByTeamX(flagCapturePoint, agent.Team);
                    }
                }
            }
        }

        private void TickObjectives(float dt)
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

        public override bool CheckForWarmupEnd()
        {
            int[] array = new int[2];
            foreach (NetworkCommunicator networkPeer in GameNetwork.NetworkPeers)
            {
                MissionPeer component = networkPeer.GetComponent<MissionPeer>();
                if (networkPeer.IsSynchronized && component?.Team != null && component.Team.Side != BattleSideEnum.None)
                {
                    array[(int)component.Team.Side]++;
                }
            }
            return array.Sum() >= MultiplayerOptions.OptionType.MaxNumberOfPlayers.GetIntValue();
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
            foreach (FlagCapturePoint item in AllCapturePoints.Where((FlagCapturePoint cp) => !cp.IsDeactivated))
            {
                GameNetwork.BeginModuleEventAsServer(networkPeer);
                GameNetwork.WriteMessage(new FlagDominationCapturePointMessage(item.FlagIndex, _capturePointOwners[item.FlagIndex]));
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
                if ((missionPeer.Team.Side == BattleSideEnum.Defender && missionPeer.Representative.Gold > DefenderRespawnGoldLowest - num) || (missionPeer.Team.Side == BattleSideEnum.Attacker && missionPeer.Representative.Gold > AttackerRespawnGoldLowest - num))
                {
                    ChangeCurrentGoldForPeer(missionPeer, missionPeer.Representative.Gold + num);
                }
                else if (missionPeer.Team.Side == BattleSideEnum.Attacker)
                {
                    ChangeCurrentGoldForPeer(missionPeer, AttackerRespawnGoldLowest);
                }
                else if (missionPeer.Team.Side == BattleSideEnum.Defender)
                {
                    ChangeCurrentGoldForPeer(missionPeer, DefenderRespawnGoldLowest);
                }
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
            GameNetwork.WriteMessage(new SiegeMoraleChangeMessage(_morales[1], _morales[0], _capturePointRemainingMoraleGains));
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
            foreach (CastleGate item in Mission.Current.MissionObjects.FindAllWithType<CastleGate>())
            {
                foreach (StandingPoint standingPoint in item.StandingPoints)
                {
                    standingPoint.SetIsDeactivatedSynched(value: false);
                }
            }
        }
    }
}