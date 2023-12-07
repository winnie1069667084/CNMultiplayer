using NetworkMessages.FromServer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.MissionRepresentatives;
using TaleWorlds.MountAndBlade.Network.Messages;
using TaleWorlds.MountAndBlade.Objects;
using TaleWorlds.ObjectSystem;

namespace CNMultiplayer.Common.Modes.CNMSiege
{
    public class CNMSiegeClient : MissionMultiplayerGameModeBaseClient, ICommanderInfo, IMissionBehavior
    {
        public override bool IsGameModeUsingGold => true;

        public override bool IsGameModeTactical => true;

        public override bool IsGameModeUsingRoundCountdown => true;

        public override MultiplayerGameType GameType => MultiplayerGameType.Siege;

        public event Action<BattleSideEnum, float> OnMoraleChangedEvent;

        public event Action OnFlagNumberChangedEvent;

        public event Action<FlagCapturePoint, Team> OnCapturePointOwnerChangedEvent;

        public event Action<GoldGain> OnGoldGainEvent;

        public event Action<int[]> OnCapturePointRemainingMoraleGainsChangedEvent;

        public bool AreMoralesIndependent => true;

        public IEnumerable<FlagCapturePoint> AllCapturePoints { get; private set; }

        protected override void AddRemoveMessageHandlers(GameNetwork.NetworkMessageHandlerRegistererContainer registerer)
        {
            if (GameNetwork.IsClient)
            {
                registerer.RegisterBaseHandler<SiegeMoraleChangeMessage>(new GameNetworkMessage.ServerMessageHandlerDelegate<GameNetworkMessage>(this.HandleMoraleChangedMessage));
                registerer.RegisterBaseHandler<SyncGoldsForSkirmish>(new GameNetworkMessage.ServerMessageHandlerDelegate<GameNetworkMessage>(this.HandleServerEventUpdateGold));
                registerer.RegisterBaseHandler<FlagDominationFlagsRemovedMessage>(new GameNetworkMessage.ServerMessageHandlerDelegate<GameNetworkMessage>(this.HandleFlagsRemovedMessage));
                registerer.RegisterBaseHandler<FlagDominationCapturePointMessage>(new GameNetworkMessage.ServerMessageHandlerDelegate<GameNetworkMessage>(this.HandleServerEventPointCapturedMessage));
                registerer.RegisterBaseHandler<GoldGain>(new GameNetworkMessage.ServerMessageHandlerDelegate<GameNetworkMessage>(this.HandleServerEventTDMGoldGain));
            }
        }

        public override void OnBehaviorInitialize()
        {
            base.OnBehaviorInitialize();
            base.MissionNetworkComponent.OnMyClientSynchronized += this.OnMyClientSynchronized;
            this._capturePointOwners = new Team[7];
            this.AllCapturePoints = Mission.Current.MissionObjects.FindAllWithType<FlagCapturePoint>();
        }

        public override void AfterStart()
        {
            base.Mission.SetMissionMode(MissionMode.Battle, true);
            foreach (FlagCapturePoint flagCapturePoint in this.AllCapturePoints)
            {
                if (flagCapturePoint.GameEntity.HasTag("keep_capture_point"))
                {
                    this._masterFlag = flagCapturePoint;
                }
                else if (flagCapturePoint.FlagIndex == 0)
                {
                    MatrixFrame globalFrame = flagCapturePoint.GameEntity.GetGlobalFrame();
                    this._retreatHornPosition = globalFrame.origin + globalFrame.rotation.u * 3f;
                }
            }
        }

        private void OnMyClientSynchronized()
        {
            this._myRepresentative = GameNetwork.MyPeer.GetComponent<CNMSiegeMissionRepresentative>();
        }

        public override int GetGoldAmount()
        {
            return this._myRepresentative.Gold;
        }

        public override void OnGoldAmountChangedForRepresentative(MissionRepresentativeBase representative, int goldAmount)
        {
            if (representative != null && base.MissionLobbyComponent.CurrentMultiplayerState != MissionLobbyComponent.MultiplayerGameState.Ending)
            {
                representative.UpdateGold(goldAmount);
                base.ScoreboardComponent.PlayerPropertiesChanged(representative.MissionPeer);
            }
        }

