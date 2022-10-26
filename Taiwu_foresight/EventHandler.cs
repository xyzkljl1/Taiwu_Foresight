using GameData.Domains.TaiwuEvent.DisplayEvent;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Taiwu_Foresight
{
    public partial class Foresight
    {
        public static string LeaveNestTrue = "关闭巢穴(无奖励)";
        public static string LeaveNestFalse = "离开巢穴(不关闭)";
        public static string Nothing = "无事发生";
        public static string StartCombat = "开战";

        public unsafe static string GetConquerNestText()
        {
            var enemyNestCfg = Config.EnemyNest.Instance.First((Config.EnemyNestItem nest) => nest.AdventureId == currentAdventure.TemplateId);
            return ToInfo("征服巢穴")
                +ToInfo(enemyNestCfg.TipDesc);//似乎没有额外奖励
        }
        //摧毁巢穴，根据性格获得奖励，性格为-1时不会获得任何一种性格对应的奖励，但仍有其它奖励
        public unsafe static string GetDestroyNestText(sbyte behaviorType, int infoLevel = 1)
        {
            var result = ToInfo("摧毁巢穴\n", infoLevel);
            //AdventureDomain.DestroyEnemyNest
            if (enemyNestCfg != null)
            {
                //UnityEngine.Debug.Log($"Foresight:外道{enemyNestCfg.TipDesc} {enemyNestCfg.TipTitle}");
                Config.Character instance = Config.Character.Instance;
                List<short> members = enemyNestCfg.Members;
                int index = members.Count - 1;
                Config.CharacterItem enemyLeaderCfg = instance[members[index]];
                result += ToInfo($"获得{enemyNestCfg.MoneyReward}金钱", infoLevel);
                result += ToInfo($"获得{enemyNestCfg.AuthorityReward}威望", infoLevel);
                result += ToInfo($"获得{enemyNestCfg.ExpReward}经验", infoLevel);

                Config.LegacyPointItem configData = Config.LegacyPoint.Instance[13];//遗惠
                result += ToInfo($"获得{configData.BasePoint}(加成前)生平遗惠", infoLevel);
                result += ToInfo($"获得{(enemyNestCfg.SpiritualDebtChange/10).ToString("f1")}地区恩义", infoLevel);
                switch (behaviorType)
                {
                    case 0:
                        {
                            Config.FameActionItem template = Config.FameAction.Instance[41];
                            result += ToInfo($"获得{template.Name}名声", infoLevel);
                            result += ToInfo("当前地区所有城镇安定+1", infoLevel);
                        }
                        break;
                    case 1:
                        {
                            Config.FameActionItem template = Config.FameAction.Instance[38];
                            result += ToInfo($"获得{template.Name}名声", infoLevel);
                            result += ToInfo("当前地区所有城镇文化+1", infoLevel);
                        }
                        break;
                    case 2:
                        {
                            result += ToInfo("当前地区所有城镇安定+1", infoLevel);
                            result += ToInfo("当前地区所有城镇文化+1", infoLevel);
                        }
                        break;
                    case 3:
                        {
                            Config.FameActionItem template = Config.FameAction.Instance[42];
                            result += ToInfo($"获得{template.Name}名声", infoLevel);
                            result += ToInfo("心情+3", infoLevel);
                            int moneyGain = enemyLeaderCfg.Resources.Items[6];
                            result += ToInfo($"获得{moneyGain * 5}~{moneyGain * 10 + 1}金钱", infoLevel);
                        }
                        break;
                    case 4:
                        {
                            Config.FameActionItem template = Config.FameAction.Instance[44];
                            result += ToInfo($"获得{template.Name}名声", infoLevel);
                            result += ToInfo($"获得同道", infoLevel);
                        }
                        break;
                }

            }
            return result;
        }

        public static string Standard_MultiCheckOrGiveUp(int idx, EventOptionInfo eventOptionInfo, object[] paras)//可从N个选项中选一个进行检测或放弃，失败了重来直到全部失
        {
            if (eventOptionInfo.OptionKey == currEvent.EventOptionInfos.Last().OptionKey)//最后一项
                return ToInfo("如同检测全部失败");
            if (paras != null && paras.Count() >= 3)
                return ToInfo($"{paras[0] as string}检测")
                    + ToInfo($"成功:{paras[1] as string}")
                    + ToInfo($"失败:重新选择")
                    + ToInfo($"全部失败:{paras[2] as string}");
            return "远见挂了！";
        }


        public static string Standard_Check2(int idx, EventOptionInfo eventOptionInfo, object[] paras)//进行XX检测，成功和失败分支的事件
        {
            if(paras!=null&&paras.Count()>=3)
                return ToInfo($"{paras[0] as string}检测")
                    +ToInfo($"成功:{paras[1] as string}")
                    +ToInfo($"失败:{paras[2] as string}");
            return "远见挂了！";
        }
        public static string Standard_Check2OrOther(int idx, EventOptionInfo eventOptionInfo, object[] paras)//可选进行XX检测或其它的事件
        {
            if (paras != null && paras.Count() >= 4)
                if (idx == 1)
                    return ToInfo(paras[3] as string);
                else
                    return Standard_Check2(idx, eventOptionInfo, paras);
            return "远见挂了！";
        }

        public static string Standard_Simple(int idx, EventOptionInfo eventOptionInfo,  object[] paras)//有若干选项，各自为固定文本的简单选项
        {
            if(paras!=null&&paras.Count()>idx)
                return ToInfo(paras[idx] as string);
            return "远见挂了！";
        }

        public static string Standard_AllSame(int idx, EventOptionInfo eventOptionInfo,  object[] paras)
        {
            return ToInfo("别看了,都一样");
        }
        public static string Standard_Destroy(int idx, EventOptionInfo eventOptionInfo, object[] paras)//六个选项的通用摧毁巢穴选项
        {
            //选项0-4对应5个性格，5对应逃跑即-1
            return GetDestroyNestText(idx< 5 ? (sbyte) idx : (sbyte)-1);
        }
        public static string Standard_ConqOrDestroy(int idx, EventOptionInfo eventOptionInfo, object[] paras)//征服或摧毁
        {
                if (idx == 0)//征服
                    return GetConquerNestText();
                else
                    return ToInfo("摧毁巢穴");
        }
        public static string Standard_ConqOrDestroyCombat(int idx, EventOptionInfo eventOptionInfo, object[] paras)//征服或摧毁(战前)
        {
            if (idx == 0)//征服
                return GetConquerNestText();
            else
                return ToInfo(StartCombat)+ToInfo("摧毁巢穴");
        }
        public static string Standard_ConqSame(int idx, EventOptionInfo eventOptionInfo, object[] paras)//N个选项全是康壳
        {
           return GetConquerNestText()+ ToInfo("别看了，所有选项都一样");
        }
        public static string Standard_CombatConqSame(int idx, EventOptionInfo eventOptionInfo,  object[] paras)//N个选项全是开战康壳
        {
            return ToInfo(StartCombat)
                +GetConquerNestText()
                + ToInfo("别看了，所有选项都一样");
        }
        public static string Standard_ConqOrDestroy_Delay(int idx, EventOptionInfo eventOptionInfo, object[] paras)//征服前听bossBB两句
        {
            if (idx == 0)//征服
                return ToInfo("征服或摧毁巢穴");
            else
                return ToInfo("摧毁巢穴");
        }
        //贼人营寨
        public static string Zeiren_Pickmoney(int idx, EventOptionInfo eventOptionInfo,  object[] paras)//捡钱
        {
            if (idx == 0)
                return ToInfo(Nothing);
            else
            {
                return ToInfo("获得随机金钱")
                    +ToInfo($"50%:{Nothing}")
                    +ToInfo($"50%:被察觉，可能开战、带路或离开巢穴");
            }
        }
        public static string Zeiren_PickmoneyCombat(int idx, EventOptionInfo eventOptionInfo,  object[] paras)//捡钱后开战
        {
            if (idx == 0||idx==1)
                return ToInfo(StartCombat)
                    +ToInfo("胜利:可选带路");
            else
            {
                return ToInfo(LeaveNestFalse);
            }
        }
        //迷香阵
        public static string MiXiang_QiDisorder3(int idx, EventOptionInfo eventOptionInfo, object[] paras)//内息三项
        {
            var degree = GetAdventureParameter($"neiQiDegree{idx + 1}");
            //SolorTerm:节气
            return ToInfo($"内息紊乱增加{(degree/10).ToString("f2")}")
                +ToInfo("可被节气效果减轻",2);
        }
        public static string MiXiang_QiDisorder2(int idx, EventOptionInfo eventOptionInfo,  object[] paras)//内息两项
        {
            var degree = GetAdventureParameter(idx == 0 ? "neiQiDegree1": "neiQiDegree3");
            //SolorTerm:节气
            return ToInfo($"内息紊乱增加{(degree / 10).ToString("f2")}")
                + ToInfo("可被节气效果减轻", 2);
        }
        //邪人死地
        public static string Xieren_FirstMet(int idx, EventOptionInfo eventOptionInfo, object[] paras)
        {
            return ToInfo("开战")
                + ToInfo("关押：关闭巢穴(无奖励)", 2)
                + ToInfo("胜利：摧毁巢穴", 2)
                + ToInfo("逃跑：遇到npc并触发剧情", 2);
        }
        public static string Xieren_TrickChoose(int idx, EventOptionInfo eventOptionInfo, object[] paras)
        {
            if (idx < 5)
                return ToInfo("解除该属性功法的限制")
                    + ToInfo("在下个拐点再战boss");
            else
                return ToInfo("在接下来的每个拐点再战boss")
                    +ToInfo("逃跑:前进")
                    +ToInfo("胜利:摧毁巢穴")
                    + ToInfo("(如果道中未胜利)在终点解除限制并再战boss");
        }
        //修罗
        public static string Xiuluo_StartChoose(int idx, EventOptionInfo eventOptionInfo, object[] paras)
        {
            if (idx == 0)
                return
                    ToInfo("可摧毁巢穴")
                    + ToInfo("记录杀人数(处决=三杀)")
                    +ToInfo("拐点1检测杀人计数")
                    +ToInfo("<4:无事发生",2)
                    +ToInfo("否则:和入魔人开战,胜利可摧毁巢穴", 2)
                    + ToInfo("拐点2检测杀人计数")
                    + ToInfo("<6:无事发生",2)
                    + ToInfo("否则:和入魔人开战,胜利可摧毁巢穴", 2)
                    + ToInfo("终点检测杀人计数")
                    + ToInfo("<8:和炼心师开战,胜利可摧毁巢穴", 2)
                    + ToInfo("否则:和入魔人开战,胜利可摧毁巢穴", 2);
            else
                return ToInfo("限制两种真气凝聚(我也不知道啥叫限制凝聚)")
                    +ToInfo("终点和炼心师开战")
                    +ToInfo("可征服/摧毁巢穴");
        }

        //弃世绝境
        public static string QiShi_FinalMultiCheck(int idx, EventOptionInfo eventOptionInfo, object[] paras)//终点征服或摧毁
        {
            if (eventOptionInfo.OptionKey == currEvent.EventOptionInfos.Last().OptionKey)//最后一项
            {
                return
                    ToInfo(StartCombat)
                    + GetDestroyNestText(-1,1);
            }
            else
                return ToInfo("检测真气")
                    +ToInfo("成功:开战并征服巢穴",2)
                    +ToInfo("失败:可重新选择",2)
                    +ToInfo("全部失败:开战并摧毁巢穴",2)
                    +GetDestroyNestText(-1,3);
        }
        public static string QiShi_MiddleMultiCheck(int idx, EventOptionInfo eventOptionInfo, object[] paras)//道中跟踪或摧毁
        {
            if (eventOptionInfo.OptionKey == currEvent.EventOptionInfos.Last().OptionKey)//最后一项
            {
                return
                    ToInfo(StartCombat)
                    + GetDestroyNestText(-1, 1);
            }
            else
                return ToInfo("检测该项武学造诣")
                    + ToInfo("成功:无事发生", 2)
                    + ToInfo("失败:可重新选择", 2)
                    + ToInfo("全部失败:开战并摧毁巢穴", 2)
                    + GetDestroyNestText(-1, 3);
        }
        public static string QiShi_MiddleSingleCheck(int idx, EventOptionInfo eventOptionInfo, object[] paras)//道中跟踪或摧毁
        {

            if (idx==1)
            {
                return
                    ToInfo(StartCombat)
                    + GetDestroyNestText(-1, 1);
            }
            else
            {
                var d0 = GetAdventureParameter("D0");
                if(d0>=0)
                {
                    var skill_name = Config.CombatSkillType.Instance[d0].Name;
                    return ToInfo($"检测{skill_name}造诣")
                            + ToInfo("成功:无事发生", 2)
                            + ToInfo("失败:开战并摧毁巢穴", 2)
                            + GetDestroyNestText(-1, 3);
                }
                return "";
            }
        }
    }
}