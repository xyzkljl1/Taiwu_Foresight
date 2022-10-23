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
            return ToInfo("征服巢穴");//似乎没有额外奖励
        }
        //摧毁巢穴，根据性格获得奖励，性格为-1时不会获得任何一种性格对应的奖励，但仍有其它奖励
        public unsafe static string GetDestroyNestText(sbyte behaviorType)
        {
            var result = "摧毁巢穴\n";
            //AdventureDomain.DestroyEnemyNest
            if (enemyNestCfg != null)
            {
                UnityEngine.Debug.Log($"Foresight:外道{enemyNestCfg.TipDesc} {enemyNestCfg.TipTitle}");
                Config.Character instance = Config.Character.Instance;
                List<short> members = enemyNestCfg.Members;
                int index = members.Count - 1;
                Config.CharacterItem enemyLeaderCfg = instance[members[index]];
                result += ToInfo($"获得{enemyNestCfg.MoneyReward}金钱");
                result += ToInfo($"获得{enemyNestCfg.AuthorityReward}威望");
                result += ToInfo($"获得{enemyNestCfg.ExpReward}经验");

                Config.LegacyPointItem configData = Config.LegacyPoint.Instance[13];//遗惠
                result += ToInfo($"获得{configData.BasePoint}(加成前)生平遗惠");
                result += ToInfo($"获得{(enemyNestCfg.SpiritualDebtChange/10).ToString("f1")}地区恩义");
                switch (behaviorType)
                {
                    case 0:
                        {
                            Config.FameActionItem template = Config.FameAction.Instance[41];
                            result += ToInfo($"获得{template.Name}名声");
                            result += ToInfo("当前地区所有城镇安定+1");
                        }
                        break;
                    case 1:
                        {
                            Config.FameActionItem template = Config.FameAction.Instance[38];
                            result += ToInfo($"获得{template.Name}名声");
                            result += ToInfo("当前地区所有城镇文化+1");
                        }
                        break;
                    case 2:
                        {
                            result += ToInfo("当前地区所有城镇安定+1");
                            result += ToInfo("当前地区所有城镇文化+1");
                        }
                        break;
                    case 3:
                        {
                            Config.FameActionItem template = Config.FameAction.Instance[42];
                            result += ToInfo($"获得{template.Name}名声");
                            result += ToInfo("心情+3");
                            int moneyGain = enemyLeaderCfg.Resources.Items[6];
                            result += ToInfo($"获得{moneyGain * 5}~{moneyGain * 10 + 1}金钱");
                        }
                        break;
                    case 4:
                        {
                            Config.FameActionItem template = Config.FameAction.Instance[44];
                            result += ToInfo($"获得{template.Name}名声");
                            result += ToInfo($"获得同道");
                        }
                        break;
                }

            }
            return result;
        }

        public static string Standard_Check2(int idx, object[] paras)//进行XX检测，成功和失败分支的事件
        {
            if(paras!=null&&paras.Count()>=3)
                return ToInfo($"{paras[0] as string}检测")
                    +ToInfo($"成功:{paras[1] as string}")
                    +ToInfo($"失败:{paras[2] as string}");
            return "远见挂了！";
        }
        public static string Standard_Check2OrOther(int idx, object[] paras)//可选进行XX检测或其它的事件
        {
            if (paras != null && paras.Count() >= 4)
                if (idx == 1)
                    return ToInfo(paras[3] as string);
                else
                    return Standard_Check2(idx, paras);
            return "远见挂了！";
        }

        public static string Standard_Simple(int idx, object[] paras)//有若干选项，各自为固定文本的简单选项
        {
            if(paras!=null&&paras.Count()>idx)
                return ToInfo(paras[idx] as string);
            return "远见挂了！";
        }

        public static string Standard_AllSame(int idx, object[] paras)
        {
            return ToInfo("别看了,都一样");
        }
        public static string Standard_Destroy(int idx, object[] paras)//六个选项的通用摧毁巢穴选项
        {
            //选项0-4对应5个性格，5对应逃跑即-1
            return GetDestroyNestText(idx< 5 ? (sbyte) idx : (sbyte)-1);
        }
        public static string Standard_ConqOrDestroy(int idx, object[] paras)//征服或摧毁
        {
                if (idx == 0)//征服
                    return GetConquerNestText();
                else
                    return ToInfo("摧毁巢穴");
        }
        public static string Standard_ConqOrDestroyCombat(int idx, object[] paras)//征服或摧毁(战前)
        {
            if (idx == 0)//征服
                return GetConquerNestText();
            else
                return ToInfo(StartCombat)+ToInfo("摧毁巢穴");
        }
        public static string Standard_ConqSame(int idx, object[] paras)//N个选项全是康壳
        {
           return GetConquerNestText()+ ToInfo("别看了，所有选项都一样");
        }
        public static string Standard_ConqOrDestroy_Delay(int idx, object[] paras)//征服前听bossBB两句
        {
            if (idx == 0)//征服
                return ToInfo("征服或摧毁巢穴");
            else
                return ToInfo("摧毁巢穴");
        }
        //贼人营寨
        public static string Zeiren_Pickmoney(int idx, object[] paras)//捡钱
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
        public static string Zeiren_PickmoneyCombat(int idx, object[] paras)//捡钱后开战
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
        public static string MiXiang_QiDisorder3(int idx, object[] paras)//内息三项
        {
            var degree = GetAdventureParameter($"neiQiDegree{idx + 1}");
            //SolorTerm:节气
            return ToInfo($"内息紊乱增加{(degree/10).ToString("f2")}")
                +ToInfo("可被节气效果减轻",2);
        }
        public static string MiXiang_QiDisorder2(int idx, object[] paras)//内息两项
        {
            var degree = GetAdventureParameter(idx == 0 ? "neiQiDegree1": "neiQiDegree3");
            //SolorTerm:节气
            return ToInfo($"内息紊乱增加{(degree / 10).ToString("f2")}")
                + ToInfo("可被节气效果减轻", 2);
        }


    }
}
