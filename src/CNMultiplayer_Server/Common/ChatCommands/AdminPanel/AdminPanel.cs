﻿using NetworkMessages.FromServer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.DedicatedCustomServer;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace ChatCommands
{
    public struct MissionData
    {
        public string gameType;
        public string mapId;
        public string cultureTeam1;
        public string cultureTeam2;
        public bool cultureVote;
        public bool mapVote;
    }

    class StartMissionThread
    {
        // To successfully change the map, this thread must be called when the mission has ended
        public static void ThreadProc(Object missionData)
        {
            // Give us some buffer between the OnMissionEnd event and starting the next mission
            Thread.Sleep(500);

            // Prevent infinite loop if for some reason a call to StartMission 
            AdminPanel.Instance.StartMissionOnly((MissionData)missionData);
            AdminPanel.Instance.EndingCurrentMissionThenStartingNewMission = false;
        }
    }

    class MissionListener : IMissionListener
    {
        MissionData missionData;

        public void setMissionData(MissionData missionData)
        {
            this.missionData = missionData;
        }

        public void OnConversationCharacterChanged()
        {

        }

        public void OnEndMission()
        {
            // Run a thread that will create a start a mission after a delay
            Thread t = new Thread(new ParameterizedThreadStart(StartMissionThread.ThreadProc));
            t.Start(missionData);

            Mission.Current.RemoveListener(this);
        }

        public void OnEquipItemsFromSpawnEquipment(Agent agent, Agent.CreationType creationType)
        {

        }

        public void OnEquipItemsFromSpawnEquipmentBegin(Agent agent, Agent.CreationType creationType)
        {

        }

        public void OnInitialDeploymentPlanMade(BattleSideEnum battleSide, bool isFirstPlan)
        {

        }

        public void OnMissionModeChange(MissionMode oldMissionMode, bool atStart)
        {

        }

        public void OnResetMission()
        {

        }
    }

    // TODO: A lot of the find/search functions are all the same. See if we can refactor that down a bit
    public class AdminPanel
    {

        // Singleton
        private static AdminPanel instance;
        public static AdminPanel Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new AdminPanel();
                }
                return instance;
            }
        }

        public bool MissionIsRunning
        {
            get
            {
                return Mission.Current != null;
            }
        }

        // Prevent multiple missions from being started at once
        public bool EndingCurrentMissionThenStartingNewMission = false;

        string GetOptionString(MultiplayerOptions.OptionType optionType)
        {
            string toReturn;
            MultiplayerOptions.Instance.GetOptionFromOptionType(optionType).GetValue(out toReturn);
            return toReturn;
        }

        MissionData getMultiplayerOptionsState()
        {
            MissionData toReturn;

            toReturn.cultureVote = MultiplayerIntermissionVotingManager.Instance.IsCultureVoteEnabled;
            toReturn.mapVote = MultiplayerIntermissionVotingManager.Instance.IsMapVoteEnabled;
            toReturn.cultureTeam1 = GetOptionString(MultiplayerOptions.OptionType.CultureTeam1);
            toReturn.cultureTeam2 = GetOptionString(MultiplayerOptions.OptionType.CultureTeam2);
            toReturn.mapId = GetOptionString(MultiplayerOptions.OptionType.Map);
            toReturn.gameType = GetOptionString(MultiplayerOptions.OptionType.GameType);

            return toReturn;
        }

        List<string> GetMapsForCurrentGameType()
        {
            return MultiplayerOptions.Instance.GetMultiplayerOptionsList(MultiplayerOptions.OptionType.Map);
        }

        List<string> GetMapsInPool()
        {
            return MultiplayerIntermissionVotingManager.Instance.MapVoteItems.Select(kvp => kvp.Id).ToList();
        }

        public List<string> GetAllAvailableMaps()
        {
            return GetMapsForCurrentGameType().Union(GetMapsInPool()).ToList();
        }

        List<string> FindMaps(string searchString)
        {
            return GetAllAvailableMaps().Where(str => str.Contains(searchString)).ToList();
        }

        public Tuple<bool, string> FindSingleMap(string searchString)
        {
            List<string> foundMaps = FindMaps(searchString);

            if (foundMaps.Count == 1)
            {
                return new Tuple<bool, string>(true, foundMaps[0]);
            }
            else if (foundMaps.Count > 1)
            {
                // Check for special case where the name of a map sits inside the name of another map ie mp_tdm_map_001 or mp_tdm_map_001_spring
                foreach (string mapName in foundMaps)
                {
                    if (mapName == searchString)
                    {
                        return new Tuple<bool, string>(true, mapName);
                    }
                }

                return new Tuple<bool, string>(false, "More than one map found matching '" + searchString + "'");
            }
            else
            {
                return new Tuple<bool, string>(false, "No maps found matching '" + searchString + "'");
            }
        }

        public void ChangeMap(string mapId)
        {
            MissionData currentState = getMultiplayerOptionsState();
            currentState.mapId = mapId;
            StartMission(currentState);
        }

        public List<string> GetAllFactions()
        {
            return MultiplayerOptions.Instance.GetMultiplayerOptionsList(MultiplayerOptions.OptionType.CultureTeam1);
        }

        public List<string> FindMatchingFactions(string searchString)
        {
            List<string> availableFactions = GetAllFactions();

            return availableFactions.Where(str => str.Contains(searchString)).ToList();
        }

        public Tuple<bool, string> FindSingleFaction(string searchString)
        {
            List<string> foundFactions = FindMatchingFactions(searchString);

            if (foundFactions.Count == 1)
            {
                return new Tuple<bool, string>(true, foundFactions[0]);
            }
            else if (foundFactions.Count > 1)
            {
                return new Tuple<bool, string>(false, "More than one faction found matching '" + searchString + "'");
            }
            else
            {
                return new Tuple<bool, string>(false, "No factions found matching '" + searchString + "'");
            }
        }

        public List<string> GetGameTypes()
        {
            return MultiplayerOptions.Instance.GetMultiplayerOptionsList(MultiplayerOptions.OptionType.GameType);
        }

        private List<string> GetMatchingGameTypes(string searchString)
        {
            List<string> availableGameTypes = GetGameTypes();

            return availableGameTypes.Where(str => str.Contains(searchString)).ToList();
        }

        public Tuple<bool, string> FindSingleGameType(string searchString)
        {
            List<string> foundGameTypes = GetMatchingGameTypes(searchString);

            if (foundGameTypes.Count == 1)
            {
                return new Tuple<bool, string>(true, foundGameTypes[0]);
            }
            else if (foundGameTypes.Count > 1)
            {
                return new Tuple<bool, string>(false, "More than one game type found matching '" + searchString + "'");
            }
            else
            {
                return new Tuple<bool, string>(false, "No game types found matching '" + searchString + "'");
            }
        }

        public List<string> GetMapsForGameType(string searchString)
        {

            Tuple<bool, string> gameTypeSearch = FindSingleGameType(searchString);
            if (gameTypeSearch.Item1)
            {
                return MultiplayerGameTypes.GetGameTypeInfo(gameTypeSearch.Item2).Scenes.ToList().Union(GetMapsInPool()).ToList();
            }
            return new List<string>();
        }


        public Tuple<bool, string> FindMapForGameType(string gameType, string searchString)
        {
            List<string> foundMaps = GetMapsForGameType(gameType);

            List<string> filtered = foundMaps.Where(str => str.Contains(searchString)).ToList(); ;

            if (filtered.Count == 1)
            {
                return new Tuple<bool, string>(true, filtered[0]);
            }
            else if (filtered.Count > 1)
            {
                // Check for special case where the name of a map sits inside the name of another map ie mp_tdm_map_001 or mp_tdm_map_001_spring
                foreach (string mapName in filtered)
                {
                    if (mapName == searchString)
                    {
                        return new Tuple<bool, string>(true, mapName);
                    }
                }

                return new Tuple<bool, string>(false, "More than one map found matching '" + searchString + "'");
            }
            else
            {
                return new Tuple<bool, string>(false, "No maps found matching '" + searchString + "'");
            }
        }

        // NOTE: Does not verify if the current map matches the game type!
        public void ChangeGameTypeMapFactions(string gameType, string mapId, string faction1, string faction2)
        {
            MissionData currentState = getMultiplayerOptionsState();
            currentState.gameType = gameType;
            currentState.mapId = mapId;
            currentState.cultureTeam1 = faction1;
            currentState.cultureTeam2 = faction2;
            StartMission(currentState);
        }

        public void ChangeMapAndFactions(string mapId, string faction1, string faction2)
        {
            MissionData currentState = getMultiplayerOptionsState();
            currentState.mapId = mapId;
            currentState.cultureTeam1 = faction1;
            currentState.cultureTeam2 = faction2;
            StartMission(currentState);
        }

        public void SetMultiplayerOptions(MissionData missionData, MultiplayerOptions.MultiplayerOptionsAccessMode opetionSet = MultiplayerOptions.MultiplayerOptionsAccessMode.CurrentMapOptions)
        {
            MultiplayerOptions.Instance.GetOptionFromOptionType(MultiplayerOptions.OptionType.GameType, opetionSet).UpdateValue(missionData.gameType);
            MultiplayerOptions.Instance.GetOptionFromOptionType(MultiplayerOptions.OptionType.Map, opetionSet).UpdateValue(missionData.mapId);
            MultiplayerOptions.Instance.GetOptionFromOptionType(MultiplayerOptions.OptionType.CultureTeam1, opetionSet).UpdateValue(missionData.cultureTeam1);
            MultiplayerOptions.Instance.GetOptionFromOptionType(MultiplayerOptions.OptionType.CultureTeam2, opetionSet).UpdateValue(missionData.cultureTeam2);
            MultiplayerIntermissionVotingManager.Instance.IsCultureVoteEnabled = missionData.cultureVote;
            MultiplayerIntermissionVotingManager.Instance.IsMapVoteEnabled = missionData.mapVote;
            MultiplayerIntermissionVotingManager.Instance.ClearVotes();
            MultiplayerIntermissionVotingManager.Instance.SetVotesOfCulture(1, 100);
            MultiplayerIntermissionVotingManager.Instance.SetVotesOfCulture(2, 100);

        }

        private void SyncMultiplayerOptionsToClients()
        {
            GameNetwork.BeginBroadcastModuleEvent();
            GameNetwork.WriteMessage((GameNetworkMessage)new MultiplayerOptionsInitial());
            GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.IncludeUnsynchronizedClients, (NetworkCommunicator)null);
            GameNetwork.BeginBroadcastModuleEvent();
            GameNetwork.WriteMessage((GameNetworkMessage)new MultiplayerOptionsImmediate());
            GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.IncludeUnsynchronizedClients, (NetworkCommunicator)null);
        }

        private void ResetFactionVoteCount()
        {
            MultiplayerIntermissionVotingManager.Instance.ClearVotes();
        }

        public void StartMission(MissionData missionData)
        {
            if (!EndingCurrentMissionThenStartingNewMission)
            {
                if (!MissionIsRunning)
                {
                    StartMissionOnly(missionData);
                }
                else
                {
                    EndMissionThenStartMission(missionData);
                }
            }
        }

        private void EndMissionThenStartMission(MissionData missionData)
        {
            MissionListener listener = new MissionListener();
            Mission.Current.AddListener(listener);

            //MultiplayerIntermissionVotingManager.Instance.IsCultureVoteEnabled = false;
            //MultiplayerIntermissionVotingManager.Instance.IsMapVoteEnabled = false;

            EndingCurrentMissionThenStartingNewMission = true;

            listener.setMissionData(missionData);
            DedicatedCustomServerSubModule.Instance.EndMission();
        }

        public bool StartMissionOnly(MissionData missionData)
        {
            if (!MissionIsRunning)
            {
                SetMultiplayerOptions(missionData);
                DedicatedCustomServerSubModule.Instance.StartMission();
                SyncMultiplayerOptionsToClients();
                ResetFactionVoteCount();
                return true;
            }

            return false;
        }

        public bool EndMission()
        {
            if (MissionIsRunning)
            {
                DedicatedCustomServerSubModule.Instance.StartMission();
            }

            return false;
        }

        public void SetBots(int team1, int team2)
        {
            MultiplayerOptions.Instance.GetOptionFromOptionType(MultiplayerOptions.OptionType.NumberOfBotsTeam1).UpdateValue(team1);
            MultiplayerOptions.Instance.GetOptionFromOptionType(MultiplayerOptions.OptionType.NumberOfBotsTeam2).UpdateValue(team2);
        }
    }
}
