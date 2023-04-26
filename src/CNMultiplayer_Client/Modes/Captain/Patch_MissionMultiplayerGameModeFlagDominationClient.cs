using HarmonyLib;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace HarmonyPatches
{
    internal class Patch_MissionMultiplayerGameModeFlagDominationClient
    {
        [HarmonyPatch(typeof(MissionMultiplayerGameModeFlagDominationClient), "OnClearScene")]
        internal class Patch_OnClearScene
        {
            public static void Postfix()
            {
                SetNewNumOfBotsPerFormation();
            }
        }

        private static void SetNewNumOfBotsPerFormation()
        {
            // 动态带兵数量，在原版1~3倍兵力间浮动，计算公式1500/（总玩家数 + 总AI数）
            int playerCount = GetCurrentPlayerCount();
            int botCount = MultiplayerOptions.OptionType.NumberOfBotsTeam1.GetIntValue() + MultiplayerOptions.OptionType.NumberOfBotsTeam2.GetIntValue();
            int newNumOfBotsPerFormation = (int)MathF.Clamp(CNMCaptainSpawningBehavior.CNMCaptainSumOfAgents / (playerCount + botCount), 25, CNMCaptainSpawningBehavior.InitialNumOfBotsPerFormation);
            MultiplayerOptions.OptionType.NumberOfBotsPerFormation.SetValue(newNumOfBotsPerFormation);
        }

        private static int GetCurrentPlayerCount()
        {
            int num = 0;
            foreach (NetworkCommunicator networkCommunicator in GameNetwork.NetworkPeers)
            {
                MissionPeer component = networkCommunicator.GetComponent<MissionPeer>();
                if (networkCommunicator.IsSynchronized && component != null && component.Team != null && component.Team.Side != BattleSideEnum.None)
                {
                    num++;
                }
            }
            return num;
        }
    }
}
