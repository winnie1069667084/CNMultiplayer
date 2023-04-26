using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace CNMultiplayer.Common.Network
{
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
    public sealed class SetNumOfBotsPerFormation : GameNetworkMessage
    {
        public int NumOfBotsPerFormation { get; private set; }

        //TW的奇怪规则，NetworkMessage类必须有一个空的构造函数才能被正常使用
        public SetNumOfBotsPerFormation()
        {
        }

        public SetNumOfBotsPerFormation(int numOfBotsPerFormation)
        {
            NumOfBotsPerFormation = numOfBotsPerFormation;
        }

        protected override void OnWrite()
        {
            GameNetworkMessage.WriteIntToPacket(NumOfBotsPerFormation, CompressionBasic.NumberOfBotsPerFormationCompressionInfo);
        }

        protected override bool OnRead()
        {
            bool flag = true;
            NumOfBotsPerFormation = GameNetworkMessage.ReadIntFromPacket(CompressionBasic.NumberOfBotsPerFormationCompressionInfo, ref flag);
            return flag;
        }

        protected override MultiplayerMessageFilter OnGetLogFilter()
        {
            return MultiplayerMessageFilter.Mission;
        }

        protected override string OnGetLogFormat()
        {
            return "Syncing number of bots per formation";
        }
    }
}
