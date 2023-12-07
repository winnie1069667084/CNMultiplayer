using CNMultiplayer.Common;
using CNMultiplayer.Helpers;
using System;
using System.Linq;
using System.Reflection;
using TaleWorlds.MountAndBlade;

namespace CNMultiplayer.Modes.Warmup
{
    /// <summary>
    /// Custom warmup component so we can load the <see cref="CNMSpawningBehaviorBase"/> as soon as warmup ends.
    /// </summary>
    internal class CNMWarmupComponent : MultiplayerWarmupComponent
    {
        private static readonly FieldInfo WarmupStateField = typeof(MultiplayerWarmupComponent).GetField("_warmupState", BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly FieldInfo TimerComponentField = typeof(MultiplayerWarmupComponent).GetField("_timerComponent", BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly FieldInfo GameModeField = typeof(MultiplayerWarmupComponent).GetField("_gameMode", BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly FieldInfo LobbyComponentField = typeof(MultiplayerWarmupComponent).GetField("_lobbyComponent", BindingFlags.NonPublic | BindingFlags.Instance);

        private readonly Func<(SpawnFrameBehaviorBase, SpawningBehaviorBase)> _createSpawnBehaviors;

        public CNMWarmupComponent(Func<(SpawnFrameBehaviorBase, SpawningBehaviorBase)> createSpawnBehaviors)
        {
            _createSpawnBehaviors = createSpawnBehaviors;
        }

        private WarmupStates WarmupStateReflection
        {
            get => (WarmupStates)WarmupStateField.GetValue(this);
            set => WarmupStateField.SetValue(this, value);
        }

        private MultiplayerTimerComponent TimerComponentReflection => (MultiplayerTimerComponent)TimerComponentField.GetValue(this);
        private MissionMultiplayerGameModeBase GameModeReflection => (MissionMultiplayerGameModeBase)GameModeField.GetValue(this);
        private MissionLobbyComponent LobbyComponentReflection => (MissionLobbyComponent)LobbyComponentField.GetValue(this);

        public override void OnPreDisplayMissionTick(float dt)
        {
            if (!GameNetwork.IsServer)
            {
                return;
            }

            switch (WarmupStateReflection)
            {
                case WarmupStates.WaitingForPlayers:
                    BeginWarmup();
                    break;
                case WarmupStates.InProgress:
                    if (CheckForWarmupProgressEnd())
                        EndWarmupProgress();

                    break;
                case WarmupStates.Ending:
                    if (TimerComponentReflection.CheckIfTimerPassed())
                        EndWarmup();

                    break;
                case WarmupStates.Ended:
                    if (TimerComponentReflection.CheckIfTimerPassed())
                        Mission.RemoveMissionBehavior(this);

                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void BeginWarmup()
        {
            WarmupStateReflection = WarmupStates.InProgress;
            Mission.ResetMission();
            GameModeReflection.MultiplayerTeamSelectComponent.BalanceTeams();
            TimerComponentReflection.StartTimerAsServer(TotalWarmupDuration);
            GameModeReflection.SpawnComponent.SpawningBehavior.Clear();
            SpawnComponent spawnComponent = Mission.GetMissionBehavior<SpawnComponent>();
            spawnComponent.SetNewSpawnFrameBehavior(new FFASpawnFrameBehavior());
            spawnComponent.SetNewSpawningBehavior(new CNMWarmupSpawningBehavior());
        }

        private new void EndWarmupProgress()
        {
            WarmupStateReflection = WarmupStates.Ending;
            TimerComponentReflection.StartTimerAsServer(30f);
            ReflectionHelper.RaiseEvent(this, nameof(OnWarmupEnding), Array.Empty<object>());
        }

        private void EndWarmup()
        {
            WarmupStateReflection = WarmupStates.Ended;
            TimerComponentReflection.StartTimerAsServer(3f);
            ReflectionHelper.RaiseEvent(this, nameof(OnWarmupEnded), Array.Empty<object>());

            if (GameNetwork.NetworkPeers.Count() < MultiplayerOptions.OptionType.MinNumberOfPlayersForMatchStart.GetIntValue())
            {
                LobbyComponentReflection.SetStateEndingAsServer();
                return;
            }

            if (!GameNetwork.IsDedicatedServer)
            {
                ReflectionHelper.InvokeMethod(this, "PlayBattleStartingSound", Array.Empty<object>());
            }

            Mission.Current.ResetMission();
            GameModeReflection.MultiplayerTeamSelectComponent.BalanceTeams();
            GameModeReflection.SpawnComponent.SpawningBehavior.Clear();
            SpawnComponent spawnComponent = Mission.GetMissionBehavior<SpawnComponent>();
            (SpawnFrameBehaviorBase spawnFrame, SpawningBehaviorBase spawning) = _createSpawnBehaviors();
            spawnComponent.SetNewSpawnFrameBehavior(spawnFrame);
            spawnComponent.SetNewSpawningBehavior(spawning);
        }
    }

}