using HarmonyLib;
using NetworkMessages.FromServer;
using System.Collections.Generic;
using System;
using System.Reflection;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.MissionRepresentatives;

[HarmonyPatch(typeof(MissionMultiplayerSiegeClient), "OnNumberOfFlagsChanged")]//�Ƴ����Ƿ��Ƴ����ĵĽ�ҽ���
internal class Patch_OnNumberOfFlagsChanged
{
    public static bool Prefix(Action ___OnFlagNumberChangedEvent)
    {
        Action onFlagNumberChangedEvent = ___OnFlagNumberChangedEvent;
        if (onFlagNumberChangedEvent != null)
        {
            onFlagNumberChangedEvent();
        }
        return false;
    }
}