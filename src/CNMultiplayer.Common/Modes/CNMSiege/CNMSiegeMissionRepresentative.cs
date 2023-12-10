using NetworkMessages.FromServer;
using System.Collections.Generic;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade;
using TaleWorlds.Library;

namespace CNMultiplayer.Common.Modes.CNMSiege
{
    public class CNMSiegeMissionRepresentative : MissionRepresentativeBase
    {
        private const int minimumKillGold = 2; //最少差价击杀

        private const int maximumKillGold = 20; //最多差价击杀

        private const int firstMeleeKillGold = 10; //首次近战击杀

        private const int firstRangeKillGold = 5; //首次远程击杀

        private const int fifthKillGold = 5; //五杀

        private const int tenthKillGold = 10; //十杀

        private const int firstAssistGold = 5; //第一助攻

        private const int secondAssistGold = 3; //第二助攻

        private const int thirdAssistGold = 2; //第三助攻

        private const int defaultAssistGold = 1; //默认助攻

        private const int defaultDestructableGold = 50; //默认可破坏物（投石车、弩炮）

        private const int gateAndWallGold = 300; //大门与可破坏墙

        private const int ramAndTowerGold = 150; //攻城槌与攻城塔

        private const int otherDestructableGold = 300; //其它

        private GoldGainFlags _currentGoldGains;

        private int _killCountOnSpawn;

        private int _assistCountOnSpawn;

        public int GetGoldAmountForVisual()
        {
            if (base.Gold < 0)
            {
                return 80;
            }
            return base.Gold;
        }

        public override void OnAgentSpawned()
        {
            this._currentGoldGains = (GoldGainFlags)0;
            this._killCountOnSpawn = base.MissionPeer.KillCount;
            this._assistCountOnSpawn = base.MissionPeer.AssistCount;
        }

        public int GetGoldGainsFromKillDataAndUpdateFlags(MPPerkObject.MPPerkHandler killerPerkHandler, MPPerkObject.MPPerkHandler assistingHitterPerkHandler, MultiplayerClassDivisions.MPHeroClass victimClass, bool isAssist, bool isRanged, bool isFriendly)
        {
            int num = 0;
            List<KeyValuePair<ushort, int>> list = new List<KeyValuePair<ushort, int>>();
            if (isAssist)
            {
                if (!isFriendly)
                {
                    int num3 = ((killerPerkHandler != null) ? killerPerkHandler.GetRewardedGoldOnAssist() : 0) + ((assistingHitterPerkHandler != null) ? assistingHitterPerkHandler.GetGoldOnAssist() : 0);
                    if (num3 > 0)
                    {
                        num += num3;
                        this._currentGoldGains |= GoldGainFlags.PerkBonus;
                        list.Add(new KeyValuePair<ushort, int>(2048, num3));
                    }
                }
                switch (base.MissionPeer.AssistCount - this._assistCountOnSpawn)
                {
                    case 1:
                        num += firstAssistGold;
                        this._currentGoldGains |= GoldGainFlags.FirstAssist;
                        list.Add(new KeyValuePair<ushort, int>(4, firstAssistGold));
                        break;
                    case 2:
                        num += secondAssistGold;
                        this._currentGoldGains |= GoldGainFlags.SecondAssist;
                        list.Add(new KeyValuePair<ushort, int>(8, secondAssistGold));
                        break;
                    case 3:
                        num += thirdAssistGold;
                        this._currentGoldGains |= GoldGainFlags.ThirdAssist;
                        list.Add(new KeyValuePair<ushort, int>(16, thirdAssistGold));
                        break;
                    default:
                        num += defaultAssistGold;
                        list.Add(new KeyValuePair<ushort, int>(256, defaultAssistGold));
                        break;
                }
            }
            else
            {
                int num4 = 0;
                if (base.ControlledAgent != null)
                {
                    num4 = MultiplayerClassDivisions.GetMPHeroClassForCharacter(base.ControlledAgent.Character).TroopCasualCost;
                    int num5 = victimClass.TroopCasualCost - num4; //根据击杀兵种金币差距奖励金币
                    int num6 = MBMath.ClampInt(num5 / 10, minimumKillGold, maximumKillGold);
                    num += num6;
                    list.Add(new KeyValuePair<ushort, int>(128, num6));
                }
                int num7 = ((killerPerkHandler != null) ? killerPerkHandler.GetGoldOnKill((float)num4, (float)victimClass.TroopCasualCost) : 0);
                if (num7 > 0)
                {
                    num += num7;
                    this._currentGoldGains |= GoldGainFlags.PerkBonus;
                    list.Add(new KeyValuePair<ushort, int>(2048, num7));
                }
                int num8 = base.MissionPeer.KillCount - this._killCountOnSpawn;
                if (num8 != 5)
                {
                    if (num8 == 10)
                    {
                        num += tenthKillGold;
                        this._currentGoldGains |= GoldGainFlags.TenthKill;
                        list.Add(new KeyValuePair<ushort, int>(64, tenthKillGold));
                    }
                }
                else
                {
                    num += fifthKillGold;
                    this._currentGoldGains |= GoldGainFlags.FifthKill;
                    list.Add(new KeyValuePair<ushort, int>(32, fifthKillGold));
                }
                if (isRanged && !this._currentGoldGains.HasAnyFlag(GoldGainFlags.FirstRangedKill))
                {
                    num += firstRangeKillGold;
                    this._currentGoldGains |= GoldGainFlags.FirstRangedKill;
                    list.Add(new KeyValuePair<ushort, int>(1, firstRangeKillGold));
                }
                if (!isRanged && !this._currentGoldGains.HasAnyFlag(GoldGainFlags.FirstMeleeKill))
                {
                    num += firstMeleeKillGold;
                    this._currentGoldGains |= GoldGainFlags.FirstMeleeKill;
                    list.Add(new KeyValuePair<ushort, int>(2, firstMeleeKillGold));
                }
            }
            int num9 = 0;
            if (base.MissionPeer.Team == Mission.Current.Teams.Attacker)
            {
                num9 = MultiplayerOptions.OptionType.GoldGainChangePercentageTeam1.GetIntValue(MultiplayerOptions.MultiplayerOptionsAccessMode.CurrentMapOptions);
            }
            else if (base.MissionPeer.Team == Mission.Current.Teams.Defender)
            {
                num9 = MultiplayerOptions.OptionType.GoldGainChangePercentageTeam2.GetIntValue(MultiplayerOptions.MultiplayerOptionsAccessMode.CurrentMapOptions);
            }
            if (num9 != 0 && (num > 0 || list.Count > 0))
            {
                num = 0;
                float num10 = 1f + (float)num9 * 0.01f;
                for (int i = 0; i < list.Count; i++)
                {
                    int num11 = (int)((float)list[i].Value * num10);
                    list[i] = new KeyValuePair<ushort, int>(list[i].Key, num11);
                    num += num11;
                }
            }
            if (list.Count > 0 && !base.Peer.Communicator.IsServerPeer && base.Peer.Communicator.IsConnectionActive)
            {
                GameNetwork.BeginModuleEventAsServer(base.Peer);
                GameNetwork.WriteMessage(new GoldGain(list));
                GameNetwork.EndModuleEventAsServer();
            }
            return num;
        }

