using NetworkMessages.FromServer;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;
using TaleWorlds.TwoDimension;
using static TaleWorlds.MountAndBlade.MPPerkObject;

namespace CNMultiplayer.Common
{
    public abstract class CNMSpawningBehaviorBase : SpawningBehaviorBase
    {
        private const float FemaleAiPossibility = 0.1f; //女性AI比例

        //private const float cavalryBotSpawnRatio = 0.20f; //骑兵相较Native的Spawn概率

        private const float archerBotSpawnRatio = 0.20f; //射手相较Native的Spawn概率

        //private const float horseArcherBotSpawnRatio = 0.20f; //骑射手相较Native的Spawn概率

        //最大Agent数，找不到IMBAgent，先用2048代替
        //private static int MaxAgentCount = MBAPI.IMBAgent.GetMaximumNumberOfAgents();
        private static int MaxAgentCount = 2048;

        private static readonly int AgentCountThreshold = (int)((float)CNMSpawningBehaviorBase.MaxAgentCount * 0.9f);

        private List<AgentBuildData> _agentsToBeSpawnedCache;

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

        //这个方法包含了领军模式中士兵的生成逻辑，被移除了一般模式的BOT生成逻辑
        public override void OnTick(float dt)
        {
            int currentAgentCount = Mission.Current.AllAgents.Count;
            int agentsToBeSpawnedCount = 0;
            this._agentsToBeSpawnedCache.Clear();
            foreach (NetworkCommunicator networkCommunicator in GameNetwork.NetworkPeers)
            {
                if (networkCommunicator.IsSynchronized)
                {
                    MissionPeer missionPeer = networkCommunicator.GetComponent<MissionPeer>();
                    if (missionPeer != null && missionPeer.ControlledAgent == null && missionPeer.HasSpawnedAgentVisuals && !this.CanUpdateSpawnEquipment(missionPeer))
                    {
                        MultiplayerClassDivisions.MPHeroClass mpheroClassForPeer = MultiplayerClassDivisions.GetMPHeroClassForPeer(missionPeer, false);
                        MPPerkObject.MPOnSpawnPerkHandler onSpawnPerkHandler = MPPerkObject.GetOnSpawnPerkHandler(missionPeer);
                        GameNetwork.BeginBroadcastModuleEvent();
                        GameNetwork.WriteMessage(new SyncPerksForCurrentlySelectedTroop(networkCommunicator, missionPeer.Perks[missionPeer.SelectedTroopIndex]));
                        GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.ExcludeOtherTeamPlayers, networkCommunicator);
                        int troopCount = 0;
                        bool hasBannerBearer = false;
                        int numBotsPerFormation = MultiplayerOptions.OptionType.NumberOfBotsPerFormation.GetIntValue(MultiplayerOptions.MultiplayerOptionsAccessMode.CurrentMapOptions);
                        if (numBotsPerFormation > 0 && (this.GameMode.WarmupComponent == null || !this.GameMode.WarmupComponent.IsInWarmup))
                        {
                            troopCount = MPPerkObject.GetTroopCount(mpheroClassForPeer, numBotsPerFormation, onSpawnPerkHandler);
                            using (List<MPPerkObject>.Enumerator selectedPerk = missionPeer.SelectedPerks.GetEnumerator())
                            {
                                while (selectedPerk.MoveNext())
                                {
                                    // 选择的perk是否有旗手？联机领军没有旗手perk，可能是直接移植单机代码
                                    if (selectedPerk.Current.HasBannerBearer)
                                    {
                                        hasBannerBearer = true;
                                        break;
                                    }
                                }
                            }
                        }
                        if (troopCount > 0)
                        {
                            // 玩家数量不平衡调整双方troop乘数
                            troopCount = (int)((float)troopCount * this.GameMode.GetTroopNumberMultiplierForMissingPlayer(missionPeer));
                        }
                        troopCount += (hasBannerBearer ? 2 : 1);
                        IEnumerable<ValueTuple<EquipmentIndex, EquipmentElement>> enumerable = (onSpawnPerkHandler?.GetAlternativeEquipments(false));
                        int i = 0;
                        while (i < troopCount)
                        {
                            bool isPlayer = i == 0;
                            // 为旗手单独生成一个CharacterObject
                            BasicCharacterObject basicCharacterObject = (isPlayer ? mpheroClassForPeer.HeroCharacter : ((hasBannerBearer && i == 1) ? mpheroClassForPeer.BannerBearerCharacter : mpheroClassForPeer.TroopCharacter));
                            uint clothColor1 = ((!this.GameMode.IsGameModeUsingOpposingTeams || missionPeer.Team == this.Mission.AttackerTeam) ? missionPeer.Culture.Color : missionPeer.Culture.ClothAlternativeColor);
                            uint clothColor2 = ((!this.GameMode.IsGameModeUsingOpposingTeams || missionPeer.Team == this.Mission.AttackerTeam) ? missionPeer.Culture.Color2 : missionPeer.Culture.ClothAlternativeColor2);
                            uint backGroundColor = ((!this.GameMode.IsGameModeUsingOpposingTeams || missionPeer.Team == this.Mission.AttackerTeam) ? missionPeer.Culture.BackgroundColor1 : missionPeer.Culture.BackgroundColor2);
                            uint foreGroundColor = ((!this.GameMode.IsGameModeUsingOpposingTeams || missionPeer.Team == this.Mission.AttackerTeam) ? missionPeer.Culture.ForegroundColor1 : missionPeer.Culture.ForegroundColor2);
                            // 玩家自定义旗帜
                            Banner banner = new Banner(missionPeer.Peer.BannerCode, backGroundColor, foreGroundColor);
                            AgentBuildData agentBuildData = new AgentBuildData(basicCharacterObject).VisualsIndex(i).Team(missionPeer.Team).TroopOrigin(new BasicBattleAgentOrigin(basicCharacterObject))
                                .Formation(missionPeer.ControlledFormation)
                                .IsFemale(isPlayer ? missionPeer.Peer.IsFemale : basicCharacterObject.IsFemale)
                                .ClothingColor1(clothColor1)
                                .ClothingColor2(clothColor2)
                                .Banner(banner);
                            if (isPlayer)
                            {
                                agentBuildData.MissionPeer(missionPeer);
                            }
                            else
                            {
                                //为玩家添加指挥的士兵
                                agentBuildData.OwningMissionPeer(missionPeer);
                            }
                            //为玩家和士兵分配不同的Equipment
                            Equipment equipment = (isPlayer ? basicCharacterObject.Equipment.Clone(false) : Equipment.GetRandomEquipmentElements(basicCharacterObject, false, false, MBRandom.RandomInt()));
                            //实装perk的AlternativeEquipments
                            IEnumerable<ValueTuple<EquipmentIndex, EquipmentElement>> perkAlternativeEquipments = (isPlayer ? (onSpawnPerkHandler?.GetAlternativeEquipments(true)) : enumerable);
                            if (perkAlternativeEquipments != null)
                            {
                                foreach (ValueTuple<EquipmentIndex, EquipmentElement> valueTuple in perkAlternativeEquipments)
                                {
                                    equipment[valueTuple.Item1] = valueTuple.Item2;
                                }
                            }
                            agentBuildData.Equipment(equipment);
                            /* 移除大厅自定义皮肤
                            if (flag2)
                            {
                                this.GameMode.AddCosmeticItemsToEquipment(equipment, this.GameMode.GetUsedCosmeticsFromPeer(missionPeer, basicCharacterObject));
                            }
                            */
                            if (isPlayer)
                            {
                                agentBuildData.BodyProperties(this.GetBodyProperties(missionPeer, missionPeer.Culture));
                                agentBuildData.Age((int)agentBuildData.AgentBodyProperties.Age);
                            }
                            else
                            {
                                //为BOT分配随机Equipment和身体特征
                                agentBuildData.EquipmentSeed(this.MissionLobbyComponent.GetRandomFaceSeedForCharacter(basicCharacterObject, agentBuildData.AgentVisualsIndex));
                                agentBuildData.BodyProperties(BodyProperties.GetRandomBodyProperties(agentBuildData.AgentRace, agentBuildData.AgentIsFemale, basicCharacterObject.GetBodyPropertiesMin(false), basicCharacterObject.GetBodyPropertiesMax(), (int)agentBuildData.AgentOverridenSpawnEquipment.HairCoverType, agentBuildData.AgentEquipmentSeed, basicCharacterObject.HairTags, basicCharacterObject.BeardTags, basicCharacterObject.TattooTags));
                            }
                            if (missionPeer.ControlledFormation != null && missionPeer.ControlledFormation.Banner == null)
                            {
                                missionPeer.ControlledFormation.Banner = banner;
                            }
                            // spawnFrame相关看不懂
                            MatrixFrame spawnFrame = this.SpawnComponent.GetSpawnFrame(missionPeer.Team, equipment[EquipmentIndex.ArmorItemEndSlot].Item != null, missionPeer.SpawnCountThisRound == 0);
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
                            if (missionPeer.ControlledAgent != null && !isPlayer)
                            {
                                MatrixFrame frame = missionPeer.ControlledAgent.Frame;
                                frame.rotation.OrthonormalizeAccordingToForwardAndKeepUpAsZAxis();
                                MatrixFrame matrixFrame = frame;
                                matrixFrame.origin -= matrixFrame.rotation.f.NormalizedCopy() * 3.5f;
                                Mat3 rotation = matrixFrame.rotation;
                                rotation.MakeUnit();
                                bool flag3 = !basicCharacterObject.Equipment[EquipmentIndex.ArmorItemEndSlot].IsEmpty;
                                int num7 = MathF.Min(troopCount, 10);
                                MatrixFrame matrixFrame2 = Formation.GetFormationFramesForBeforeFormationCreation((float)num7 * Formation.GetDefaultUnitDiameter(flag3) + (float)(num7 - 1) * Formation.GetDefaultMinimumInterval(flag3), troopCount, flag3, new WorldPosition(Mission.Current.Scene, matrixFrame.origin), rotation)[i - 1].ToGroundMatrixFrame();
                                agentBuildData.InitialPosition(matrixFrame2.origin);
                                AgentBuildData agentBuildData3 = agentBuildData;
                                vec = matrixFrame2.rotation.f.AsVec2;
                                vec = vec.Normalized();
                                agentBuildData3.InitialDirection(vec);
                            }
                            this._agentsToBeSpawnedCache.Add(agentBuildData);
                            agentsToBeSpawnedCount++;
                            if (!agentBuildData.AgentOverridenSpawnEquipment[EquipmentIndex.ArmorItemEndSlot].IsEmpty)
                            {
                                agentsToBeSpawnedCount++;
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
            int sumAgentsCount = agentsToBeSpawnedCount + currentAgentCount;
            // 当要生成的Agent数量与现有Agent数量大于允许的Agent数量上限时，清理超过30s未被骑过的无人马
            if (sumAgentsCount > CNMSpawningBehaviorBase.AgentCountThreshold && this._nextTimeToCleanUpMounts.IsPast)
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
            // 剩余允许生成的Agent数
            int remainAllowGeneratedAgentCount = CNMSpawningBehaviorBase.MaxAgentCount - sumAgentsCount;
            // 生成缓存池中存储的Agent
            if (remainAllowGeneratedAgentCount >= 0)
            {
                for (int k = this._agentsToBeSpawnedCache.Count - 1; k >= 0; k--)
                {
                    AgentBuildData agentBuildData4 = this._agentsToBeSpawnedCache[k];
                    bool isPlayer = agentBuildData4.AgentMissionPeer != null;
                    MissionPeer missionPeer = (isPlayer ? agentBuildData4.AgentMissionPeer : agentBuildData4.OwningAgentMissionPeer);
                    MPPerkObject.MPOnSpawnPerkHandler spawnPerk = MPPerkObject.GetOnSpawnPerkHandler(missionPeer);
                    Agent agent = this.Mission.SpawnAgent(agentBuildData4, true);
                    agent.AddComponent(new MPPerksAgentComponent(agent));
                    Agent mountAgent = agent.MountAgent;
                    mountAgent?.UpdateAgentProperties();
                    agent.HealthLimit += ((spawnPerk != null) ? spawnPerk.GetHitpoints(isPlayer) : 0f);
                    agent.Health = agent.HealthLimit;
                    if (!isPlayer)
                    {
                        agent.SetWatchState(Agent.WatchState.Alarmed);
                    }
                    // 设置初始武器
                    agent.WieldInitialWeapons(Agent.WeaponWieldActionType.InstantAfterPickUp, Equipment.InitialWeaponEquipPreference.Any);
                    if (isPlayer)
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
                        perkHandler?.OnEvent(MPPerkCondition.PerkEventFlags.SpawnEnd);
                    }
                }
                /* 隐藏SpawningBehaviorBase中成BOT的部分
                int intValue2 = MultiplayerOptions.OptionType.NumberOfBotsTeam1.GetIntValue(MultiplayerOptions.MultiplayerOptionsAccessMode.CurrentMapOptions);
                int intValue3 = MultiplayerOptions.OptionType.NumberOfBotsTeam2.GetIntValue(MultiplayerOptions.MultiplayerOptionsAccessMode.CurrentMapOptions);
                if (this.GameMode.IsGameModeUsingOpposingTeams && (intValue2 > 0 || intValue3 > 0))
                {
                    ValueTuple<Team, BasicCultureObject, int>[] array = new ValueTuple<Team, BasicCultureObject, int>[]
                    {
                        new ValueTuple<Team, BasicCultureObject, int>(this.Mission.DefenderTeam, MBObjectManager.Instance.GetObject<BasicCultureObject>(MultiplayerOptions.OptionType.CultureTeam2.GetStrValue(MultiplayerOptions.MultiplayerOptionsAccessMode.CurrentMapOptions)), intValue3 - this._botsCountForSides[0]),
                        new ValueTuple<Team, BasicCultureObject, int>(this.Mission.AttackerTeam, MBObjectManager.Instance.GetObject<BasicCultureObject>(MultiplayerOptions.OptionType.CultureTeam1.GetStrValue(MultiplayerOptions.MultiplayerOptionsAccessMode.CurrentMapOptions)), intValue2 - this._botsCountForSides[1])
                    };
                    if (num9 >= 4)
                    {
                        for (int l = 0; l < Math.Min(num9 / 2, array[0].Item3 + array[1].Item3); l++)
                        {
                            this.SpawnBot(array[l % 2].Item1, array[l % 2].Item2);
                        }
                    }
                }
                */
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
                if (botsAlive <= numberOfBots - 1) //每次生成1个AI，加快AI生成速度
                {
                    SpawnBot(team, teamCulture);
                }
            }
        }

        // 这个方法提供了与Native不同的AI生成方式，为AI添加了盾牌旗帜、实装随机perk效果，限定生成AI兵种的比例
        private new void SpawnBot(Team team, BasicCultureObject teamCulture)
        {
            //TODO: 限定AI兵种比例
            uint backGroundColor = ((!this.GameMode.IsGameModeUsingOpposingTeams || team == this.Mission.AttackerTeam) ? teamCulture.BackgroundColor1 : teamCulture.BackgroundColor2);
            uint foreGroundColor = ((!this.GameMode.IsGameModeUsingOpposingTeams || team == this.Mission.AttackerTeam) ? teamCulture.ForegroundColor1 : teamCulture.ForegroundColor2);
            Banner banner = new Banner(teamCulture.BannerKey, backGroundColor, foreGroundColor);

            Random random = new Random();
            MultiplayerClassDivisions.MPHeroClass heroClass = null;
            while (heroClass == null) //谓词查找可能会导致heroClass为null
            {
                heroClass = MultiplayerClassDivisions.GetMPHeroClasses()
                .GetRandomElementWithPredicate<MultiplayerClassDivisions.MPHeroClass>(x => x.Culture == teamCulture &&
                    ((!x.TroopCharacter.IsMounted && x.TroopCharacter.IsRanged && random.NextFloat() < archerBotSpawnRatio) || //射手
                    !x.TroopCharacter.IsMounted)); // 禁用骑兵（包括骑射手）
                    //(x.TroopCharacter.IsMounted && !x.TroopCharacter.IsRanged && random.NextFloat() < cavalryBotSpawnRatio) || // 骑兵
                    //(x.TroopCharacter.IsMounted && x.TroopCharacter.IsRanged && random.NextFloat() < horseArcherBotSpawnRatio))); // 骑射手
            }
            var heroCharacter = heroClass.HeroCharacter;
            Equipment equipment = (heroCharacter.Equipment.Clone(false));
            MatrixFrame spawnFrame = SpawnComponent.GetSpawnFrame(team, heroCharacter.HasMount(), true);
            AgentBuildData agentBuildData = new AgentBuildData(heroCharacter).Team(team).InitialPosition(spawnFrame.origin).VisualsIndex(0);
            Vec2 vec = spawnFrame.rotation.f.AsVec2;
            vec = vec.Normalized();
            AgentBuildData agentBuildData2 = agentBuildData.InitialDirection(vec).TroopOrigin(new BasicBattleAgentOrigin(heroCharacter)).EquipmentSeed(MissionLobbyComponent.GetRandomFaceSeedForCharacter(heroCharacter, 0))
                .ClothingColor1((team.Side == BattleSideEnum.Attacker) ? teamCulture.Color : teamCulture.ClothAlternativeColor)
                .ClothingColor2((team.Side == BattleSideEnum.Attacker) ? teamCulture.Color2 : teamCulture.ClothAlternativeColor2)
                .IsFemale(random.NextFloat() < FemaleAiPossibility) //BOT性别控制
                .Banner(banner); //为BOT生成国家旗帜

            // BOT随机选择Perk，by chatGPT
            List<IReadOnlyPerkObject> selectedPerks = new List<IReadOnlyPerkObject>();

            var allPerkGroups = MultiplayerClassDivisions.GetAllPerksForHeroClass(heroClass);

            foreach (var perkGroup in allPerkGroups)
            {
                if (perkGroup.Any())
                {
                    IReadOnlyPerkObject randomPerk = perkGroup[random.Next(perkGroup.Count)];
                    selectedPerks.Add(randomPerk);
                }
            }
            MPPerkObject.MPOnSpawnPerkHandler onSpawnPerkHandler = MPPerkObject.GetOnSpawnPerkHandler(selectedPerks);

            // 实装AIPerk中选择的装备
            IEnumerable<ValueTuple<EquipmentIndex, EquipmentElement>> perkAlternativeEquipments = (onSpawnPerkHandler?.GetAlternativeEquipments(true));
            if (perkAlternativeEquipments != null)
            {
                foreach (ValueTuple<EquipmentIndex, EquipmentElement> valueTuple in perkAlternativeEquipments)
                {
                    if (valueTuple.Item1 == EquipmentIndex.ExtraWeaponSlot) continue; //如果抽到“旗手”perk，跳过装备旗帜
                    equipment[valueTuple.Item1] = valueTuple.Item2;
                }
            }
            agentBuildData2.Equipment(equipment);
            agentBuildData2.BodyProperties(BodyProperties.GetRandomBodyProperties(agentBuildData2.AgentRace, agentBuildData2.AgentIsFemale, heroCharacter.GetBodyPropertiesMin(false), heroCharacter.GetBodyPropertiesMax(), (int)agentBuildData2.AgentOverridenSpawnEquipment.HairCoverType, agentBuildData2.AgentEquipmentSeed, heroCharacter.HairTags, heroCharacter.BeardTags, heroCharacter.TattooTags));
            Agent agent = Mission.SpawnAgent(agentBuildData2, false);
            MultiplayerClassDivisions.MPHeroClass mPHeroClassForCharacter = MultiplayerClassDivisions.GetMPHeroClassForCharacter(agent.Character);
            agent.AIStateFlags |= Agent.AIStateFlag.Alarmed;

            // 为AI添加护甲，与MPClassDivision及Perk相匹配
            // 为AI添加Perk的其他属性（移速、速度加成、伤害等）
            for (int i = 0; i < 55; i++)
            {
                DrivenProperty drivenProperty = (DrivenProperty)i;
                float stat = agent.AgentDrivenProperties.GetStat(drivenProperty);
                if (drivenProperty == DrivenProperty.ArmorHead || drivenProperty == DrivenProperty.ArmorTorso || drivenProperty == DrivenProperty.ArmorLegs || drivenProperty == DrivenProperty.ArmorArms)
                {
                    agent.AgentDrivenProperties.SetStat(drivenProperty, stat + (float)mPHeroClassForCharacter.ArmorValue + onSpawnPerkHandler.GetDrivenPropertyBonusOnSpawn(true, drivenProperty, stat));
                }
                else
                {
                    agent.AgentDrivenProperties.SetStat(drivenProperty, stat + onSpawnPerkHandler.GetDrivenPropertyBonusOnSpawn(true, drivenProperty, stat));
                }
            }
        }

        public override bool AllowEarlyAgentVisualsDespawning(MissionPeer missionPeer)
        {
            return false;
        }
    }
}