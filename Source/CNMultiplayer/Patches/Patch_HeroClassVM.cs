using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.ViewModelCollection.Multiplayer.ClassLoadout;
using HarmonyLib;
using System.Reflection;
using static TaleWorlds.MountAndBlade.MultiplayerClassDivisions;
using System.Linq;

namespace Patches
{
    [HarmonyPatch(typeof(HeroClassVM), "UpdateEnabled")]//根据战场兵种比例锁定兵种，限定射手、骑兵、骑射手分别不超过总兵力的1/4。
    internal class Patch_UpdateEnabled
    {
        public static bool Prefix(HeroClassVM __instance, MissionMultiplayerGameModeBaseClient ____gameMode)
        {
            if (MultiplayerOptions.OptionType.GameType.GetStrValue(MultiplayerOptions.MultiplayerOptionsAccessMode.CurrentMapOptions) != "Siege")
            { return true; }
            bool flag = true;
            MissionPeer component = GameNetwork.MyPeer.GetComponent<MissionPeer>();
            int Sum = GetTroopTypeCountForTeam(component.Team)[0];
            int Infantry = GetTroopTypeCountForTeam(component.Team)[1];
            int Ranged = GetTroopTypeCountForTeam(component.Team)[2];
            int Cavalry = GetTroopTypeCountForTeam(component.Team)[3];
            int HorseArcher = GetTroopTypeCountForTeam(component.Team)[4];
            string Id = __instance.TroopTypeId;
            if ((Id == "Ranged" && Ranged > Sum / 4) || (Id == "Cavalry" && Cavalry > Sum / 4) || (Id == "HorseArcher" && HorseArcher > Sum / 4))
            {
                flag = false;
                if (__instance.IsSelected)
                    __instance.IsSelected = false;
            }
            __instance.IsEnabled = ____gameMode.IsInWarmup || !____gameMode.IsGameModeUsingGold || (____gameMode.GetGoldAmount() >= __instance.Gold && flag);
            return false;
        }

        public static int[] GetTroopTypeCountForTeam(Team team)//统计某方战场存活兵种数，0总数；1步兵；2射手；3骑兵；4骑射
        {
            int[] num = new int[5];
            foreach (NetworkCommunicator networkPeer in GameNetwork.NetworkPeers)
            {
                MissionPeer component = networkPeer.GetComponent<MissionPeer>();
                if (component?.Team != null && component.Team == team && component.IsControlledAgentActive)
                {
                    BasicCharacterObject Character = MultiplayerClassDivisions.GetMPHeroClassForPeer(component).HeroCharacter;
                    num[0]++;   
                    if (Character.IsInfantry)
                    {
                        num[1]++;
                        continue;
                    }
                    if (Character.IsRanged)
                        num[2]++;
                    if (Character.IsMounted)
                        num[3]++;
                    if (Character.IsRanged && Character.IsMounted)
                        num[4]++;
                }
            }
            num[2] -= num[4];
            num[3] -= num[4];
            return num;
        }
    }
}
