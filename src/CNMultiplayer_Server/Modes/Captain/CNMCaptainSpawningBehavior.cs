using CNMultiplayer.Common;
using NetworkMessages.FromServer;
using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.LinQuick;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;
using MathF = TaleWorlds.Library.MathF;

namespace CNMultiplayer.Modes.Captain
{
    internal class CNMCaptainSpawningBehavior : CNMSpawningBehaviorBase
    {
        private List<KeyValuePair<MissionPeer, Timer>> _enforcedSpawnTimers;

        private MultiplayerRoundController? _roundController;

        private bool _haveBotsBeenSpawned;

        private MissionMultiplayerFlagDomination _flagDominationMissionController;

        public CNMCaptainSpawningBehavior()
        {
            this._enforcedSpawnTimers = new List<KeyValuePair<MissionPeer, Timer>>();
        }

        public override void Initialize(SpawnComponent spawnComponent)
        {
            base.Initialize(spawnComponent);
            this._flagDominationMissionController = base.Mission.GetMissionBehavior<MissionMultiplayerFlagDomination>();
            this._roundController = base.Mission.GetMissionBehavior<MultiplayerRoundController>();
            this._roundController.OnRoundStarted += this.RequestStartSpawnSession;
            this._roundController.OnRoundEnding += base.RequestStopSpawnSession;
            this._roundController.OnRoundEnding += base.SetRemainingAgentsInvulnerable;
            if (MultiplayerOptions.OptionType.NumberOfBotsPerFormation.GetIntValue(MultiplayerOptions.MultiplayerOptionsAccessMode.CurrentMapOptions) == 0)
            {
                this._roundController.EnableEquipmentUpdate();
            }
            base.OnAllAgentsFromPeerSpawnedFromVisuals += this.OnAllAgentsFromPeerSpawnedFromVisuals;
            base.OnPeerSpawnedFromVisuals += this.OnPeerSpawnedFromVisuals;
        }

        protected override bool IsRoundInProgress()
        {
            return _roundController.IsRoundInProgress;
        }

