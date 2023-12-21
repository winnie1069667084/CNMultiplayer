using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Diamond;
using TaleWorlds.MountAndBlade.Multiplayer.NetworkComponents;
using TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.Intermission;

namespace CNMultiplayer.Client.GUI.Intermission
{
    // copy from Full Invasion 3
    internal class CNMIntermissionVM : ViewModel
    {
        public CNMIntermissionVM()
        {
            this.AvailableMaps = new MBBindingList<MPIntermissionMapItemVM>();
            this.AvailableCultures = new MBBindingList<MPIntermissionCultureItemVM>();
            this.RefreshValues();
        }

        public override void RefreshValues()
        {
            this.QuitText = new TextObject("{=3sRdGQou}Leave", null).ToString();
            this.PlayersLabel = new TextObject("{=RfXJdNye}Players", null).ToString();
            this.MapVoteText = new TextObject("{=DraJ6bxq}Vote for the Next Map", null).ToString();
            this.CultureVoteText = new TextObject("{=oF27vprQ}Vote for the Next Faction", null).ToString();
        }

        public void Tick()
        {
            bool flag = !this._hasBaseNetworkComponentSet;
            if (flag)
            {
                this._baseNetworkComponent = GameNetwork.GetNetworkComponent<BaseNetworkComponent>();
                bool flag2 = this._baseNetworkComponent != null;
                if (flag2)
                {
                    this._hasBaseNetworkComponentSet = true;
                    BaseNetworkComponent baseNetworkComponent = this._baseNetworkComponent;
                    baseNetworkComponent.OnIntermissionStateUpdated = (Action)Delegate.Combine(baseNetworkComponent.OnIntermissionStateUpdated, new Action(this.OnIntermissionStateUpdated));
                }
            }
            else
            {
                bool flag3 = this._baseNetworkComponent.ClientIntermissionState == MultiplayerIntermissionState.Idle;
                if (flag3)
                {
                    this.NextGameStateTimerLabel = this._serverIdleLabelText.ToString();
                    this.NextGameStateTimerValue = string.Empty;
                    this.IsMissionTimerEnabled = false;
                    this.IsEndGameTimerEnabled = false;
                    this.IsNextMapInfoEnabled = false;
                    this.IsMapVoteEnabled = false;
                    this.IsCultureVoteEnabled = false;
                    this.IsPlayerCountEnabled = false;
                }
            }
        }

        public override void OnFinalize()
        {
            bool flag = this._baseNetworkComponent != null;
            if (flag)
            {
                BaseNetworkComponent baseNetworkComponent = this._baseNetworkComponent;
                baseNetworkComponent.OnIntermissionStateUpdated = (Action)Delegate.Remove(baseNetworkComponent.OnIntermissionStateUpdated, new Action(this.OnIntermissionStateUpdated));
            }
            MultiplayerIntermissionVotingManager.Instance.ClearItems();
        }

