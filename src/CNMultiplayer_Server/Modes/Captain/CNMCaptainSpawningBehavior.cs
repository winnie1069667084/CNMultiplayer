using CNMultiplayer.Common;
using NetworkMessages.FromServer;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.LinQuick;
using TaleWorlds.ObjectSystem;

namespace TaleWorlds.MountAndBlade
{
    internal class CNMCaptainSpawningBehavior : CNMSpawningBehaviorBase
    {
        private const int EnforcedSpawnTimeInSeconds = 15;

        private float _spawningTimer;

        private bool _spawningTimerTicking;

        private bool _haveBotsBeenSpawned;

        private bool _roundInitialSpawnOver;

        private MissionMultiplayerFlagDomination _flagDominationMissionController;

        private MultiplayerRoundController _roundController;

        private List<KeyValuePair<MissionPeer, Timer>> _enforcedSpawnTimers;

        public CNMCaptainSpawningBehavior()
        {
            _enforcedSpawnTimers = new List<KeyValuePair<MissionPeer, Timer>>();
        }

        public override void Initialize(SpawnComponent spawnComponent)
        {
            base.Initialize(spawnComponent);
            _flagDominationMissionController = base.Mission.GetMissionBehavior<MissionMultiplayerFlagDomination>();
            _roundController = base.Mission.GetMissionBehavior<MultiplayerRoundController>();
            _roundController.OnRoundStarted += RequestStartSpawnSession;
            _roundController.OnRoundEnding += base.RequestStopSpawnSession;
            _roundController.OnRoundEnding += base.SetRemainingAgentsInvulnerable;
            if (MultiplayerOptions.OptionType.NumberOfBotsPerFormation.GetIntValue() == 0)
            {
                _roundController.EnableEquipmentUpdate();
            }
            base.OnAllAgentsFromPeerSpawnedFromVisuals += OnAllAgentsFromPeerSpawnedFromVisuals;
            base.OnPeerSpawnedFromVisuals += OnPeerSpawnedFromVisuals;
        }

        public override void Clear()
        {
            base.Clear();
            _roundController.OnRoundStarted -= RequestStartSpawnSession;
            _roundController.OnRoundEnding -= base.SetRemainingAgentsInvulnerable;
            _roundController.OnRoundEnding -= base.RequestStopSpawnSession;
            base.OnAllAgentsFromPeerSpawnedFromVisuals -= OnAllAgentsFromPeerSpawnedFromVisuals;
            base.OnPeerSpawnedFromVisuals -= OnPeerSpawnedFromVisuals;
        }

        public override void OnTick(float dt)
        {
            if (_spawningTimerTicking)
            {
                _spawningTimer += dt;
            }
            if (IsSpawningEnabled)
            {
                if (!_roundInitialSpawnOver && IsRoundInProgress())
                {
                    foreach (NetworkCommunicator networkPeer in GameNetwork.NetworkPeers)
                    {
                        MissionPeer component = networkPeer.GetComponent<MissionPeer>();
                        if (component?.Team != null && component.Team.Side != BattleSideEnum.None)
                        {
                            SpawnComponent.SetEarlyAgentVisualsDespawning(component);
                        }
                    }
                    _roundInitialSpawnOver = true;
                    base.Mission.AllowAiTicking = true;
                }
                SpawnAgents();
                if (_roundInitialSpawnOver && _flagDominationMissionController.GameModeUsesSingleSpawning && _spawningTimer > (float)MultiplayerOptions.OptionType.RoundPreparationTimeLimit.GetIntValue())
                {
                    IsSpawningEnabled = false;
                    _spawningTimer = 0f;
                    _spawningTimerTicking = false;
                }
            }
            base.OnTick(dt);
        }