        protected override void SpawnAgents()
        {
            BasicCultureObject @object = MBObjectManager.Instance.GetObject<BasicCultureObject>(MultiplayerOptions.OptionType.CultureTeam1.GetStrValue());
            BasicCultureObject object2 = MBObjectManager.Instance.GetObject<BasicCultureObject>(MultiplayerOptions.OptionType.CultureTeam2.GetStrValue());
            if (!_haveBotsBeenSpawned && (MultiplayerOptions.OptionType.NumberOfBotsTeam1.GetIntValue() > 0 || MultiplayerOptions.OptionType.NumberOfBotsTeam2.GetIntValue() > 0))
            {
                Mission.Current.AllowAiTicking = false;
                List<string> list = new List<string> { "11.8.1.4345.4345.770.774.1.0.0.133.7.5.512.512.784.769.1.0.0", "11.8.1.4345.4345.770.774.1.0.0.156.7.5.512.512.784.769.1.0.0", "11.8.1.4345.4345.770.774.1.0.0.155.7.5.512.512.784.769.1.0.0", "11.8.1.4345.4345.770.774.1.0.0.158.7.5.512.512.784.769.1.0.0", "11.8.1.4345.4345.770.774.1.0.0.118.7.5.512.512.784.769.1.0.0", "11.8.1.4345.4345.770.774.1.0.0.149.7.5.512.512.784.769.1.0.0" };
                foreach (Team team2 in base.Mission.Teams)
                {
                    if (base.Mission.AttackerTeam != team2 && base.Mission.DefenderTeam != team2)
                    {
                        continue;
                    }
                    BasicCultureObject teamCulture = ((team2 == base.Mission.AttackerTeam) ? @object : object2);
                    int num = ((base.Mission.AttackerTeam == team2) ? MultiplayerOptions.OptionType.NumberOfBotsTeam1.GetIntValue() : MultiplayerOptions.OptionType.NumberOfBotsTeam2.GetIntValue());
                    int num2 = 0;
                    for (int i = 0; i < num; i++)
                    {
                        Formation formation = null;
                        if (MultiplayerOptions.OptionType.NumberOfBotsPerFormation.GetIntValue() > 0)
                        {
                            while (formation == null || formation.PlayerOwner != null)
                            {
                                FormationClass formationClass = (FormationClass)num2;
                                formation = team2.GetFormation(formationClass);
                                _ = 10;
                                num2++;
                            }
                        }
                        if (formation != null)
                        {
                            formation.BannerCode = list[num2 - 1];
                        }
                        MultiplayerClassDivisions.MPHeroClass randomElementWithPredicate = MultiplayerClassDivisions.GetMPHeroClasses().GetRandomElementWithPredicate((MultiplayerClassDivisions.MPHeroClass x) => x.Culture == teamCulture);
                        BasicCharacterObject heroCharacter = randomElementWithPredicate.HeroCharacter;
                        AgentBuildData agentBuildData = new AgentBuildData(heroCharacter).Equipment(randomElementWithPredicate.HeroCharacter.Equipment).TroopOrigin(new BasicBattleAgentOrigin(heroCharacter)).EquipmentSeed(MissionLobbyComponent.GetRandomFaceSeedForCharacter(heroCharacter))
                            .Team(team2)
                            .VisualsIndex(0)
                            .Formation(formation)
                            .IsFemale(heroCharacter.IsFemale)
                            .ClothingColor1((team2.Side == BattleSideEnum.Attacker) ? teamCulture.Color : teamCulture.ClothAlternativeColor)
                            .ClothingColor2((team2.Side == BattleSideEnum.Attacker) ? teamCulture.Color2 : teamCulture.ClothAlternativeColor2);
                        if (MultiplayerOptions.OptionType.NumberOfBotsPerFormation.GetIntValue() == 0)
                        {
                            MatrixFrame spawnFrame = SpawnComponent.GetSpawnFrame(team2, randomElementWithPredicate.HeroCharacter.Equipment[EquipmentIndex.ArmorItemEndSlot].Item != null, isInitialSpawn: true);
                            agentBuildData.InitialPosition(in spawnFrame.origin);
                            Vec2 direction = spawnFrame.rotation.f.AsVec2.Normalized();
                            agentBuildData.InitialDirection(in direction);
                        }
                        agentBuildData.BodyProperties(BodyProperties.GetRandomBodyProperties(agentBuildData.AgentRace, agentBuildData.AgentIsFemale, heroCharacter.GetBodyPropertiesMin(), heroCharacter.GetBodyPropertiesMax(), (int)agentBuildData.AgentOverridenSpawnEquipment.HairCoverType, agentBuildData.AgentEquipmentSeed, heroCharacter.HairTags, heroCharacter.BeardTags, heroCharacter.TattooTags));
                        Agent agent = base.Mission.SpawnAgent(agentBuildData);
                        agent.SetWatchState(Agent.WatchState.Alarmed);
                        agent.WieldInitialWeapons();
                        if (MultiplayerOptions.OptionType.NumberOfBotsPerFormation.GetIntValue() > 0)
                        {
                            int num3 = MathF.Ceiling((float)MultiplayerOptions.OptionType.NumberOfBotsPerFormation.GetIntValue() * randomElementWithPredicate.TroopMultiplier);
                            for (int j = 0; j < num3; j++)
                            {
                                SpawnBotInBotFormation(j + 1, team2, teamCulture, randomElementWithPredicate.TroopCharacter, formation);
                            }
                            BotFormationSpawned(team2);
                            formation.SetControlledByAI(isControlledByAI: true);
                        }
                    }
                    if (num > 0 && team2.FormationsIncludingEmpty.AnyQ((Formation f) => f.CountOfUnits > 0))
                    {
                        TeamAIGeneral teamAIGeneral = new TeamAIGeneral(Mission.Current, team2);
                        teamAIGeneral.AddTacticOption(new TacticSergeantMPBotTactic(team2));
                        team2.AddTeamAI(teamAIGeneral);
                    }
                }
                AllBotFormationsSpawned();
                _haveBotsBeenSpawned = true;
            }
            foreach (NetworkCommunicator networkPeer in GameNetwork.NetworkPeers)
            {
                MissionPeer component = networkPeer.GetComponent<MissionPeer>();
                if (!networkPeer.IsSynchronized || component.Team == null || component.Team.Side == BattleSideEnum.None || (MultiplayerOptions.OptionType.NumberOfBotsPerFormation.GetIntValue() == 0 && CheckIfEnforcedSpawnTimerExpiredForPeer(component)))
                {
                    continue;
                }
                Team team = component.Team;
                bool num4 = team == base.Mission.AttackerTeam;
                _ = base.Mission.DefenderTeam;
                BasicCultureObject basicCultureObject = (num4 ? @object : object2);
                MultiplayerClassDivisions.MPHeroClass mPHeroClassForPeer = MultiplayerClassDivisions.GetMPHeroClassForPeer(component);
                int num5 = ((_flagDominationMissionController.GetMissionType() == MissionLobbyComponent.MultiplayerGameType.Battle) ? mPHeroClassForPeer.TroopBattleCost : mPHeroClassForPeer.TroopCost);
                if (component.ControlledAgent != null || component.HasSpawnedAgentVisuals || component.Team == null || component.Team == base.Mission.SpectatorTeam || !component.TeamInitialPerkInfoReady || !component.SpawnTimer.Check(base.Mission.CurrentTime))
                {
                    continue;
                }
                int currentGoldForPeer = _flagDominationMissionController.GetCurrentGoldForPeer(component);
                if (mPHeroClassForPeer == null || (_flagDominationMissionController.UseGold() && num5 > currentGoldForPeer))
                {
                    if (currentGoldForPeer >= MultiplayerClassDivisions.GetMinimumTroopCost(basicCultureObject) && component.SelectedTroopIndex != 0)
                    {
                        component.SelectedTroopIndex = 0;
                        GameNetwork.BeginBroadcastModuleEvent();
                        GameNetwork.WriteMessage(new UpdateSelectedTroopIndex(networkPeer, 0));
                        GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.ExcludeOtherTeamPlayers, networkPeer);
                    }
                    continue;
                }
                if (MultiplayerOptions.OptionType.NumberOfBotsPerFormation.GetIntValue() == 0)
                {
                    CreateEnforcedSpawnTimerForPeer(component, 15);
                }
                Formation formation2 = component.ControlledFormation;
                if (MultiplayerOptions.OptionType.NumberOfBotsPerFormation.GetIntValue() > 0 && formation2 == null)
                {
                    FormationClass formationIndex = component.Team.FormationsIncludingEmpty.First((Formation x) => x.PlayerOwner == null && !x.ContainsAgentVisuals && x.CountOfUnits == 0).FormationIndex;
                    formation2 = team.GetFormation(formationIndex);
                    formation2.ContainsAgentVisuals = true;
                    if (string.IsNullOrEmpty(formation2.BannerCode))
                    {
                        formation2.BannerCode = component.Peer.BannerCode;
                    }
                }
                BasicCharacterObject heroCharacter2 = mPHeroClassForPeer.HeroCharacter;
                AgentBuildData agentBuildData2 = new AgentBuildData(heroCharacter2).MissionPeer(component).Team(component.Team).VisualsIndex(0)
                    .Formation(formation2)
                    .MakeUnitStandOutOfFormationDistance(7f)
                    .IsFemale(component.Peer.IsFemale)
                    .BodyProperties(GetBodyProperties(component, (component.Team == base.Mission.AttackerTeam) ? @object : object2))
                    .ClothingColor1((team == base.Mission.AttackerTeam) ? basicCultureObject.Color : basicCultureObject.ClothAlternativeColor)
                    .ClothingColor2((team == base.Mission.AttackerTeam) ? basicCultureObject.Color2 : basicCultureObject.ClothAlternativeColor2);
                MPPerkObject.MPOnSpawnPerkHandler onSpawnPerkHandler = MPPerkObject.GetOnSpawnPerkHandler(component);
                Equipment equipment = heroCharacter2.Equipment.Clone();
                IEnumerable<(EquipmentIndex, EquipmentElement)> enumerable = onSpawnPerkHandler?.GetAlternativeEquipments(isPlayer: true);
                if (enumerable != null)
                {
                    foreach (var item in enumerable)
                    {
                        equipment[item.Item1] = item.Item2;
                    }
                }
                int amountOfAgentVisualsForPeer = component.GetAmountOfAgentVisualsForPeer();
                bool flag = amountOfAgentVisualsForPeer > 0;
                agentBuildData2.Equipment(equipment);
                if (MultiplayerOptions.OptionType.NumberOfBotsPerFormation.GetIntValue() == 0)
                {
                    if (!flag)
                    {
                        MatrixFrame spawnFrame2 = SpawnComponent.GetSpawnFrame(component.Team, equipment[EquipmentIndex.ArmorItemEndSlot].Item != null, isInitialSpawn: true);
                        agentBuildData2.InitialPosition(in spawnFrame2.origin);
                        Vec2 direction = spawnFrame2.rotation.f.AsVec2.Normalized();
                        agentBuildData2.InitialDirection(in direction);
                    }
                    else
                    {
                        MatrixFrame frame = component.GetAgentVisualForPeer(0).GetFrame();
                        agentBuildData2.InitialPosition(in frame.origin);
                        Vec2 direction = frame.rotation.f.AsVec2.Normalized();
                        agentBuildData2.InitialDirection(in direction);
                    }
                }
                if (GameMode.ShouldSpawnVisualsForServer(networkPeer))
                {
                    base.AgentVisualSpawnComponent.SpawnAgentVisualsForPeer(component, agentBuildData2, component.SelectedTroopIndex);
                }
                GameMode.HandleAgentVisualSpawning(networkPeer, agentBuildData2);
                component.ControlledFormation = formation2;
                if (MultiplayerOptions.OptionType.NumberOfBotsPerFormation.GetIntValue() <= 0)
                {
                    continue;
                }
                int troopCount = MPPerkObject.GetTroopCount(mPHeroClassForPeer, onSpawnPerkHandler);
                IEnumerable<(EquipmentIndex, EquipmentElement)> alternativeEquipments = onSpawnPerkHandler?.GetAlternativeEquipments(isPlayer: false);
                for (int k = 0; k < troopCount; k++)
                {
                    if (k + 1 >= amountOfAgentVisualsForPeer)
                    {
                        flag = false;
                    }
                    SpawnBotVisualsInPlayerFormation(component, k + 1, team, basicCultureObject, mPHeroClassForPeer.TroopCharacter.StringId, formation2, flag, troopCount, alternativeEquipments);
                }
            }
        }

