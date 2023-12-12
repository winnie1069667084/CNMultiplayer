using NetworkMessages.FromServer;
using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;

namespace CNMultiplayer.Common
{
    public abstract class CNMSpawningBehaviorBase : SpawningBehaviorBase
    {
        private const float FemaleAiPossibility = 0.125f; //女性AI比例

        private List<AgentBuildData> _agentsToBeSpawnedCache;

        private static int MaxAgentCount = 2048; //找不到IMBAgent，先用2048代替

        private static int AgentCountThreshold = (int)((float)CNMSpawningBehaviorBase.MaxAgentCount * 0.9f);

        private MissionTime _nextTimeToCleanUpMounts;

        private bool _hasCalledSpawningEnded;

        protected new event Action<MissionPeer> OnAllAgentsFromPeerSpawnedFromVisuals;

        protected new event Action<MissionPeer> OnPeerSpawnedFromVisuals;

        public new delegate void OnSpawningEndedEventDelegate();

        public new event SpawningBehaviorBase.OnSpawningEndedEventDelegate OnSpawningEnded;

        public override void Initialize(SpawnComponent spawnComponent)
        {
            base.Initialize(spawnComponent);
            this.SpawnComponent = spawnComponent;
            this.GameMode = this.Mission.GetMissionBehavior<MissionMultiplayerGameModeBase>();
            this.MissionLobbyComponent = this.Mission.GetMissionBehavior<MissionLobbyComponent>();
            this.MissionLobbyEquipmentNetworkComponent = this.Mission.GetMissionBehavior<MissionLobbyEquipmentNetworkComponent>();
            this.MissionLobbyEquipmentNetworkComponent.OnEquipmentRefreshed += this.OnPeerEquipmentUpdated;
            this._spawnCheckTimer = new Timer(Mission.Current.CurrentTime, 0.2f, true);
            this._agentsToBeSpawnedCache = new List<AgentBuildData>();
            this._nextTimeToCleanUpMounts = MissionTime.Now;
        }

        public override void Clear()
        {
            this.MissionLobbyEquipmentNetworkComponent.OnEquipmentRefreshed -= this.OnPeerEquipmentUpdated;
            this._agentsToBeSpawnedCache = null;
            base.Clear();

        }