        public void OnNumberOfFlagsChanged()
        {
            Action onFlagNumberChangedEvent = this.OnFlagNumberChangedEvent;
            if (onFlagNumberChangedEvent != null)
            {
                onFlagNumberChangedEvent();
            }
            CNMSiegeMissionRepresentative myRepresentative = this._myRepresentative;
            bool flag;
            if (myRepresentative == null)
            {
                flag = false;
            }
            else
            {
                Team team = myRepresentative.MissionPeer.Team;
                BattleSideEnum? battleSideEnum = ((team != null) ? new BattleSideEnum?(team.Side) : null);
                BattleSideEnum battleSideEnum2 = BattleSideEnum.Attacker;
                flag = (battleSideEnum.GetValueOrDefault() == battleSideEnum2) & (battleSideEnum != null);
            }
            if (flag)
            {
                Action<GoldGain> onGoldGainEvent = this.OnGoldGainEvent;
                if (onGoldGainEvent == null)
                {
                    return;
                }
                onGoldGainEvent(new GoldGain(new List<KeyValuePair<ushort, int>>
                {
                    new KeyValuePair<ushort, int>(512, 35)
                }));
            }
        }

        public void OnCapturePointOwnerChanged(FlagCapturePoint flagCapturePoint, Team ownerTeam)
        {
            this._capturePointOwners[flagCapturePoint.FlagIndex] = ownerTeam;
            Action<FlagCapturePoint, Team> onCapturePointOwnerChangedEvent = this.OnCapturePointOwnerChangedEvent;
            if (onCapturePointOwnerChangedEvent != null)
            {
                onCapturePointOwnerChangedEvent(flagCapturePoint, ownerTeam);
            }
            if (ownerTeam != null && ownerTeam.Side == BattleSideEnum.Defender && this._remainingTimeForBellSoundToStop > 8f && flagCapturePoint == this._masterFlag)
            {
                this._bellSoundEvent.Stop();
                this._bellSoundEvent = null;
                this._remainingTimeForBellSoundToStop = float.MinValue;
                this._lastBellSoundPercentage += DefenderMoraleDropThresholdIncrement;
            }
            if (this._myRepresentative != null && this._myRepresentative.MissionPeer.Team != null)
            {
                MatrixFrame cameraFrame = Mission.Current.GetCameraFrame();
                Vec3 vec = cameraFrame.origin + cameraFrame.rotation.u;
                if (this._myRepresentative.MissionPeer.Team == ownerTeam)
                {
                    MBSoundEvent.PlaySound(SoundEvent.GetEventIdFromString("event:/alerts/report/flag_captured"), vec);
                    return;
                }
                MBSoundEvent.PlaySound(SoundEvent.GetEventIdFromString("event:/alerts/report/flag_lost"), vec);
            }
        }