        protected void SpawnBotVisualsInPlayerFormation(MissionPeer missionPeer, int visualsIndex, Team agentTeam, BasicCultureObject cultureLimit, string troopName, Formation formation, bool updateExistingAgentVisuals, int totalCount, IEnumerable<ValueTuple<EquipmentIndex, EquipmentElement>> alternativeEquipments)
        {
            BasicCharacterObject @object = MBObjectManager.Instance.GetObject<BasicCharacterObject>(troopName);
            AgentBuildData agentBuildData = new AgentBuildData(@object).Team(agentTeam).OwningMissionPeer(missionPeer).VisualsIndex(visualsIndex)
                .TroopOrigin(new BasicBattleAgentOrigin(@object))
                .EquipmentSeed(this.MissionLobbyComponent.GetRandomFaceSeedForCharacter(@object, visualsIndex))
                .Formation(formation)
                .IsFemale(@object.IsFemale)
                .ClothingColor1((agentTeam.Side == BattleSideEnum.Attacker) ? cultureLimit.Color : cultureLimit.ClothAlternativeColor)
                .ClothingColor2((agentTeam.Side == BattleSideEnum.Attacker) ? cultureLimit.Color2 : cultureLimit.ClothAlternativeColor2);
            Equipment randomEquipmentElements = Equipment.GetRandomEquipmentElements(@object, !(Game.Current.GameType is MultiplayerGame), false, MBRandom.RandomInt());
            if (alternativeEquipments != null)
            {
                foreach (ValueTuple<EquipmentIndex, EquipmentElement> valueTuple in alternativeEquipments)
                {
                    randomEquipmentElements[valueTuple.Item1] = valueTuple.Item2;
                }
            }
            agentBuildData.Equipment(randomEquipmentElements);
            agentBuildData.BodyProperties(BodyProperties.GetRandomBodyProperties(agentBuildData.AgentRace, agentBuildData.AgentIsFemale, @object.GetBodyPropertiesMin(false), @object.GetBodyPropertiesMax(), (int)agentBuildData.AgentOverridenSpawnEquipment.HairCoverType, agentBuildData.AgentEquipmentSeed, @object.HairTags, @object.BeardTags, @object.TattooTags));
            NetworkCommunicator networkPeer = missionPeer.GetNetworkPeer();
            if (this.GameMode.ShouldSpawnVisualsForServer(networkPeer))
            {
                base.AgentVisualSpawnComponent.SpawnAgentVisualsForPeer(missionPeer, agentBuildData, -1, true, totalCount);
            }
            this.GameMode.HandleAgentVisualSpawning(networkPeer, agentBuildData, totalCount, false);
        }

