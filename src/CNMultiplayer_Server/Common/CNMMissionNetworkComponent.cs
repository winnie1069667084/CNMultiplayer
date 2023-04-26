using CNMultiplayer.Common.Network;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace CNMultiplayer.Common
{
    internal class CNMMissionNetworkComponent : MissionNetwork
    {
        protected override void AddRemoveMessageHandlers(GameNetwork.NetworkMessageHandlerRegistererContainer registerer)
        {
            base.AddRemoveMessageHandlers(registerer);
            if (GameNetwork.IsClientOrReplay)
            {
                registerer.Register<SetNumOfBotsPerFormation>(new GameNetworkMessage.ServerMessageHandlerDelegate<SetNumOfBotsPerFormation>(HandleServerEventSetNumOfBotsPerFormation));
            }
        }

        private void HandleServerEventSetNumOfBotsPerFormation(SetNumOfBotsPerFormation message)
        {
            MultiplayerOptions.OptionType.NumberOfBotsPerFormation.SetValue(message.NumOfBotsPerFormation);
        }
    }
}