        public void OnMoraleChanged(int attackerMorale, int defenderMorale, int[] capturePointRemainingMoraleGains)
        {
            float num = (float)attackerMorale / 360f;
            float num2 = (float)defenderMorale / 360f;
            CNMSiegeMissionRepresentative myRepresentative = this._myRepresentative;
            if (((myRepresentative != null) ? myRepresentative.MissionPeer.Team : null) != null && this._myRepresentative.MissionPeer.Team.Side != BattleSideEnum.None)
            {
                if ((this._capturePointOwners[this._masterFlag.FlagIndex] == null || this._capturePointOwners[this._masterFlag.FlagIndex].Side != BattleSideEnum.Defender) && this._remainingTimeForBellSoundToStop < 0f)
                {
                    if (num2 > this._lastBellSoundPercentage)
                    {
                        this._lastBellSoundPercentage += DefenderMoraleDropThresholdIncrement;
                    }
                    if (num2 <= DefenderMoraleDropThresholdLow)
                    {
                        if (this._lastBellSoundPercentage > DefenderMoraleDropThresholdLow)
                        {
                            this._remainingTimeForBellSoundToStop = float.MaxValue;
                            this._lastBellSoundPercentage = DefenderMoraleDropThresholdLow;
                        }
                    }
                    else if (num2 <= DefenderMoraleDropThresholdMedium)
                    {
                        if (this._lastBellSoundPercentage > DefenderMoraleDropThresholdMedium)
                        {
                            this._remainingTimeForBellSoundToStop = 8f;
                            this._lastBellSoundPercentage = DefenderMoraleDropThresholdMedium;
                        }
                    }
                    else if (num2 <= DefenderMoraleDropThresholdHigh && this._lastBellSoundPercentage > DefenderMoraleDropThresholdHigh)
                    {
                        this._remainingTimeForBellSoundToStop = DefenderMoraleDropHighDuration;
                        this._lastBellSoundPercentage = DefenderMoraleDropThresholdHigh;
                    }
                    if (this._remainingTimeForBellSoundToStop > 0f)
                    {
                        BattleSideEnum side = this._myRepresentative.MissionPeer.Team.Side;
                        if (side != BattleSideEnum.Defender)
                        {
                            if (side == BattleSideEnum.Attacker)
                            {
                                this._bellSoundEvent = SoundEvent.CreateEventFromString("event:/multiplayer/warning_bells_attacker", base.Mission.Scene);
                            }
                        }
                        else
                        {
                            this._bellSoundEvent = SoundEvent.CreateEventFromString("event:/multiplayer/warning_bells_defender", base.Mission.Scene);
                        }
                        MatrixFrame globalFrame = this._masterFlag.GameEntity.GetGlobalFrame();
                        this._bellSoundEvent.PlayInPosition(globalFrame.origin + globalFrame.rotation.u * 3f);
                    }
                }
                if (!this._battleEndingNotificationGiven || !this._battleEndingLateNotificationGiven)
                {
                    float num3 = ((!this._battleEndingNotificationGiven) ? BattleWinLoseAlertThreshold : BattleWinLoseLateAlertThreshold);
                    MatrixFrame cameraFrame = Mission.Current.GetCameraFrame();
                    Vec3 vec = cameraFrame.origin + cameraFrame.rotation.u;
                    if (num <= num3 && num2 > num3)
                    {
                        MBSoundEvent.PlaySound(SoundEvent.GetEventIdFromString((this._myRepresentative.MissionPeer.Team.Side == BattleSideEnum.Attacker) ? BattleLosingSoundEventString : BattleWinningSoundEventString), vec);
                        if (this._myRepresentative.MissionPeer.Team.Side == BattleSideEnum.Attacker)
                        {
                            MBSoundEvent.PlaySound(SoundEvent.GetEventIdFromString("event:/multiplayer/retreat_horn_attacker"), this._retreatHornPosition);
                        }
                        else if (this._myRepresentative.MissionPeer.Team.Side == BattleSideEnum.Defender)
                        {
                            MBSoundEvent.PlaySound(SoundEvent.GetEventIdFromString("event:/multiplayer/retreat_horn_defender"), this._retreatHornPosition);
                        }
                        if (this._battleEndingNotificationGiven)
                        {
                            this._battleEndingLateNotificationGiven = true;
                        }
                        this._battleEndingNotificationGiven = true;
                    }
                    if (num2 <= num3 && num > num3)
                    {
                        MBSoundEvent.PlaySound(SoundEvent.GetEventIdFromString((this._myRepresentative.MissionPeer.Team.Side == BattleSideEnum.Defender) ? BattleLosingSoundEventString : BattleWinningSoundEventString), vec);
                        if (this._battleEndingNotificationGiven)
                        {
                            this._battleEndingLateNotificationGiven = true;
                        }
                        this._battleEndingNotificationGiven = true;
                    }
                }
            }
            Action<BattleSideEnum, float> onMoraleChangedEvent = this.OnMoraleChangedEvent;
            if (onMoraleChangedEvent != null)
            {
                onMoraleChangedEvent(BattleSideEnum.Attacker, num);
            }
            Action<BattleSideEnum, float> onMoraleChangedEvent2 = this.OnMoraleChangedEvent;
            if (onMoraleChangedEvent2 != null)
            {
                onMoraleChangedEvent2(BattleSideEnum.Defender, num2);
            }
            Action<int[]> onCapturePointRemainingMoraleGainsChangedEvent = this.OnCapturePointRemainingMoraleGainsChangedEvent;
            if (onCapturePointRemainingMoraleGainsChangedEvent == null)
            {
                return;
            }
            onCapturePointRemainingMoraleGainsChangedEvent(capturePointRemainingMoraleGains);
        }

        public Team GetFlagOwner(FlagCapturePoint flag)
        {
            return this._capturePointOwners[flag.FlagIndex];
        }

        public override void OnRemoveBehavior()
        {
            base.MissionNetworkComponent.OnMyClientSynchronized -= this.OnMyClientSynchronized;
            base.OnRemoveBehavior();
        }

        public override void OnMissionTick(float dt)
        {
            base.OnMissionTick(dt);
            if (this._remainingTimeForBellSoundToStop > 0f)
            {
                this._remainingTimeForBellSoundToStop -= dt;
                if (this._remainingTimeForBellSoundToStop <= 0f || base.MissionLobbyComponent.CurrentMultiplayerState != MissionLobbyComponent.MultiplayerGameState.Playing)
                {
                    this._remainingTimeForBellSoundToStop = float.MinValue;
                    this._bellSoundEvent.Stop();
                    this._bellSoundEvent = null;
                }
            }
        }