        private void OnIntermissionStateUpdated()
        {
            this._currentIntermissionState = this._baseNetworkComponent.ClientIntermissionState;
            bool flag = true;
            bool flag2 = this._currentIntermissionState == MultiplayerIntermissionState.CountingForMapVote;
            if (flag2)
            {
                int num = (int)this._baseNetworkComponent.CurrentIntermissionTimer;
                this.NextGameStateTimerLabel = this._voteLabelText.ToString();
                this.NextGameStateTimerValue = num.ToString();
                this.IsMissionTimerEnabled = true;
                this.IsEndGameTimerEnabled = false;
                this.IsNextMapInfoEnabled = false;
                this.IsCultureVoteEnabled = false;
                this.IsPlayerCountEnabled = true;
                flag = false;
                List<IntermissionVoteItem> mapVoteItems = MultiplayerIntermissionVotingManager.Instance.MapVoteItems;
                bool flag3 = mapVoteItems.Count > 0;
                if (flag3)
                {
                    this.IsMapVoteEnabled = true;
                    using (List<IntermissionVoteItem>.Enumerator enumerator = mapVoteItems.GetEnumerator())
                    {
                        while (enumerator.MoveNext())
                        {
                            IntermissionVoteItem mapItem = enumerator.Current;
                            bool flag4 = this.AvailableMaps.FirstOrDefault((MPIntermissionMapItemVM m) => m.MapID == mapItem.Id) == null;
                            if (flag4)
                            {
                                this.AvailableMaps.Add(new MPIntermissionMapItemVM(mapItem.Id, new Action<MPIntermissionMapItemVM>(this.OnPlayerVotedForMap)));
                            }
                            int voteCount = mapItem.VoteCount;
                            this.AvailableMaps.First((MPIntermissionMapItemVM m) => m.MapID == mapItem.Id).Votes = voteCount;
                        }
                    }
                }
            }
            bool flag5 = this._baseNetworkComponent.ClientIntermissionState == MultiplayerIntermissionState.CountingForCultureVote;
            if (flag5)
            {
                int num2 = (int)this._baseNetworkComponent.CurrentIntermissionTimer;
                this.NextGameStateTimerLabel = this._voteLabelText.ToString();
                this.NextGameStateTimerValue = num2.ToString();
                this.IsMissionTimerEnabled = true;
                this.IsEndGameTimerEnabled = false;
                this.IsNextMapInfoEnabled = false;
                this.IsMapVoteEnabled = false;
                this.IsPlayerCountEnabled = true;
                flag = false;
                List<IntermissionVoteItem> cultureVoteItems = MultiplayerIntermissionVotingManager.Instance.CultureVoteItems;
                bool flag6 = cultureVoteItems.Count > 0;
                if (flag6)
                {
                    this.IsCultureVoteEnabled = true;
                    using (List<IntermissionVoteItem>.Enumerator enumerator2 = cultureVoteItems.GetEnumerator())
                    {
                        while (enumerator2.MoveNext())
                        {
                            IntermissionVoteItem cultureItem = enumerator2.Current;
                            bool flag7 = this.AvailableCultures.FirstOrDefault((MPIntermissionCultureItemVM c) => c.CultureCode == cultureItem.Id) == null;
                            if (flag7)
                            {
                                this.AvailableCultures.Add(new MPIntermissionCultureItemVM(cultureItem.Id, new Action<MPIntermissionCultureItemVM>(this.OnPlayerVotedForCulture)));
                            }
                            int voteCount2 = cultureItem.VoteCount;
                            this.AvailableCultures.FirstOrDefault((MPIntermissionCultureItemVM c) => c.CultureCode == cultureItem.Id).Votes = voteCount2;
                        }
                    }
                }
                string nextFactionACultureID;
                MultiplayerOptions.Instance.GetOptionFromOptionType(MultiplayerOptions.OptionType.CultureTeam1, MultiplayerOptions.MultiplayerOptionsAccessMode.CurrentMapOptions).GetValue(out nextFactionACultureID);
                string nextFactionBCultureID;
                MultiplayerOptions.Instance.GetOptionFromOptionType(MultiplayerOptions.OptionType.CultureTeam2, MultiplayerOptions.MultiplayerOptionsAccessMode.CurrentMapOptions).GetValue(out nextFactionBCultureID);
                this.NextFactionACultureID = nextFactionACultureID;
                this.NextFactionBCultureID = nextFactionBCultureID;
            }
            bool flag8 = this._currentIntermissionState == MultiplayerIntermissionState.CountingForMission;
            if (flag8)
            {
                int num3 = (int)this._baseNetworkComponent.CurrentIntermissionTimer;
                this.NextGameStateTimerLabel = this._nextGameLabelText.ToString();
                this.NextGameStateTimerValue = num3.ToString();
                this.IsMissionTimerEnabled = true;
                this.IsEndGameTimerEnabled = false;
                this.IsNextMapInfoEnabled = true;
                this.IsMapVoteEnabled = false;
                this.IsCultureVoteEnabled = false;
                this.IsPlayerCountEnabled = true;
                flag = true;
                this.AvailableMaps.Clear();
                this.AvailableCultures.Clear();
                MultiplayerIntermissionVotingManager.Instance.ClearVotes();
                this._votedMapItem = null;
                this._votedCultureItem = null;
            }
            bool flag9 = this._currentIntermissionState == MultiplayerIntermissionState.CountingForEnd;
            if (flag9)
            {
                TextObject textObject = GameTexts.FindText("str_string_newline_string", null);
                textObject.SetTextVariable("STR1", this._matchFinishedText.ToString());
                textObject.SetTextVariable("STR2", this._returningToLobbyText.ToString());
                this.NextGameStateTimerLabel = textObject.ToString();
                this.NextGameStateTimerValue = string.Empty;
                this.IsMissionTimerEnabled = false;
                this.IsEndGameTimerEnabled = false;
                this.IsNextMapInfoEnabled = false;
                this.IsMapVoteEnabled = false;
                this.IsCultureVoteEnabled = false;
                this.IsPlayerCountEnabled = false;
                flag = false;
            }
            string text;
            MultiplayerOptions.Instance.GetOptionFromOptionType(MultiplayerOptions.OptionType.Map, MultiplayerOptions.MultiplayerOptionsAccessMode.CurrentMapOptions).GetValue(out text);
            this.NextMapID = (this.IsEndGameTimerEnabled ? string.Empty : text);
            this.NextMapName = (this.IsEndGameTimerEnabled ? string.Empty : GameTexts.FindText("str_multiplayer_scene_name", text).ToString());
            bool flag10 = flag;
            if (flag10)
            {
                string text2;
                MultiplayerOptions.Instance.GetOptionFromOptionType(MultiplayerOptions.OptionType.CultureTeam1, MultiplayerOptions.MultiplayerOptionsAccessMode.CurrentMapOptions).GetValue(out text2);
                this.IsFactionAValid = !this.IsEndGameTimerEnabled && !string.IsNullOrEmpty(text2) && this._currentIntermissionState != MultiplayerIntermissionState.CountingForMapVote;
                this.NextFactionACultureID = (this.IsEndGameTimerEnabled ? string.Empty : text2);
                string text3;
                MultiplayerOptions.Instance.GetOptionFromOptionType(MultiplayerOptions.OptionType.CultureTeam2, MultiplayerOptions.MultiplayerOptionsAccessMode.CurrentMapOptions).GetValue(out text3);
                this.IsFactionBValid = !this.IsEndGameTimerEnabled && !string.IsNullOrEmpty(text3) && this._currentIntermissionState != MultiplayerIntermissionState.CountingForMapVote;
                this.NextFactionBCultureID = (this.IsEndGameTimerEnabled ? string.Empty : text3);
            }
            else
            {
                this.IsFactionAValid = false;
                this.IsFactionBValid = false;
            }
            string serverName;
            MultiplayerOptions.Instance.GetOptionFromOptionType(MultiplayerOptions.OptionType.ServerName, MultiplayerOptions.MultiplayerOptionsAccessMode.CurrentMapOptions).GetValue(out serverName);
            this.ServerName = serverName;
            string variation;
            MultiplayerOptions.Instance.GetOptionFromOptionType(MultiplayerOptions.OptionType.GameType, MultiplayerOptions.MultiplayerOptionsAccessMode.CurrentMapOptions).GetValue(out variation);
            this.NextGameType = (this.IsEndGameTimerEnabled ? string.Empty : GameTexts.FindText("str_multiplayer_game_type", variation).ToString());
            string text4;
            MultiplayerOptions.Instance.GetOptionFromOptionType(MultiplayerOptions.OptionType.WelcomeMessage, MultiplayerOptions.MultiplayerOptionsAccessMode.CurrentMapOptions).GetValue(out text4);
            this.WelcomeMessage = (this.IsEndGameTimerEnabled ? string.Empty : text4);
            int num4;
            MultiplayerOptions.Instance.GetOptionFromOptionType(MultiplayerOptions.OptionType.MaxNumberOfPlayers, MultiplayerOptions.MultiplayerOptionsAccessMode.CurrentMapOptions).GetValue(out num4);
            this.MaxNumPlayersValueText = num4.ToString();
            this.ConnectedPlayersCountValueText = GameNetwork.NetworkPeers.Count.ToString();
        }

