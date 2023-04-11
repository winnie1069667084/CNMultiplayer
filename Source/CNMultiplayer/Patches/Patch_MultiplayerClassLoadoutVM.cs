using TaleWorlds.MountAndBlade.ViewModelCollection.Multiplayer.ClassLoadout;
using HarmonyLib;
using TaleWorlds.MountAndBlade;

namespace Patches
{
    [HarmonyPatch(typeof(MultiplayerClassLoadoutVM), "Tick")]//调用兵种锁定
    internal class Patch_Tick
    {
        public static void Postfix(MultiplayerClassLoadoutVM __instance)
        {
            if (MultiplayerOptions.OptionType.GameType.GetStrValue(MultiplayerOptions.MultiplayerOptionsAccessMode.CurrentMapOptions) != "Siege" || !GameNetwork.IsClient)
                return;
            foreach (HeroClassGroupVM heroClassGroupVM in __instance.Classes)
            {
                heroClassGroupVM.SubClasses.ApplyActionOnAllItems(delegate (HeroClassVM sc)
                {
                    sc.UpdateEnabled();
                });
            }
        }
    }
}