        public List<ItemObject> GetSiegeMissiles()
        {
            List<ItemObject> list = new List<ItemObject>();
            ItemObject @object = MBObjectManager.Instance.GetObject<ItemObject>("grapeshot_fire_projectile");
            list.Add(@object);
            foreach (GameEntity gameEntity in Mission.Current.GetActiveEntitiesWithScriptComponentOfType<RangedSiegeWeapon>())
            {
                RangedSiegeWeapon firstScriptOfType = gameEntity.GetFirstScriptOfType<RangedSiegeWeapon>();
                if (!string.IsNullOrEmpty(firstScriptOfType.MissileItemID))
                {
                    ItemObject object2 = MBObjectManager.Instance.GetObject<ItemObject>(firstScriptOfType.MissileItemID);
                    if (!list.Contains(object2))
                    {
                        list.Add(object2);
                    }
                }
            }
            foreach (GameEntity gameEntity2 in Mission.Current.GetActiveEntitiesWithScriptComponentOfType<StonePile>())
            {
                StonePile firstScriptOfType2 = gameEntity2.GetFirstScriptOfType<StonePile>();
                if (!string.IsNullOrEmpty(firstScriptOfType2.GivenItemID))
                {
                    ItemObject object3 = MBObjectManager.Instance.GetObject<ItemObject>(firstScriptOfType2.GivenItemID);
                    if (!list.Contains(object3))
                    {
                        list.Add(object3);
                    }
                }
            }
            return list;
        }

        private void HandleMoraleChangedMessage(GameNetworkMessage baseMessage)
        {
            SiegeMoraleChangeMessage siegeMoraleChangeMessage = (SiegeMoraleChangeMessage)baseMessage;
            this.OnMoraleChanged(siegeMoraleChangeMessage.AttackerMorale, siegeMoraleChangeMessage.DefenderMorale, siegeMoraleChangeMessage.CapturePointRemainingMoraleGains);
        }

        private void HandleServerEventUpdateGold(GameNetworkMessage baseMessage)
        {
            SyncGoldsForSkirmish syncGoldsForSkirmish = (SyncGoldsForSkirmish)baseMessage;
            CNMSiegeMissionRepresentative component = syncGoldsForSkirmish.VirtualPlayer.GetComponent<CNMSiegeMissionRepresentative>();
            this.OnGoldAmountChangedForRepresentative(component, syncGoldsForSkirmish.GoldAmount);
        }

        private void HandleFlagsRemovedMessage(GameNetworkMessage baseMessage)
        {
            this.OnNumberOfFlagsChanged();
        }

        private void HandleServerEventPointCapturedMessage(GameNetworkMessage baseMessage)
        {
            FlagDominationCapturePointMessage flagDominationCapturePointMessage = (FlagDominationCapturePointMessage)baseMessage;
            foreach (FlagCapturePoint flagCapturePoint in this.AllCapturePoints)
            {
                if (flagCapturePoint.FlagIndex == flagDominationCapturePointMessage.FlagIndex)
                {
                    this.OnCapturePointOwnerChanged(flagCapturePoint, Mission.MissionNetworkHelper.GetTeamFromTeamIndex(flagDominationCapturePointMessage.OwnerTeamIndex));
                    break;
                }
            }
        }

        private void HandleServerEventTDMGoldGain(GameNetworkMessage baseMessage)
        {
            GoldGain goldGain = (GoldGain)baseMessage;
            Action<GoldGain> onGoldGainEvent = this.OnGoldGainEvent;
            if (onGoldGainEvent == null)
            {
                return;
            }
            onGoldGainEvent(goldGain);
        }

        private const float DefenderMoraleDropThresholdIncrement = 0.2f;

        private const float DefenderMoraleDropThresholdLow = 0.4f;

        private const float DefenderMoraleDropThresholdMedium = 0.6f;

        private const float DefenderMoraleDropThresholdHigh = 0.8f;

        private const float DefenderMoraleDropMediumDuration = 8f;

        private const float DefenderMoraleDropHighDuration = 4f;

        private const float BattleWinLoseAlertThreshold = 0.25f;

        private const float BattleWinLoseLateAlertThreshold = 0.15f;

        private const string BattleWinningSoundEventString = "event:/alerts/report/battle_winning";

        private const string BattleLosingSoundEventString = "event:/alerts/report/battle_losing";

        private const float IndefiniteDurationThreshold = 8f;

        private Team[] _capturePointOwners;

        private FlagCapturePoint _masterFlag;

        private CNMSiegeMissionRepresentative _myRepresentative;

        private SoundEvent _bellSoundEvent;

        private float _remainingTimeForBellSoundToStop = float.MinValue;

        private float _lastBellSoundPercentage = 1f;

        private bool _battleEndingNotificationGiven;

        private bool _battleEndingLateNotificationGiven;

        private Vec3 _retreatHornPosition;
    }
}