        public void ExecuteQuitServer()
        {
            LobbyClient gameClient = NetworkMain.GameClient;
            bool flag = gameClient.CurrentState == LobbyClient.State.InCustomGame;
            if (flag)
            {
                gameClient.QuitFromCustomGame();
            }
            MultiplayerIntermissionVotingManager.Instance.ClearItems();
        }

        private void OnPlayerVotedForMap(MPIntermissionMapItemVM mapItem)
        {
            bool flag = this._votedMapItem != null;
            int votes;
            if (flag)
            {
                this._baseNetworkComponent.IntermissionCastVote(this._votedMapItem.MapID, -1);
                this._votedMapItem.IsSelected = false;
                MPIntermissionMapItemVM votedMapItem = this._votedMapItem;
                votes = votedMapItem.Votes;
                votedMapItem.Votes = votes - 1;
            }
            this._baseNetworkComponent.IntermissionCastVote(mapItem.MapID, 1);
            this._votedMapItem = mapItem;
            this._votedMapItem.IsSelected = true;
            MPIntermissionMapItemVM votedMapItem2 = this._votedMapItem;
            votes = votedMapItem2.Votes;
            votedMapItem2.Votes = votes + 1;
        }

        private void OnPlayerVotedForCulture(MPIntermissionCultureItemVM cultureItem)
        {
            bool flag = this._votedCultureItem != null;
            int votes;
            if (flag)
            {
                this._baseNetworkComponent.IntermissionCastVote(this._votedCultureItem.CultureCode, -1);
                this._votedCultureItem.IsSelected = false;
                MPIntermissionCultureItemVM votedCultureItem = this._votedCultureItem;
                votes = votedCultureItem.Votes;
                votedCultureItem.Votes = votes - 1;
            }
            this._baseNetworkComponent.IntermissionCastVote(cultureItem.CultureCode, 1);
            this._votedCultureItem = cultureItem;
            this._votedCultureItem.IsSelected = true;
            MPIntermissionCultureItemVM votedCultureItem2 = this._votedCultureItem;
            votes = votedCultureItem2.Votes;
            votedCultureItem2.Votes = votes + 1;
        }

