using System;

namespace CNMultiplayer.Common.Gold
{
    [Flags]
    public enum GoldGainFlags : ushort
    {
        HeadShot = 1, //爆头
        DefaultKill = 2, //击杀
        FirstKill = 4, //首杀
        DoubleKill = 8, //双杀
        FifthKill = 16, //五杀
        TenthKill = 32, //十杀
        DefaultAssist = 64, //助攻
        ScrambleFlag = 128, //争夺旗帜
        FlagRemove = 256, //旗帜移除
        ObjectiveCompleted = 512, //目标完成
        ObjectiveDestroyed = 1024, //目标摧毁
        PerkBonus = 2048, //Perk
    }
}