        private void AllBotFormationsSpawned()
        {
            if (base.Mission.NumOfFormationsSpawnedTeamOne != 0 || base.Mission.NumOfFormationsSpawnedTeamTwo != 0)
            {
                GameNetwork.BeginBroadcastModuleEvent();
                GameNetwork.WriteMessage(new SetSpawnedFormationCount(base.Mission.NumOfFormationsSpawnedTeamOne, base.Mission.NumOfFormationsSpawnedTeamTwo));
                GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None, null);
            }
        }

        protected void SpawnBotInBotFormation(int visualsIndex, Team agentTeam, BasicCultureObject cultureLimit, BasicCharacterObject character, Formation formation)
        {
            AgentBuildData agentBuildData = new AgentBuildData(character).Team(agentTeam).TroopOrigin(new BasicBattleAgentOrigin(character)).VisualsIndex(visualsIndex)
                .EquipmentSeed(this.MissionLobbyComponent.GetRandomFaceSeedForCharacter(character, visualsIndex))
                .Formation(formation)
                .IsFemale(character.IsFemale)
                .ClothingColor1((agentTeam.Side == BattleSideEnum.Attacker) ? cultureLimit.Color : cultureLimit.ClothAlternativeColor)
                .ClothingColor2((agentTeam.Side == BattleSideEnum.Attacker) ? cultureLimit.Color2 : cultureLimit.ClothAlternativeColor2);
            agentBuildData.Equipment(Equipment.GetRandomEquipmentElements(character, !(Game.Current.GameType is MultiplayerGame), false, agentBuildData.AgentEquipmentSeed));
            agentBuildData.BodyProperties(BodyProperties.GetRandomBodyProperties(agentBuildData.AgentRace, agentBuildData.AgentIsFemale, character.GetBodyPropertiesMin(false), character.GetBodyPropertiesMax(), (int)agentBuildData.AgentOverridenSpawnEquipment.HairCoverType, agentBuildData.AgentEquipmentSeed, character.HairTags, character.BeardTags, character.TattooTags));
            base.Mission.SpawnAgent(agentBuildData, false).AIStateFlags |= Agent.AIStateFlag.Alarmed;
        }

        private void CreateEnforcedSpawnTimerForPeer(MissionPeer peer, int durationInSeconds)
        {
            if (this._enforcedSpawnTimers.Any((KeyValuePair<MissionPeer, Timer> pair) => pair.Key == peer))
            {
                return;
            }
            this._enforcedSpawnTimers.Add(new KeyValuePair<MissionPeer, Timer>(peer, new Timer(base.Mission.CurrentTime, (float)durationInSeconds, true)));
            Debug.Print(string.Concat(new object[] { "EST for ", peer.Name, " set to ", durationInSeconds, " seconds." }), 0, Debug.DebugColor.Yellow, 64UL);
        }

        private void BotFormationSpawned(Team team)
        {
            if (team == base.Mission.AttackerTeam)
            {
                base.Mission.NumOfFormationsSpawnedTeamOne++;
                return;
            }
            if (team == base.Mission.DefenderTeam)
            {
                base.Mission.NumOfFormationsSpawnedTeamTwo++;
            }
        }

        private bool CheckIfEnforcedSpawnTimerExpiredForPeer(MissionPeer peer)
        {
            KeyValuePair<MissionPeer, Timer> keyValuePair = this._enforcedSpawnTimers.FirstOrDefault((KeyValuePair<MissionPeer, Timer> pr) => pr.Key == peer);
            if (keyValuePair.Key == null)
            {
                return false;
            }
            if (peer.ControlledAgent != null)
            {
                this._enforcedSpawnTimers.RemoveAll((KeyValuePair<MissionPeer, Timer> p) => p.Key == peer);
                Debug.Print("EST for " + peer.Name + " is no longer valid (spawned already).", 0, Debug.DebugColor.Yellow, 64UL);
                return false;
            }
            Timer value = keyValuePair.Value;
            if (peer.HasSpawnedAgentVisuals && value.Check(Mission.Current.CurrentTime))
            {
                this.SpawnComponent.SetEarlyAgentVisualsDespawning(peer, true);
                this._enforcedSpawnTimers.RemoveAll((KeyValuePair<MissionPeer, Timer> p) => p.Key == peer);
                Debug.Print("EST for " + peer.Name + " has expired.", 0, Debug.DebugColor.Yellow, 64UL);
                return true;
            }
            return false;
        }

        private new void OnAllAgentsFromPeerSpawnedFromVisuals(MissionPeer peer)
        {
            if (peer.ControlledFormation != null)
            {
                peer.ControlledFormation.OnFormationDispersed();
                peer.ControlledFormation.SetMovementOrder(MovementOrder.MovementOrderFollow(peer.ControlledAgent));
                NetworkCommunicator networkPeer = peer.GetNetworkPeer();
                if (peer.BotsUnderControlAlive != 0 || peer.BotsUnderControlTotal != 0)
                {
                    GameNetwork.BeginBroadcastModuleEvent();
                    GameNetwork.WriteMessage(new BotsControlledChange(networkPeer, peer.BotsUnderControlAlive, peer.BotsUnderControlTotal));
                    GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None, null);
                    base.Mission.GetMissionBehavior<MissionMultiplayerGameModeFlagDominationClient>().OnBotsControlledChanged(peer, peer.BotsUnderControlAlive, peer.BotsUnderControlTotal);
                }
                if (peer.Team == base.Mission.AttackerTeam)
                {
                    base.Mission.NumOfFormationsSpawnedTeamOne++;
                }
                else
                {
                    base.Mission.NumOfFormationsSpawnedTeamTwo++;
                }
                GameNetwork.BeginBroadcastModuleEvent();
                GameNetwork.WriteMessage(new SetSpawnedFormationCount(base.Mission.NumOfFormationsSpawnedTeamOne, base.Mission.NumOfFormationsSpawnedTeamTwo));
                GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None, null);
            }
            if (this._flagDominationMissionController.UseGold())
            {
                bool flag = peer.Team == base.Mission.AttackerTeam;
                Team defenderTeam = base.Mission.DefenderTeam;
                MultiplayerClassDivisions.MPHeroClass mpheroClass = MultiplayerClassDivisions.GetMPHeroClasses(MBObjectManager.Instance.GetObject<BasicCultureObject>(flag ? MultiplayerOptions.OptionType.CultureTeam1.GetStrValue(MultiplayerOptions.MultiplayerOptionsAccessMode.CurrentMapOptions) : MultiplayerOptions.OptionType.CultureTeam2.GetStrValue(MultiplayerOptions.MultiplayerOptionsAccessMode.CurrentMapOptions))).ElementAt(peer.SelectedTroopIndex);
                int num = ((this._flagDominationMissionController.GetMissionType() == MissionLobbyComponent.MultiplayerGameType.Battle) ? mpheroClass.TroopBattleCost : mpheroClass.TroopCost);
                this._flagDominationMissionController.ChangeCurrentGoldForPeer(peer, this._flagDominationMissionController.GetCurrentGoldForPeer(peer) - num);
            }
        }

        private new void OnPeerSpawnedFromVisuals(MissionPeer peer)
        {
            if (peer.ControlledFormation != null)
            {
                peer.ControlledAgent.Team.AssignPlayerAsSergeantOfFormation(peer, peer.ControlledFormation.FormationIndex);
            }
        }
    }
}
