using System.Reflection;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade.GauntletUI.Mission.Multiplayer;
using TaleWorlds.MountAndBlade.View.MissionViews.Multiplayer;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.ViewModelCollection.Multiplayer.ClassLoadout;
using TaleWorlds.MountAndBlade;

namespace CNMultiplayer
{
    [OverrideView(typeof(MissionLobbyEquipmentUIHandler))]
    public class CNM_MissionLobbyEquipmentUIHandler : MissionGauntletClassLoadout
    {
        public override void OnAgentBuild(Agent agent, Banner banner)
        {
            base.OnAgentBuild(agent, banner);
            // Reflection to retrieve private field _dataSource
            MultiplayerClassLoadoutVM dataSource = (MultiplayerClassLoadoutVM)typeof(MissionGauntletClassLoadout).GetField("_dataSource", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(this);
            // OnGoldUpdated is calling HeroClassVM.UpdateEnabled()
            dataSource?.OnGoldUpdated();
        }
    }
}