        [DataSourceProperty]
        public string ConnectedPlayersCountValueText
        {
            get
            {
                return this._connectedPlayersCountValueText;
            }
            set
            {
                bool flag = value != this._connectedPlayersCountValueText;
                if (flag)
                {
                    this._connectedPlayersCountValueText = value;
                    base.OnPropertyChangedWithValue<string>(value, "ConnectedPlayersCountValueText");
                }
            }
        }

        [DataSourceProperty]
        public string MaxNumPlayersValueText
        {
            get
            {
                return this._maxNumPlayersValueText;
            }
            set
            {
                bool flag = value != this._maxNumPlayersValueText;
                if (flag)
                {
                    this._maxNumPlayersValueText = value;
                    base.OnPropertyChangedWithValue<string>(value, "MaxNumPlayersValueText");
                }
            }
        }

        [DataSourceProperty]
        public bool IsFactionAValid
        {
            get
            {
                return this._isFactionAValid;
            }
            set
            {
                bool flag = value != this._isFactionAValid;
                if (flag)
                {
                    this._isFactionAValid = value;
                    base.OnPropertyChangedWithValue(value, "IsFactionAValid");
                }
            }
        }

        [DataSourceProperty]
        public bool IsFactionBValid
        {
            get
            {
                return this._isFactionBValid;
            }
            set
            {
                bool flag = value != this._isFactionBValid;
                if (flag)
                {
                    this._isFactionBValid = value;
                    base.OnPropertyChangedWithValue(value, "IsFactionBValid");
                }
            }
        }

        [DataSourceProperty]
        public bool IsMissionTimerEnabled
        {
            get
            {
                return this._isMissionTimerEnabled;
            }
            set
            {
                bool flag = value != this._isMissionTimerEnabled;
                if (flag)
                {
                    this._isMissionTimerEnabled = value;
                    base.OnPropertyChangedWithValue(value, "IsMissionTimerEnabled");
                }
            }
        }

        [DataSourceProperty]
        public bool IsEndGameTimerEnabled
        {
            get
            {
                return this._isEndGameTimerEnabled;
            }
            set
            {
                bool flag = value != this._isEndGameTimerEnabled;
                if (flag)
                {
                    this._isEndGameTimerEnabled = value;
                    base.OnPropertyChangedWithValue(value, "IsEndGameTimerEnabled");
                }
            }
        }

        [DataSourceProperty]
        public bool IsNextMapInfoEnabled
        {
            get
            {
                return this._isNextMapInfoEnabled;
            }
            set
            {
                bool flag = value != this._isNextMapInfoEnabled;
                if (flag)
                {
                    this._isNextMapInfoEnabled = value;
                    base.OnPropertyChangedWithValue(value, "IsNextMapInfoEnabled");
                }
            }
        }