        public override void RequestStartSpawnSession()
        {
            if (!IsSpawningEnabled)
            {
                Mission.Current.SetBattleAgentCount(-1);
                IsSpawningEnabled = true;
                _haveBotsBeenSpawned = false;
                _spawningTimerTicking = true;
                ResetSpawnCounts();
                ResetSpawnTimers();
            }
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
                                //防止过多的Captain AI数组越界
                                FormationClass formationClass = (FormationClass)(num2 % (int)team2.FormationsIncludingEmpty.Count);
                                formation = team2.GetFormation(formationClass);
                                num2++;
                            }
                        }
                        if (formation != null)
                        {
                            formation.BannerCode = list[(num2 - 1) % list.Count]; //防止过多的Captain AI数组越界
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
                        //team2.AddTeamAI(teamAIGeneral); //移除TeamAI
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
                    //超8人崩服的bug在这
                    FormationClass formationIndex = component.Team.FormationsIncludingSpecialAndEmpty.First((Formation x) => x.PlayerOwner == null && !x.ContainsAgentVisuals && x.CountOfUnits == 0).FormationIndex;
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

        private new void OnPeerSpawnedFromVisuals(MissionPeer peer)
        {
            if (peer.ControlledFormation != null)
            {
                peer.ControlledAgent.Team.AssignPlayerAsSergeantOfFormation(peer, peer.ControlledFormation.FormationIndex);
            }
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
                    GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None);
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
                GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None);
            }
            if (_flagDominationMissionController.UseGold())
            {
                bool flag = peer.Team == base.Mission.AttackerTeam;
                _ = base.Mission.DefenderTeam;
                MultiplayerClassDivisions.MPHeroClass mPHeroClass = MultiplayerClassDivisions.GetMPHeroClasses(MBObjectManager.Instance.GetObject<BasicCultureObject>(flag ? MultiplayerOptions.OptionType.CultureTeam1.GetStrValue() : MultiplayerOptions.OptionType.CultureTeam2.GetStrValue())).ElementAt(peer.SelectedTroopIndex);
                int num = ((_flagDominationMissionController.GetMissionType() == MissionLobbyComponent.MultiplayerGameType.Battle) ? mPHeroClass.TroopBattleCost : mPHeroClass.TroopCost);
                _flagDominationMissionController.ChangeCurrentGoldForPeer(peer, _flagDominationMissionController.GetCurrentGoldForPeer(peer) - num);
            }
        }

        private void BotFormationSpawned(Team team)
        {
            if (team == base.Mission.AttackerTeam)
            {
                base.Mission.NumOfFormationsSpawnedTeamOne++;
            }
            else if (team == base.Mission.DefenderTeam)
            {
                base.Mission.NumOfFormationsSpawnedTeamTwo++;
            }
        }

        private void AllBotFormationsSpawned()
        {
            if (base.Mission.NumOfFormationsSpawnedTeamOne != 0 || base.Mission.NumOfFormationsSpawnedTeamTwo != 0)
            {
                GameNetwork.BeginBroadcastModuleEvent();
                GameNetwork.WriteMessage(new SetSpawnedFormationCount(base.Mission.NumOfFormationsSpawnedTeamOne, base.Mission.NumOfFormationsSpawnedTeamTwo));
                GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None);
            }
        }

        public override bool AllowEarlyAgentVisualsDespawning(MissionPeer lobbyPeer)
        {
            if (MultiplayerOptions.OptionType.NumberOfBotsPerFormation.GetIntValue() == 0)
            {
                if (!_roundController.IsRoundInProgress)
                {
                    return false;
                }
                if (!lobbyPeer.HasSpawnTimerExpired && lobbyPeer.SpawnTimer.Check(Mission.Current.CurrentTime))
                {
                    lobbyPeer.HasSpawnTimerExpired = true;
                }
                return lobbyPeer.HasSpawnTimerExpired;
            }
            return false;
        }

        protected override bool IsRoundInProgress()
        {
            return _roundController.IsRoundInProgress;
        }

        private void CreateEnforcedSpawnTimerForPeer(MissionPeer peer, int durationInSeconds)
        {
            if (!_enforcedSpawnTimers.Any((KeyValuePair<MissionPeer, Timer> pair) => pair.Key == peer))
            {
                _enforcedSpawnTimers.Add(new KeyValuePair<MissionPeer, Timer>(peer, new Timer(base.Mission.CurrentTime, durationInSeconds)));
                Debug.Print("EST for " + peer.Name + " set to " + durationInSeconds + " seconds.", 0, Debug.DebugColor.Yellow, 64uL);
            }
        }

        private bool CheckIfEnforcedSpawnTimerExpiredForPeer(MissionPeer peer)
        {
            KeyValuePair<MissionPeer, Timer> keyValuePair = _enforcedSpawnTimers.FirstOrDefault((KeyValuePair<MissionPeer, Timer> pr) => pr.Key == peer);
            if (keyValuePair.Key == null)
            {
                return false;
            }
            if (peer.ControlledAgent != null)
            {
                _enforcedSpawnTimers.RemoveAll((KeyValuePair<MissionPeer, Timer> p) => p.Key == peer);
                Debug.Print("EST for " + peer.Name + " is no longer valid (spawned already).", 0, Debug.DebugColor.Yellow, 64uL);
                return false;
            }
            Timer value = keyValuePair.Value;
            if (peer.HasSpawnedAgentVisuals && value.Check(Mission.Current.CurrentTime))
            {
                SpawnComponent.SetEarlyAgentVisualsDespawning(peer);
                _enforcedSpawnTimers.RemoveAll((KeyValuePair<MissionPeer, Timer> p) => p.Key == peer);
                Debug.Print("EST for " + peer.Name + " has expired.", 0, Debug.DebugColor.Yellow, 64uL);
                return true;
            }
            return false;
        }

        public override void OnClearScene()
        {
            base.OnClearScene();
            _enforcedSpawnTimers.Clear();
            _roundInitialSpawnOver = false;
        }

        protected void SpawnBotInBotFormation(int visualsIndex, Team agentTeam, BasicCultureObject cultureLimit, BasicCharacterObject character, Formation formation)
        {
            AgentBuildData agentBuildData = new AgentBuildData(character).Team(agentTeam).TroopOrigin(new BasicBattleAgentOrigin(character)).VisualsIndex(visualsIndex)
                .EquipmentSeed(MissionLobbyComponent.GetRandomFaceSeedForCharacter(character, visualsIndex))
                .Formation(formation)
                .IsFemale(character.IsFemale)
                .ClothingColor1((agentTeam.Side == BattleSideEnum.Attacker) ? cultureLimit.Color : cultureLimit.ClothAlternativeColor)
                .ClothingColor2((agentTeam.Side == BattleSideEnum.Attacker) ? cultureLimit.Color2 : cultureLimit.ClothAlternativeColor2);
            agentBuildData.Equipment(Equipment.GetRandomEquipmentElements(character, !(Game.Current.GameType is MultiplayerGame), isCivilianEquipment: false, agentBuildData.AgentEquipmentSeed));
            agentBuildData.BodyProperties(BodyProperties.GetRandomBodyProperties(agentBuildData.AgentRace, agentBuildData.AgentIsFemale, character.GetBodyPropertiesMin(), character.GetBodyPropertiesMax(), (int)agentBuildData.AgentOverridenSpawnEquipment.HairCoverType, agentBuildData.AgentEquipmentSeed, character.HairTags, character.BeardTags, character.TattooTags));
            base.Mission.SpawnAgent(agentBuildData).AIStateFlags |= Agent.AIStateFlag.Alarmed;
        }

        protected void SpawnBotVisualsInPlayerFormation(MissionPeer missionPeer, int visualsIndex, Team agentTeam, BasicCultureObject cultureLimit, string troopName, Formation formation, bool updateExistingAgentVisuals, int totalCount, IEnumerable<(EquipmentIndex, EquipmentElement)> alternativeEquipments)
        {
            BasicCharacterObject @object = MBObjectManager.Instance.GetObject<BasicCharacterObject>(troopName);
            AgentBuildData agentBuildData = new AgentBuildData(@object).Team(agentTeam).OwningMissionPeer(missionPeer).VisualsIndex(visualsIndex)
                .TroopOrigin(new BasicBattleAgentOrigin(@object))
                .EquipmentSeed(MissionLobbyComponent.GetRandomFaceSeedForCharacter(@object, visualsIndex))
                .Formation(formation)
                .IsFemale(@object.IsFemale)
                .ClothingColor1((agentTeam.Side == BattleSideEnum.Attacker) ? cultureLimit.Color : cultureLimit.ClothAlternativeColor)
                .ClothingColor2((agentTeam.Side == BattleSideEnum.Attacker) ? cultureLimit.Color2 : cultureLimit.ClothAlternativeColor2);
            Equipment randomEquipmentElements = Equipment.GetRandomEquipmentElements(@object, !(Game.Current.GameType is MultiplayerGame), isCivilianEquipment: false, MBRandom.RandomInt());
            if (alternativeEquipments != null)
            {
                foreach (var alternativeEquipment in alternativeEquipments)
                {
                    randomEquipmentElements[alternativeEquipment.Item1] = alternativeEquipment.Item2;
                }
            }
            agentBuildData.Equipment(randomEquipmentElements);
            agentBuildData.BodyProperties(BodyProperties.GetRandomBodyProperties(agentBuildData.AgentRace, agentBuildData.AgentIsFemale, @object.GetBodyPropertiesMin(), @object.GetBodyPropertiesMax(), (int)agentBuildData.AgentOverridenSpawnEquipment.HairCoverType, agentBuildData.AgentEquipmentSeed, @object.HairTags, @object.BeardTags, @object.TattooTags));
            NetworkCommunicator networkPeer = missionPeer.GetNetworkPeer();
            if (GameMode.ShouldSpawnVisualsForServer(networkPeer))
            {
                base.AgentVisualSpawnComponent.SpawnAgentVisualsForPeer(missionPeer, agentBuildData, -1, isBot: true, totalCount);
            }
            GameMode.HandleAgentVisualSpawning(networkPeer, agentBuildData, totalCount, useCosmetics: false);
        }

    }
}