        public override void OnTick(float dt)
        {
            int count = Mission.Current.AllAgents.Count;
            int num = 0;
            this._agentsToBeSpawnedCache.Clear();
            foreach (NetworkCommunicator networkCommunicator in GameNetwork.NetworkPeers)
            {
                if (networkCommunicator.IsSynchronized)
                {
                    MissionPeer component = networkCommunicator.GetComponent<MissionPeer>();
                    if (component != null && component.ControlledAgent == null && component.HasSpawnedAgentVisuals && !this.CanUpdateSpawnEquipment(component))
                    {
                        MultiplayerClassDivisions.MPHeroClass mpheroClassForPeer = MultiplayerClassDivisions.GetMPHeroClassForPeer(component, false);
                        MPPerkObject.MPOnSpawnPerkHandler onSpawnPerkHandler = MPPerkObject.GetOnSpawnPerkHandler(component);
                        GameNetwork.BeginBroadcastModuleEvent();
                        GameNetwork.WriteMessage(new SyncPerksForCurrentlySelectedTroop(networkCommunicator, component.Perks[component.SelectedTroopIndex]));
                        GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.ExcludeOtherTeamPlayers, networkCommunicator);
                        int num2 = 0;
                        bool flag = false;
                        int intValue = MultiplayerOptions.OptionType.NumberOfBotsPerFormation.GetIntValue(MultiplayerOptions.MultiplayerOptionsAccessMode.CurrentMapOptions);
                        if (intValue > 0 && (this.GameMode.WarmupComponent == null || !this.GameMode.WarmupComponent.IsInWarmup))
                        {
                            num2 = MPPerkObject.GetTroopCount(mpheroClassForPeer, intValue, onSpawnPerkHandler);
                            using (List<MPPerkObject>.Enumerator enumerator2 = component.SelectedPerks.GetEnumerator())
                            {
                                while (enumerator2.MoveNext())
                                {
                                    if (enumerator2.Current.HasBannerBearer)
                                    {
                                        flag = true;
                                        break;
                                    }
                                }
                            }
                        }
                        if (num2 > 0)
                        {
                            num2 = (int)((float)num2 * this.GameMode.GetTroopNumberMultiplierForMissingPlayer(component));
                        }
                        num2 += (flag ? 2 : 1);
                        IEnumerable<ValueTuple<EquipmentIndex, EquipmentElement>> enumerable = ((onSpawnPerkHandler != null) ? onSpawnPerkHandler.GetAlternativeEquipments(false) : null);
                        int i = 0;
                        while (i < num2)
                        {
                            bool flag2 = i == 0;
                            BasicCharacterObject basicCharacterObject = (flag2 ? mpheroClassForPeer.HeroCharacter : ((flag && i == 1) ? mpheroClassForPeer.BannerBearerCharacter : mpheroClassForPeer.TroopCharacter));
                            uint num3 = ((!this.GameMode.IsGameModeUsingOpposingTeams || component.Team == this.Mission.AttackerTeam) ? component.Culture.Color : component.Culture.ClothAlternativeColor);
                            uint num4 = ((!this.GameMode.IsGameModeUsingOpposingTeams || component.Team == this.Mission.AttackerTeam) ? component.Culture.Color2 : component.Culture.ClothAlternativeColor2);
                            uint num5 = ((!this.GameMode.IsGameModeUsingOpposingTeams || component.Team == this.Mission.AttackerTeam) ? component.Culture.BackgroundColor1 : component.Culture.BackgroundColor2);
                            uint num6 = ((!this.GameMode.IsGameModeUsingOpposingTeams || component.Team == this.Mission.AttackerTeam) ? component.Culture.ForegroundColor1 : component.Culture.ForegroundColor2);
                            Banner banner = new Banner(component.Peer.BannerCode, num5, num6);
                            AgentBuildData agentBuildData = new AgentBuildData(basicCharacterObject).VisualsIndex(i).Team(component.Team).TroopOrigin(new BasicBattleAgentOrigin(basicCharacterObject))
                                .Formation(component.ControlledFormation)
                                .IsFemale(flag2 ? component.Peer.IsFemale : basicCharacterObject.IsFemale)
                                .ClothingColor1(num3)
                                .ClothingColor2(num4)
                                .Banner(banner);
                            if (flag2)
                            {
                                agentBuildData.MissionPeer(component);
                            }
                            else
                            {
                                agentBuildData.OwningMissionPeer(component);
                            }
                            Equipment equipment = (flag2 ? basicCharacterObject.Equipment.Clone(false) : Equipment.GetRandomEquipmentElements(basicCharacterObject, false, false, MBRandom.RandomInt()));
                            IEnumerable<ValueTuple<EquipmentIndex, EquipmentElement>> enumerable2 = (flag2 ? ((onSpawnPerkHandler != null) ? onSpawnPerkHandler.GetAlternativeEquipments(true) : null) : enumerable);
                            if (enumerable2 != null)
                            {
                                foreach (ValueTuple<EquipmentIndex, EquipmentElement> valueTuple in enumerable2)
                                {
                                    equipment[valueTuple.Item1] = valueTuple.Item2;
                                }
                            }
                            agentBuildData.Equipment(equipment);
                            if (flag2)
                            {
                                this.GameMode.AddCosmeticItemsToEquipment(equipment, this.GameMode.GetUsedCosmeticsFromPeer(component, basicCharacterObject));
                            }
                            if (flag2)
                            {
                                agentBuildData.BodyProperties(this.GetBodyProperties(component, component.Culture));
                                agentBuildData.Age((int)agentBuildData.AgentBodyProperties.Age);
                            }
                            else
                            {
                                agentBuildData.EquipmentSeed(this.MissionLobbyComponent.GetRandomFaceSeedForCharacter(basicCharacterObject, agentBuildData.AgentVisualsIndex));
                                agentBuildData.BodyProperties(BodyProperties.GetRandomBodyProperties(agentBuildData.AgentRace, agentBuildData.AgentIsFemale, basicCharacterObject.GetBodyPropertiesMin(false), basicCharacterObject.GetBodyPropertiesMax(), (int)agentBuildData.AgentOverridenSpawnEquipment.HairCoverType, agentBuildData.AgentEquipmentSeed, basicCharacterObject.HairTags, basicCharacterObject.BeardTags, basicCharacterObject.TattooTags));
                            }
                            if (component.ControlledFormation != null && component.ControlledFormation.Banner == null)
                            {
                                component.ControlledFormation.Banner = banner;
                            }
                            MatrixFrame spawnFrame = this.SpawnComponent.GetSpawnFrame(component.Team, equipment[EquipmentIndex.ArmorItemEndSlot].Item != null, component.SpawnCountThisRound == 0);
                            if (spawnFrame.IsIdentity)
                            {
                                goto IL_587;
                            }
                            Vec2 vec;
                            if (!(spawnFrame.origin != agentBuildData.AgentInitialPosition))
                            {
                                vec = spawnFrame.rotation.f.AsVec2.Normalized();
                                Vec2? agentInitialDirection = agentBuildData.AgentInitialDirection;
                                if (!(vec != agentInitialDirection))
                                {
                                    goto IL_587;
                                }
                            }
                            agentBuildData.InitialPosition(spawnFrame.origin);
                            AgentBuildData agentBuildData2 = agentBuildData;
                            vec = spawnFrame.rotation.f.AsVec2;
                            vec = vec.Normalized();
                            agentBuildData2.InitialDirection(vec);
                        IL_5A0:
                            if (component.ControlledAgent != null && !flag2)
                            {
                                MatrixFrame frame = component.ControlledAgent.Frame;
                                frame.rotation.OrthonormalizeAccordingToForwardAndKeepUpAsZAxis();
                                MatrixFrame matrixFrame = frame;
                                matrixFrame.origin -= matrixFrame.rotation.f.NormalizedCopy() * 3.5f;
                                Mat3 rotation = matrixFrame.rotation;
                                rotation.MakeUnit();
                                bool flag3 = !basicCharacterObject.Equipment[EquipmentIndex.ArmorItemEndSlot].IsEmpty;
                                int num7 = MathF.Min(num2, 10);
                                MatrixFrame matrixFrame2 = Formation.GetFormationFramesForBeforeFormationCreation((float)num7 * Formation.GetDefaultUnitDiameter(flag3) + (float)(num7 - 1) * Formation.GetDefaultMinimumInterval(flag3), num2, flag3, new WorldPosition(Mission.Current.Scene, matrixFrame.origin), rotation)[i - 1].ToGroundMatrixFrame();
                                agentBuildData.InitialPosition(matrixFrame2.origin);
                                AgentBuildData agentBuildData3 = agentBuildData;
                                vec = matrixFrame2.rotation.f.AsVec2;
                                vec = vec.Normalized();
                                agentBuildData3.InitialDirection(vec);
                            }
                            this._agentsToBeSpawnedCache.Add(agentBuildData);
                            num++;
                            if (!agentBuildData.AgentOverridenSpawnEquipment[EquipmentIndex.ArmorItemEndSlot].IsEmpty)
                            {
                                num++;
                            }
                            i++;
                            continue;
                        IL_587:
                            Debug.FailedAssert("Spawn frame could not be found.", "C:\\Develop\\MB3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade\\Missions\\Multiplayer\\SpawnBehaviors\\SpawningBehaviors\\SpawningBehaviorBase.cs", "OnTick", 216);
                            goto IL_5A0;
                        }
                    }
                }
            }
            int num8 = num + count;
            if (num8 > CNMSpawningBehaviorBase.AgentCountThreshold && this._nextTimeToCleanUpMounts.IsPast)
            {
                this._nextTimeToCleanUpMounts = MissionTime.SecondsFromNow(5f);
                for (int j = Mission.Current.MountsWithoutRiders.Count - 1; j >= 0; j--)
                {
                    KeyValuePair<Agent, MissionTime> keyValuePair = Mission.Current.MountsWithoutRiders[j];
                    Agent key = keyValuePair.Key;
                    if (keyValuePair.Value.ElapsedSeconds > 30f)
                    {
                        key.FadeOut(false, false);
                    }
                }
            }
            int num9 = CNMSpawningBehaviorBase.MaxAgentCount - num8;
            if (num9 >= 0)
            {
                for (int k = this._agentsToBeSpawnedCache.Count - 1; k >= 0; k--)
                {
                    AgentBuildData agentBuildData4 = this._agentsToBeSpawnedCache[k];
                    bool flag4 = agentBuildData4.AgentMissionPeer != null;
                    MissionPeer missionPeer = (flag4 ? agentBuildData4.AgentMissionPeer : agentBuildData4.OwningAgentMissionPeer);
                    MPPerkObject.MPOnSpawnPerkHandler onSpawnPerkHandler2 = MPPerkObject.GetOnSpawnPerkHandler(missionPeer);
                    Agent agent = this.Mission.SpawnAgent(agentBuildData4, true);
                    agent.AddComponent(new MPPerksAgentComponent(agent));
                    Agent mountAgent = agent.MountAgent;
                    if (mountAgent != null)
                    {
                        mountAgent.UpdateAgentProperties();
                    }
                    agent.HealthLimit += ((onSpawnPerkHandler2 != null) ? onSpawnPerkHandler2.GetHitpoints(flag4) : 0f);
                    agent.Health = agent.HealthLimit;
                    if (!flag4)
                    {
                        agent.SetWatchState(Agent.WatchState.Alarmed);
                    }
                    agent.WieldInitialWeapons(Agent.WeaponWieldActionType.InstantAfterPickUp, Equipment.InitialWeaponEquipPreference.Any);
                    if (flag4)
                    {
                        MissionPeer missionPeer2 = missionPeer;
                        int spawnCountThisRound = missionPeer2.SpawnCountThisRound;
                        missionPeer2.SpawnCountThisRound = spawnCountThisRound + 1;
                        Action<MissionPeer> onPeerSpawnedFromVisuals = this.OnPeerSpawnedFromVisuals;
                        if (onPeerSpawnedFromVisuals != null)
                        {
                            onPeerSpawnedFromVisuals(missionPeer);
                        }
                        Action<MissionPeer> onAllAgentsFromPeerSpawnedFromVisuals = this.OnAllAgentsFromPeerSpawnedFromVisuals;
                        if (onAllAgentsFromPeerSpawnedFromVisuals != null)
                        {
                            onAllAgentsFromPeerSpawnedFromVisuals(missionPeer);
                        }
                        if (GameNetwork.IsServerOrRecorder)
                        {
                            GameNetwork.BeginBroadcastModuleEvent();
                            GameNetwork.WriteMessage(new RemoveAgentVisualsForPeer(missionPeer.GetNetworkPeer()));
                            GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None, null);
                        }
                        missionPeer.HasSpawnedAgentVisuals = false;
                        MPPerkObject.MPPerkHandler perkHandler = MPPerkObject.GetPerkHandler(missionPeer);
                        if (perkHandler != null)
                        {
                            perkHandler.OnEvent(MPPerkCondition.PerkEventFlags.SpawnEnd);
                        }
                    }
                }
            }
            if (!this.IsSpawningEnabled && this.IsRoundInProgress())
            {
                if (this.SpawningDelayTimer >= this.SpawningEndDelay && !this._hasCalledSpawningEnded)
                {
                    Mission.Current.AllowAiTicking = true;
                    if (this.OnSpawningEnded != null)
                    {
                        this.OnSpawningEnded();
                    }
                    this._hasCalledSpawningEnded = true;
                }
                this.SpawningDelayTimer += dt;
            }
        }

        private void OnPeerEquipmentUpdated(MissionPeer peer)
        {
            if (this.IsSpawningEnabled && this.CanUpdateSpawnEquipment(peer))
            {
                peer.HasSpawnedAgentVisuals = false;
                Debug.Print("HasSpawnedAgentVisuals = false for peer: " + peer.Name + " because he just updated his equipment", 0, Debug.DebugColor.White, 17592186044416UL);
                if (peer.ControlledFormation != null)
                {
                    peer.ControlledFormation.HasBeenPositioned = false;
                    peer.ControlledFormation.SetSpawnIndex(0);
                }
            }
        }

        protected override void SpawnAgents()
        {
            BasicCultureObject cultureTeam1 = MBObjectManager.Instance.GetObject<BasicCultureObject>(MultiplayerOptions.OptionType.CultureTeam1.GetStrValue());
            BasicCultureObject cultureTeam2 = MBObjectManager.Instance.GetObject<BasicCultureObject>(MultiplayerOptions.OptionType.CultureTeam2.GetStrValue());
            foreach (NetworkCommunicator networkPeer in GameNetwork.NetworkPeers)
            {
                if (!networkPeer.IsSynchronized)
                    continue;

                MissionPeer missionPeer = networkPeer.GetComponent<MissionPeer>();
                if (missionPeer == null || missionPeer.ControlledAgent != null || missionPeer.HasSpawnedAgentVisuals || missionPeer.Team == null || missionPeer.Team == Mission.SpectatorTeam || !missionPeer.TeamInitialPerkInfoReady || !missionPeer.SpawnTimer.Check(Mission.CurrentTime))
                    continue;

                BasicCultureObject teamCulture = ((missionPeer.Team.Side == BattleSideEnum.Attacker) ? cultureTeam1 : cultureTeam2);
                MultiplayerClassDivisions.MPHeroClass mPHeroClassForPeer = MultiplayerClassDivisions.GetMPHeroClassForPeer(missionPeer);
                if (mPHeroClassForPeer == null)
                {
                    if (missionPeer.SelectedTroopIndex != 0)
                    {
                        missionPeer.SelectedTroopIndex = 0;
                        GameNetwork.BeginBroadcastModuleEvent();
                        GameNetwork.WriteMessage(new UpdateSelectedTroopIndex(networkPeer, 0));
                        GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.ExcludeOtherTeamPlayers, networkPeer);
                    }
                    continue;
                }
                else
                {
                    BasicCharacterObject heroCharacter = mPHeroClassForPeer.HeroCharacter;
                    Equipment equipment = heroCharacter.Equipment.Clone();
                    IEnumerable<(EquipmentIndex, EquipmentElement)> enumerable = MPPerkObject.GetOnSpawnPerkHandler(missionPeer)?.GetAlternativeEquipments(isPlayer: true);
                    if (enumerable != null)
                    {
                        foreach (var item in enumerable)
                        {
                            equipment[item.Item1] = item.Item2;
                        }
                    }

                    AgentBuildData agentBuildData = new AgentBuildData(heroCharacter).MissionPeer(missionPeer).Equipment(equipment).Team(missionPeer.Team)
                        .TroopOrigin(new BasicBattleAgentOrigin(heroCharacter))
                        .IsFemale(missionPeer.Peer.IsFemale)
                        .BodyProperties(GetBodyProperties(missionPeer, (missionPeer.Team == Mission.AttackerTeam) ? cultureTeam1 : cultureTeam2))
                        .VisualsIndex(0)
                        .ClothingColor1((missionPeer.Team == Mission.AttackerTeam) ? teamCulture.Color : teamCulture.ClothAlternativeColor)
                        .ClothingColor2((missionPeer.Team == Mission.AttackerTeam) ? teamCulture.Color2 : teamCulture.ClothAlternativeColor2);
                    if (this.GameMode.ShouldSpawnVisualsForServer(networkPeer) && agentBuildData.AgentVisualsIndex == 0)
                    {
                        missionPeer.HasSpawnedAgentVisuals = true;
                        missionPeer.EquipmentUpdatingExpired = false;
                    }
                    GameMode.HandleAgentVisualSpawning(networkPeer, agentBuildData);
                }
            }
        }

        protected void SpawnBotAgents()
        {
            int botsTeam1 = MultiplayerOptions.OptionType.NumberOfBotsTeam1.GetIntValue();
            int botsTeam2 = MultiplayerOptions.OptionType.NumberOfBotsTeam2.GetIntValue();
            if (botsTeam1 <= 0 && botsTeam2 <= 0)
                return;

            foreach (Team team in Mission.Teams)
            {
                if (Mission.AttackerTeam != team && Mission.DefenderTeam != team)
                    continue;

                BasicCultureObject teamCulture;
                int numberOfBots;
                BasicCultureObject cultureTeam1 = MBObjectManager.Instance.GetObject<BasicCultureObject>(MultiplayerOptions.OptionType.CultureTeam1.GetStrValue());
                BasicCultureObject cultureTeam2 = MBObjectManager.Instance.GetObject<BasicCultureObject>(MultiplayerOptions.OptionType.CultureTeam2.GetStrValue());
                if (team.Side == BattleSideEnum.Attacker)
                {
                    teamCulture = cultureTeam1;
                    numberOfBots = MultiplayerOptions.OptionType.NumberOfBotsTeam1.GetIntValue();
                }
                else
                {
                    teamCulture = cultureTeam2;
                    numberOfBots = MultiplayerOptions.OptionType.NumberOfBotsTeam2.GetIntValue();
                }

                int botsAlive = team.ActiveAgents.Count(a => a.IsAIControlled && a.IsHuman);
                if (botsAlive < numberOfBots)
                {
                    var troopCharacter = MultiplayerClassDivisions.GetMPHeroClasses()
                        .GetRandomElementWithPredicate<MultiplayerClassDivisions.MPHeroClass>(x => !x.TroopCharacter.IsMounted && x.Culture == teamCulture).TroopCharacter; //禁用骑兵AI
                    MatrixFrame spawnFrame = SpawnComponent.GetSpawnFrame(team, troopCharacter.HasMount(), true);
                    AgentBuildData agentBuildData = new AgentBuildData(troopCharacter).Team(team).InitialPosition(spawnFrame.origin).VisualsIndex(0);
                    Vec2 vec = spawnFrame.rotation.f.AsVec2;
                    vec = vec.Normalized();
                    AgentBuildData agentBuildData2 = agentBuildData.InitialDirection(vec).TroopOrigin(new BasicBattleAgentOrigin(troopCharacter)).EquipmentSeed(MissionLobbyComponent.GetRandomFaceSeedForCharacter(troopCharacter, 0))
                        .ClothingColor1((team.Side == BattleSideEnum.Attacker) ? teamCulture.Color : teamCulture.ClothAlternativeColor)
                        .ClothingColor2((team.Side == BattleSideEnum.Attacker) ? teamCulture.Color2 : teamCulture.ClothAlternativeColor2)
                        .IsFemale(GenerateFemaleAIRandom(FemaleAiPossibility)); //AI性别控制
                    agentBuildData2.Equipment(Equipment.GetRandomEquipmentElements(troopCharacter, !(Game.Current.GameType is MultiplayerGame), false, agentBuildData2.AgentEquipmentSeed));
                    agentBuildData2.BodyProperties(BodyProperties.GetRandomBodyProperties(agentBuildData2.AgentRace, agentBuildData2.AgentIsFemale, troopCharacter.GetBodyPropertiesMin(false), troopCharacter.GetBodyPropertiesMax(), (int)agentBuildData2.AgentOverridenSpawnEquipment.HairCoverType, agentBuildData2.AgentEquipmentSeed, troopCharacter.HairTags, troopCharacter.BeardTags, troopCharacter.TattooTags));
                    Agent agent = Mission.SpawnAgent(agentBuildData2, false);
                    MultiplayerClassDivisions.MPHeroClass mPHeroClassForCharacter = MultiplayerClassDivisions.GetMPHeroClassForCharacter(agent.Character);
                    agent.AIStateFlags |= Agent.AIStateFlag.Alarmed;
                    agent.AgentDrivenProperties.ArmorHead = mPHeroClassForCharacter.ArmorValue; //为AI添加护甲，与MPClassDivision相匹配
                    agent.AgentDrivenProperties.ArmorTorso = mPHeroClassForCharacter.ArmorValue;
                    agent.AgentDrivenProperties.ArmorArms = mPHeroClassForCharacter.ArmorValue;
                    agent.AgentDrivenProperties.ArmorLegs = mPHeroClassForCharacter.ArmorValue;
                }
            }
        }

        private static bool GenerateFemaleAIRandom(float t)
        {
            Random ran = new Random();
            if (ran.NextFloat() < t)
                return true;
            else
                return false;
        }

        public override bool AllowEarlyAgentVisualsDespawning(MissionPeer missionPeer)
        {
            return false;
        }
    }
}