        [DataSourceProperty]
        public bool IsMapVoteEnabled
        {
            get
            {
                return this._isMapVoteEnabled;
            }
            set
            {
                bool flag = value != this._isMapVoteEnabled;
                if (flag)
                {
                    this._isMapVoteEnabled = value;
                    base.OnPropertyChangedWithValue(value, "IsMapVoteEnabled");
                }
            }
        }

        [DataSourceProperty]
        public bool IsCultureVoteEnabled
        {
            get
            {
                return this._isCultureVoteEnabled;
            }
            set
            {
                bool flag = value != this._isCultureVoteEnabled;
                if (flag)
                {
                    this._isCultureVoteEnabled = value;
                    base.OnPropertyChangedWithValue(value, "IsCultureVoteEnabled");
                }
            }
        }

        [DataSourceProperty]
        public bool IsPlayerCountEnabled
        {
            get
            {
                return this._isPlayerCountEnabled;
            }
            set
            {
                bool flag = value != this._isPlayerCountEnabled;
                if (flag)
                {
                    this._isPlayerCountEnabled = value;
                    base.OnPropertyChangedWithValue(value, "IsPlayerCountEnabled");
                }
            }
        }

        [DataSourceProperty]
        public string NextMapID
        {
            get
            {
                return this._nextMapId;
            }
            set
            {
                bool flag = value != this._nextMapId;
                if (flag)
                {
                    this._nextMapId = value;
                    base.OnPropertyChangedWithValue<string>(value, "NextMapID");
                }
            }
        }

        [DataSourceProperty]
        public string NextFactionACultureID
        {
            get
            {
                return this._nextFactionACultureId;
            }
            set
            {
                bool flag = value != this._nextFactionACultureId;
                if (flag)
                {
                    this._nextFactionACultureId = value;
                    base.OnPropertyChangedWithValue<string>(value, "NextFactionACultureID");
                }
            }
        }

        [DataSourceProperty]
        public string NextFactionBCultureID
        {
            get
            {
                return this._nextFactionBCultureId;
            }
            set
            {
                bool flag = value != this._nextFactionBCultureId;
                if (flag)
                {
                    this._nextFactionBCultureId = value;
                    base.OnPropertyChangedWithValue<string>(value, "NextFactionBCultureID");
                }
            }
        }

        [DataSourceProperty]
        public string PlayersLabel
        {
            get
            {
                return this._playersLabel;
            }
            set
            {
                bool flag = value != this._playersLabel;
                if (flag)
                {
                    this._playersLabel = value;
                    base.OnPropertyChangedWithValue<string>(value, "PlayersLabel");
                }
            }
        }

        [DataSourceProperty]
        public string MapVoteText
        {
            get
            {
                return this._mapVoteText;
            }
            set
            {
                bool flag = value != this._mapVoteText;
                if (flag)
                {
                    this._mapVoteText = value;
                    base.OnPropertyChangedWithValue<string>(value, "MapVoteText");
                }
            }
        }

        [DataSourceProperty]
        public string CultureVoteText
        {
            get
            {
                return this._cultureVoteText;
            }
            set
            {
                bool flag = value != this._cultureVoteText;
                if (flag)
                {
                    this._cultureVoteText = value;
                    base.OnPropertyChangedWithValue<string>(value, "CultureVoteText");
                }
            }
        }

        [DataSourceProperty]
        public string NextGameStateTimerLabel
        {
            get
            {
                return this._nextGameStateTimerLabel;
            }
            set
            {
                bool flag = value != this._nextGameStateTimerLabel;
                if (flag)
                {
                    this._nextGameStateTimerLabel = value;
                    base.OnPropertyChangedWithValue<string>(value, "NextGameStateTimerLabel");
                }
            }
        }

        [DataSourceProperty]
        public string NextGameStateTimerValue
        {
            get
            {
                return this._nextGameStateTimerValue;
            }
            set
            {
                bool flag = value != this._nextGameStateTimerValue;
                if (flag)
                {
                    this._nextGameStateTimerValue = value;
                    base.OnPropertyChangedWithValue<string>(value, "NextGameStateTimerValue");
                }
            }
        }

