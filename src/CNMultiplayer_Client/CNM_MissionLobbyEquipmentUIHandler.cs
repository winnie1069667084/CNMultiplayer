using System.Reflection;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.GauntletUI.Mission.Multiplayer;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.MissionViews.Multiplayer;
using TaleWorlds.MountAndBlade.ViewModelCollection.Multiplayer.ClassLoadout;

namespace CNMultiplayer
{
    [OverrideView(typeof(MissionLobbyEquipmentUIHandler))]
    public class CNM_MissionLobbyEquipmentUIHandler : MissionGauntletClassLoadout
    {
        public override void OnAgentBuild(Agent agent, Banner banner)//在有agent生成或死亡时统计被锁定的兵种(agent为队友且不是AI)
        {
            base.OnAgentBuild(agent, banner);
            // Reflection to retrieve private field _dataSource
            MultiplayerClassLoadoutVM dataSource = (MultiplayerClassLoadoutVM)typeof(MissionGauntletClassLoadout).GetField("_dataSource", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(this);
            // OnGoldUpdated is calling HeroClassVM.UpdateEnabled()
            if (agent.Team == GameNetwork.MyPeer.GetComponent<MissionPeer>().Team && !agent.IsAIControlled)
                dataSource?.OnGoldUpdated();
        }

        public override void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow blow)
        {
            base.OnAgentRemoved(affectedAgent, affectorAgent, agentState, blow);
            // Reflection to retrieve private field _dataSource
            MultiplayerClassLoadoutVM dataSource = (MultiplayerClassLoadoutVM)typeof(MissionGauntletClassLoadout).GetField("_dataSource", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(this);
            // OnGoldUpdated is calling HeroClassVM.UpdateEnabled()
            if (affectedAgent.Team == GameNetwork.MyPeer.GetComponent<MissionPeer>().Team && !affectedAgent.IsAIControlled)
                dataSource?.OnGoldUpdated();
        }
    }
}