        public int GetGoldGainsFromObjectiveAssist(GameEntity objectiveMostParentEntity, float contributionRatio, bool isCompleted)
        {
            int num = (int)(contributionRatio * (float)this.GetTotalGoldDistributionForDestructable(objectiveMostParentEntity));
            if (num > 0 && !base.Peer.Communicator.IsServerPeer && base.Peer.Communicator.IsConnectionActive)
            {
                GameNetwork.BeginModuleEventAsServer(base.Peer);
                GameNetwork.WriteMessage(new GoldGain(new List<KeyValuePair<ushort, int>>
                {
                    new KeyValuePair<ushort, int>((ushort)(isCompleted ? 512 : 1024), num)
                }));
                GameNetwork.EndModuleEventAsServer();
            }
            return num;
        }

        public int GetGoldGainsFromAllyDeathReward(int baseAmount)
        {
            if (baseAmount > 0 && !base.Peer.Communicator.IsServerPeer && base.Peer.Communicator.IsConnectionActive)
            {
                GameNetwork.BeginModuleEventAsServer(base.Peer);
                GameNetwork.WriteMessage(new GoldGain(new List<KeyValuePair<ushort, int>>
                {
                    new KeyValuePair<ushort, int>(2048, baseAmount)
                }));
                GameNetwork.EndModuleEventAsServer();
            }
            return baseAmount;
        }

        private int GetTotalGoldDistributionForDestructable(GameEntity objectiveMostParentEntity)
        {
            string text = null;
            foreach (string text2 in objectiveMostParentEntity.Tags)
            {
                if (text2.StartsWith("mp_siege_objective_"))
                {
                    text = text2;
                    break;
                }
            }
            if (text == null)
            {
                return defaultDestructableGold;
            }
            string text3 = text.Replace("mp_siege_objective_", "");
            if (text3 == "wall_breach" || text3 == "castle_gate")
            {
                return gateAndWallGold;
            }
            if (!(text3 == "battering_ram") && !(text3 == "siege_tower"))
            {
                return ramAndTowerGold;
            }
            return otherDestructableGold;
        }
    }
}