        [DataSourceProperty]
        public string WelcomeMessage
        {
            get
            {
                return this._welcomeMessage;
            }
            set
            {
                bool flag = value != this._welcomeMessage;
                if (flag)
                {
                    this._welcomeMessage = value;
                    base.OnPropertyChangedWithValue<string>(value, "WelcomeMessage");
                }
            }
        }

        [DataSourceProperty]
        public string ServerName
        {
            get
            {
                return this._serverName;
            }
            set
            {
                bool flag = value != this._serverName;
                if (flag)
                {
                    this._serverName = value;
                    base.OnPropertyChangedWithValue<string>(value, "ServerName");
                }
            }
        }

        [DataSourceProperty]
        public string NextGameType
        {
            get
            {
                return this._nextGameType;
            }
            set
            {
                bool flag = value != this._nextGameType;
                if (flag)
                {
                    this._nextGameType = value;
                    base.OnPropertyChangedWithValue<string>(value, "NextGameType");
                }
            }
        }

        [DataSourceProperty]
        public string NextMapName
        {
            get
            {
                return this._nextMapName;
            }
            set
            {
                bool flag = value != this._nextMapName;
                if (flag)
                {
                    this._nextMapName = value;
                    base.OnPropertyChangedWithValue<string>(value, "NextMapName");
                }
            }
        }

        [DataSourceProperty]
        public MBBindingList<MPIntermissionMapItemVM> AvailableMaps
        {
            get
            {
                return this._availableMaps;
            }
            set
            {
                bool flag = value != this._availableMaps;
                if (flag)
                {
                    this._availableMaps = value;
                    base.OnPropertyChangedWithValue<MBBindingList<MPIntermissionMapItemVM>>(value, "AvailableMaps");
                }
            }
        }

        [DataSourceProperty]
        public MBBindingList<MPIntermissionCultureItemVM> AvailableCultures
        {
            get
            {
                return this._availableCultures;
            }
            set
            {
                bool flag = value != this._availableCultures;
                if (flag)
                {
                    this._availableCultures = value;
                    base.OnPropertyChangedWithValue<MBBindingList<MPIntermissionCultureItemVM>>(value, "AvailableCultures");
                }
            }
        }

        [DataSourceProperty]
        public string QuitText
        {
            get
            {
                return this._quitText;
            }
            set
            {
                bool flag = value != this._quitText;
                if (flag)
                {
                    this._quitText = value;
                    base.OnPropertyChangedWithValue<string>(value, "QuitText");
                }
            }
        }

        private bool _hasBaseNetworkComponentSet;

        private BaseNetworkComponent _baseNetworkComponent;

        private MultiplayerIntermissionState _currentIntermissionState;

        private readonly TextObject _voteLabelText = new TextObject("{=KOVHgkVq}Voting Ends In:", null);

        private readonly TextObject _nextGameLabelText = new TextObject("{=lX9Qx7Wo}Next Game Starts In:", null);

        private readonly TextObject _serverIdleLabelText = new TextObject("{=Rhcberxf}Awaiting Server", null);

        private readonly TextObject _matchFinishedText = new TextObject("{=RbazQjFt}Match is Finished", null);

        private readonly TextObject _returningToLobbyText = new TextObject("{=1UaxKbn6}Returning to the Lobby...", null);

        private MPIntermissionMapItemVM _votedMapItem;

        private MPIntermissionCultureItemVM _votedCultureItem;

        private string _connectedPlayersCountValueText;

        private string _maxNumPlayersValueText;

        private bool _isFactionAValid;

        private bool _isFactionBValid;

        private bool _isMissionTimerEnabled;

        private bool _isEndGameTimerEnabled;

        private bool _isNextMapInfoEnabled;

        private bool _isMapVoteEnabled;

        private bool _isCultureVoteEnabled;

        private bool _isPlayerCountEnabled;

        private string _nextMapId;

        private string _nextFactionACultureId;

        private string _nextFactionBCultureId;

        private string _nextGameStateTimerLabel;

        private string _nextGameStateTimerValue;

        private string _playersLabel;

        private string _mapVoteText;

        private string _cultureVoteText;

        private string _serverName;

        private string _welcomeMessage;

        private string _nextGameType;

        private string _nextMapName;

        private string _quitText;

        private MBBindingList<MPIntermissionMapItemVM> _availableMaps;

        private MBBindingList<MPIntermissionCultureItemVM> _availableCultures;
    }
}