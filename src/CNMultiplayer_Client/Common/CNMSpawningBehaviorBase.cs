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
    internal abstract class CNMSpawningBehaviorBase : SpawningBehaviorBase
    {
        private const float FemaleAiPossibility = 0.25f; //女性AI比例

        private bool _hasCalledSpawningEnded;

        public event SpawningBehaviorBase.OnSpawningEndedEventDelegate OnSpawningEnded;

        protected event Action<MissionPeer> OnPeerSpawnedFromVisuals;

        protected event Action<MissionPeer> OnAllAgentsFromPeerSpawnedFromVisuals;

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
                    if (GameMode.ShouldSpawnVisualsForServer(networkPeer))
                    {
                        AgentVisualSpawnComponent.SpawnAgentVisualsForPeer(missionPeer, agentBuildData, missionPeer.SelectedTroopIndex);
                    }
                    GameMode.HandleAgentVisualSpawning(networkPeer, agentBuildData);
                }
            }
        }

        public override void OnTick(float dt)
        {
            foreach (NetworkCommunicator networkPeer in GameNetwork.NetworkPeers)
            {
                if (!networkPeer.IsSynchronized)
                {
                    continue;
                }
                MissionPeer component = networkPeer.GetComponent<MissionPeer>();
                if (component == null || component.ControlledAgent != null || !component.HasSpawnedAgentVisuals || CanUpdateSpawnEquipment(component))
                {
                    continue;
                }
                MultiplayerClassDivisions.MPHeroClass mPHeroClassForPeer = MultiplayerClassDivisions.GetMPHeroClassForPeer(component);
                MPPerkObject.MPOnSpawnPerkHandler onSpawnPerkHandler = MPPerkObject.GetOnSpawnPerkHandler(component);
                GameNetwork.BeginBroadcastModuleEvent();
                GameNetwork.WriteMessage(new SyncPerksForCurrentlySelectedTroop(networkPeer, component.Perks[component.SelectedTroopIndex]));
                GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.ExcludeOtherTeamPlayers, networkPeer);
                int num = 0;
                bool flag = false;
                if (MultiplayerOptions.OptionType.NumberOfBotsPerFormation.GetIntValue() > 0 && (GameMode.WarmupComponent == null || !GameMode.WarmupComponent.IsInWarmup))
                {
                    num = MPPerkObject.GetTroopCount(mPHeroClassForPeer, onSpawnPerkHandler);
                    foreach (MPPerkObject selectedPerk in component.SelectedPerks)
                    {
                        if (selectedPerk.HasBannerBearer)
                        {
                            flag = true;
                            break;
                        }
                    }
                }
                if (num > 0)
                {
                    num = (int)((float)num * GameMode.GetTroopNumberMultiplierForMissingPlayer(component));
                }
                num += ((!flag) ? 1 : 2);
                IEnumerable<(EquipmentIndex, EquipmentElement)> enumerable = onSpawnPerkHandler?.GetAlternativeEquipments(isPlayer: false);
                int num2 = 0;
                while (num2 < num)
                {
                    bool flag2 = num2 == 0;
                    BasicCharacterObject basicCharacterObject = (flag2 ? mPHeroClassForPeer.HeroCharacter : ((flag && num2 == 1) ? mPHeroClassForPeer.BannerBearerCharacter : mPHeroClassForPeer.TroopCharacter));
                    uint color = ((!GameMode.IsGameModeUsingOpposingTeams || component.Team == Mission.AttackerTeam) ? component.Culture.Color : component.Culture.ClothAlternativeColor);
                    uint color2 = ((!GameMode.IsGameModeUsingOpposingTeams || component.Team == Mission.AttackerTeam) ? component.Culture.Color2 : component.Culture.ClothAlternativeColor2);
                    uint color3 = ((!GameMode.IsGameModeUsingOpposingTeams || component.Team == Mission.AttackerTeam) ? component.Culture.BackgroundColor1 : component.Culture.BackgroundColor2);
                    uint color4 = ((!GameMode.IsGameModeUsingOpposingTeams || component.Team == Mission.AttackerTeam) ? component.Culture.ForegroundColor1 : component.Culture.ForegroundColor2);
                    Banner banner = new Banner(component.Peer.BannerCode, color3, color4);
                    AgentBuildData agentBuildData = new AgentBuildData(basicCharacterObject).VisualsIndex(num2).Team(component.Team).TroopOrigin(new BasicBattleAgentOrigin(basicCharacterObject))
                        .Formation(component.ControlledFormation)
                        .IsFemale(flag2 ? component.Peer.IsFemale : basicCharacterObject.IsFemale)
                        .ClothingColor1(color)
                        .ClothingColor2(color2)
                        .Banner(banner);
                    if (flag2)
                    {
                        agentBuildData.MissionPeer(component);
                    }
                    else
                    {
                        agentBuildData.OwningMissionPeer(component);
                    }
                    Equipment equipment = (flag2 ? basicCharacterObject.Equipment.Clone() : Equipment.GetRandomEquipmentElements(basicCharacterObject, randomEquipmentModifier: false, isCivilianEquipment: false, MBRandom.RandomInt()));
                    IEnumerable<(EquipmentIndex, EquipmentElement)> enumerable2 = ((!flag2) ? enumerable : onSpawnPerkHandler?.GetAlternativeEquipments(isPlayer: true));
                    if (enumerable2 != null)
                    {
                        foreach (var item in enumerable2)
                        {
                            equipment[item.Item1] = item.Item2;
                        }
                    }
                    agentBuildData.Equipment(equipment);
                    if (flag2)
                    {
                        AgentVisualSpawnComponent.AddCosmeticItemsToEquipment(equipment, AgentVisualSpawnComponent.GetUsedCosmeticsFromPeer(component, basicCharacterObject));
                    }
                    if (flag2)
                    {
                        agentBuildData.BodyProperties(GetBodyProperties(component, component.Culture));
                        agentBuildData.Age((int)agentBuildData.AgentBodyProperties.Age);
                    }
                    else
                    {
                        agentBuildData.EquipmentSeed(MissionLobbyComponent.GetRandomFaceSeedForCharacter(basicCharacterObject, agentBuildData.AgentVisualsIndex));
                        agentBuildData.BodyProperties(BodyProperties.GetRandomBodyProperties(agentBuildData.AgentRace, agentBuildData.AgentIsFemale, basicCharacterObject.GetBodyPropertiesMin(), basicCharacterObject.GetBodyPropertiesMax(), (int)agentBuildData.AgentOverridenSpawnEquipment.HairCoverType, agentBuildData.AgentEquipmentSeed, basicCharacterObject.HairTags, basicCharacterObject.BeardTags, basicCharacterObject.TattooTags));
                    }
                    if (component.ControlledFormation != null && component.ControlledFormation.Banner == null)
                    {
                        component.ControlledFormation.Banner = banner;
                    }
                    MatrixFrame spawnFrame = SpawnComponent.GetSpawnFrame(component.Team, equipment[EquipmentIndex.ArmorItemEndSlot].Item != null, component.SpawnCountThisRound == 0);
                    if (spawnFrame.IsIdentity)
                    {
                        goto IL_0533;
                    }
                    Vec3 origin = spawnFrame.origin;
                    Vec3? agentInitialPosition = agentBuildData.AgentInitialPosition;
                    Vec2 value;
                    if (!(origin != agentInitialPosition))
                    {
                        value = spawnFrame.rotation.f.AsVec2.Normalized();
                        Vec2? agentInitialDirection = agentBuildData.AgentInitialDirection;
                        if (!(value != agentInitialDirection))
                        {
                            goto IL_0533;
                        }
                    }
                    agentBuildData.InitialPosition(in spawnFrame.origin);
                    value = spawnFrame.rotation.f.AsVec2.Normalized();
                    agentBuildData.InitialDirection(in value);
                    goto IL_054c;
                IL_054c:
                    if (component.ControlledAgent != null && !flag2)
                    {
                        MatrixFrame frame = component.ControlledAgent.Frame;
                        frame.rotation.OrthonormalizeAccordingToForwardAndKeepUpAsZAxis();
                        MatrixFrame matrixFrame = frame;
                        matrixFrame.origin -= matrixFrame.rotation.f.NormalizedCopy() * 3.5f;
                        Mat3 rotation = matrixFrame.rotation;
                        rotation.MakeUnit();
                        bool flag3 = !basicCharacterObject.Equipment[EquipmentIndex.ArmorItemEndSlot].IsEmpty;
                        int num3 = TaleWorlds.Library.MathF.Min(num, 10);
                        MatrixFrame matrixFrame2 = Formation.GetFormationFramesForBeforeFormationCreation((float)num3 * Formation.GetDefaultUnitDiameter(flag3) + (float)(num3 - 1) * Formation.GetDefaultMinimumInterval(flag3), num, flag3, new WorldPosition(Mission.Current.Scene, matrixFrame.origin), rotation)[num2 - 1].ToGroundMatrixFrame(); //大量AI导致崩溃的地方
                        agentBuildData.InitialPosition(in matrixFrame2.origin);
                        value = matrixFrame2.rotation.f.AsVec2.Normalized();
                        agentBuildData.InitialDirection(in value);
                    }
                    Agent agent = Mission.SpawnAgent(agentBuildData, spawnFromAgentVisuals: true);
                    agent.AddComponent(new MPPerksAgentComponent(agent));
                    agent.MountAgent?.UpdateAgentProperties();
                    float num4 = onSpawnPerkHandler?.GetHitpoints(flag2) ?? 0f;
                    agent.HealthLimit += num4;
                    agent.Health = agent.HealthLimit;
                    if (!flag2)
                    {
                        agent.SetWatchState(Agent.WatchState.Alarmed);
                    }
                    agent.WieldInitialWeapons();
                    if (flag2)
                    {
                        this.OnPeerSpawnedFromVisuals?.Invoke(component);
                    }
                    num2++;
                    continue;
                IL_0533:
                    Debug.FailedAssert("Spawn frame could not be found.", "C:\\Develop\\MB3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade\\Missions\\Multiplayer\\SpawnBehaviors\\SpawningBehaviors\\SpawningBehaviorBase.cs", "OnTick", 194);
                    goto IL_054c;
                }
                component.SpawnCountThisRound++;
                this.OnAllAgentsFromPeerSpawnedFromVisuals?.Invoke(component);
                AgentVisualSpawnComponent.RemoveAgentVisuals(component, sync: true);
                MPPerkObject.GetPerkHandler(component)?.OnEvent(MPPerkCondition.PerkEventFlags.SpawnEnd);
            }
            if (IsSpawningEnabled || !IsRoundInProgress())
            {
                return;
            }
            if (SpawningDelayTimer >= SpawningEndDelay && !_hasCalledSpawningEnded)
            {
                Mission.Current.AllowAiTicking = true;
                if (this.OnSpawningEnded != null)
                {
                    this.OnSpawningEnded();
                }
                _hasCalledSpawningEnded = true;
            }
            SpawningDelayTimer += dt;
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