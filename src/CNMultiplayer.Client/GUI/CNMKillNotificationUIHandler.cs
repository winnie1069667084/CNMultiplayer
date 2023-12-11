
using CNMultiplayer.Common.Modes.CNMSiege;
using NetworkMessages.FromServer;
using System;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Multiplayer.View.MissionViews;
using TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.KillFeed;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.MissionViews;

namespace CNMultiplayer.Client.GUI
{
    [OverrideView(typeof(MissionMultiplayerKillNotificationUIHandler))]
    internal class CNMKillNotificationUIHandler : MissionView
    {
        private MPKillFeedVM _dataSource;
        private GauntletLayer _gauntletLayer;
        private MissionMultiplayerTeamDeathmatchClient _tdmClient;
        private CNMSiegeClient _siegeClient;
        private MissionMultiplayerGameModeDuelClient _duelClient;
        private MissionMultiplayerGameModeFlagDominationClient _flagDominationClient;
        private bool _isGeneralFeedEnabled;
        private bool _doesGameModeAllowGeneralFeed = true;
        private bool _isPersonalFeedEnabled;

        public override void OnMissionScreenInitialize()
        {
            base.OnMissionScreenInitialize();
            this.ViewOrderPriority = 2;
            this._isGeneralFeedEnabled = this._doesGameModeAllowGeneralFeed && BannerlordConfig.ReportCasualtiesType < 2;
            this._isPersonalFeedEnabled = BannerlordConfig.ReportPersonalDamage;
            this._dataSource = new MPKillFeedVM();
            this._gauntletLayer = new GauntletLayer(this.ViewOrderPriority, "GauntletLayer", false);
            this._gauntletLayer.LoadMovie("MultiplayerKillFeed", this._dataSource);
            base.MissionScreen.AddLayer(this._gauntletLayer);
            CombatLogManager.OnGenerateCombatLog += this.OnCombatLogManagerOnPrintCombatLog;
            ManagedOptions.OnManagedOptionChanged = (ManagedOptions.OnManagedOptionChangedDelegate)Delegate.Combine(ManagedOptions.OnManagedOptionChanged, new ManagedOptions.OnManagedOptionChangedDelegate(this.OnOptionChange));
        }

        private void OnOptionChange(ManagedOptions.ManagedOptionsType changedManagedOptionsType)
        {
            if (changedManagedOptionsType == ManagedOptions.ManagedOptionsType.ReportCasualtiesType)
            {
                this._isGeneralFeedEnabled = this._doesGameModeAllowGeneralFeed && BannerlordConfig.ReportCasualtiesType < 2;
                return;
            }
            if (changedManagedOptionsType == ManagedOptions.ManagedOptionsType.ReportPersonalDamage)
            {
                this._isPersonalFeedEnabled = BannerlordConfig.ReportPersonalDamage;
            }
        }

        public override void AfterStart()
        {
            base.AfterStart();
            this._tdmClient = base.Mission.GetMissionBehavior<MissionMultiplayerTeamDeathmatchClient>();
            if (this._tdmClient != null)
            {
                this._tdmClient.OnGoldGainEvent += this.OnGoldGain;
            }
            this._siegeClient = base.Mission.GetMissionBehavior<CNMSiegeClient>();
            if (this._siegeClient != null)
            {
                this._siegeClient.OnGoldGainEvent += this.OnGoldGain;
            }
            this._flagDominationClient = base.Mission.GetMissionBehavior<MissionMultiplayerGameModeFlagDominationClient>();
            if (this._flagDominationClient != null)
            {
                this._flagDominationClient.OnGoldGainEvent += this.OnGoldGain;
            }
            this._duelClient = base.Mission.GetMissionBehavior<MissionMultiplayerGameModeDuelClient>();
            if (this._duelClient != null)
            {
                this._doesGameModeAllowGeneralFeed = false;
            }
        }

        public override void OnMissionScreenFinalize()
        {
            base.OnMissionScreenFinalize();
            CombatLogManager.OnGenerateCombatLog -= this.OnCombatLogManagerOnPrintCombatLog;
            ManagedOptions.OnManagedOptionChanged = (ManagedOptions.OnManagedOptionChangedDelegate)Delegate.Remove(ManagedOptions.OnManagedOptionChanged, new ManagedOptions.OnManagedOptionChangedDelegate(this.OnOptionChange));
            if (this._tdmClient != null)
            {
                this._tdmClient.OnGoldGainEvent -= this.OnGoldGain;
            }
            if (this._siegeClient != null)
            {
                this._siegeClient.OnGoldGainEvent -= this.OnGoldGain;
            }
            if (this._flagDominationClient != null)
            {
                this._flagDominationClient.OnGoldGainEvent -= this.OnGoldGain;
            }
            base.MissionScreen.RemoveLayer(this._gauntletLayer);
            this._gauntletLayer = null;
            this._dataSource.OnFinalize();
            this._dataSource = null;
        }

        private void OnGoldGain(GoldGain goldGainMessage)
        {
            if (this._isPersonalFeedEnabled)
            {
                foreach (KeyValuePair<ushort, int> keyValuePair in goldGainMessage.GoldChangeEventList)
                {
                    this._dataSource.PersonalCasualty.OnGoldChange(keyValuePair.Value, (GoldGainFlags)keyValuePair.Key);
                }
            }
        }

        private void OnCombatLogManagerOnPrintCombatLog(CombatLogData logData)
        {
            if (this._isPersonalFeedEnabled && (logData.IsAttackerAgentMine || logData.IsAttackerAgentRiderAgentMine) && logData.TotalDamage > 0 && !logData.IsVictimAgentSameAsAttackerAgent)
            {
                this._dataSource.OnPersonalDamage(logData.TotalDamage, logData.IsFatalDamage, logData.IsVictimAgentMount, logData.IsFriendlyFire, logData.BodyPartHit == BoneBodyPartType.Head, logData.VictimAgentName);
            }
        }

        public override void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow killingBlow)
        {
            base.OnAgentRemoved(affectedAgent, affectorAgent, agentState, killingBlow);
            if (!this._isGeneralFeedEnabled || GameNetwork.IsDedicatedServer || affectorAgent == null || !affectedAgent.IsHuman || (agentState != AgentState.Killed && agentState != AgentState.Unconscious))
            {
                return;
            }
            this._dataSource.OnAgentRemoved(affectedAgent, affectorAgent, this._isPersonalFeedEnabled);
        }
    }
}
