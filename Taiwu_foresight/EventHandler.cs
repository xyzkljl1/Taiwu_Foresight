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
        public static string AllSame = "别看了都一样";
        public static string LeaveNestFalse = "离开巢穴(不关闭)";
        public static string Nothing = "无事发生";
        public static string StartCombat = "开战";
        public enum DibaoBugType
        {
            Plus20 = 1,
            Plus1 = 2,
            Plus0=3,
            Plus2020 = 1
        };

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

        //若干固定文本的单行选项
        public static string Standard_Simple(int idx, EventOptionInfo eventOptionInfo,  object[] paras)
        {
            if(paras!=null&&paras.Count()>idx)
                return ToInfo(paras[idx] as string);
            return "远见挂了！";
        }
        //每个选项都一样，多行文本或固定"都一样"
        public static string Standard_AllSame(int idx, EventOptionInfo eventOptionInfo, object[] paras)
        {
            if (paras!=null&& paras.Count() >= 0)
                return String.Join("",paras);
            return ToInfo(AllSame);
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
        //异士居
        public static string YiShi_Challenge(int idx, EventOptionInfo eventOptionInfo, object[] paras)
        {

            if (idx == 0)
            {
                return
                    ToInfo("开始较艺")
                    + ToInfo("胜利:无事发生", 2)
                    + ToInfo("失败:进入战斗分支");
            }
            else
            {
                return ToInfo("开战")
                    +ToInfo("进入战斗分支");
            }
        }
        //天材地宝
        public static string Dibao_Feed_Posion_Check(int idx, EventOptionInfo eventOptionInfo, object[] paras)//道中给材料
        {
            if (idx == 1)
                return ToInfo(Nothing);
            if(paras!=null&&paras.Count()>0)
            {
                int skillType = (int)paras[0];
                int minValue = GetAdventureParameter("minValue");
                int perValue = GetAdventureParameter("perValue");
                int successRate = GetAdventureParameter("successRate");
                int perSuccessRate = GetAdventureParameter("perSuccessRate");
                string skill_name = Config.LifeSkillType.Instance[skillType].Name;
                return
                    ToInfo($"当前基础成功率:{successRate}")
                    + ToInfo("每投入一个材料，在前方添加一个对应品级的事件(问号格子)")
                    + ToInfo($"如果触发事件，进行{skill_name}造诣检测")
                    + ToInfo($"成功: 增加基础成功率",2)
                    + ToInfo($"八品:造诣>={minValue},基础成功率增加{1 * perSuccessRate}", 3)
                    + ToInfo($"七品:造诣>={minValue+perValue},基础成功率增加{2 * perSuccessRate}", 3)
                    + ToInfo($"六品:造诣>={minValue + perValue*2},基础成功率增加{3 * perSuccessRate}", 3)
                    + ToInfo($"五品:造诣>={minValue + perValue*3},基础成功率增加{4 * perSuccessRate}", 3)
                    + ToInfo($"四品:造诣>={minValue + perValue*4},基础成功率增加{5 * perSuccessRate}", 3)
                    + ToInfo($"三品:造诣>={minValue + perValue*4},基础成功率增加{5 * perSuccessRate}(和四品一样)", 3)
                    + ToInfo("失败: 无事发生", 2)
                    + ToInfo("终点获得二品材料概率=(基础成功率/99.0*100.0)%");
            }
            return "";
        }
        public static string Dibao_Grow_Posion(int idx, EventOptionInfo eventOptionInfo, object[] paras)//投入材料添加的事件
        {
            if (paras != null && paras.Count() > 1)
            {
                int skillType = (int)paras[0];
                int rate = (int)paras[1];
                int minValue = GetAdventureParameter("minValue");
                int perValue = GetAdventureParameter("perValue");
                int successRate = GetAdventureParameter("successRate");
                int perSuccessRate = GetAdventureParameter("perSuccessRate");
                string skill_name = Config.LifeSkillType.Instance[skillType].Name;
                return
                    ToInfo($"当前基础成功率:{successRate}")
                    + ToInfo($"{skill_name}造诣检测")
                    + ToInfo($"若>={minValue+ (rate-1)*perValue},基础成功率增加{rate * perSuccessRate}", 2)
                    + ToInfo("否则: 无事发生", 2)
                    + ToInfo("终点获得二品材料概率=(基础成功率/99.0*100.0)%")
                    + ToInfo("别看了，都一样");
            }
            return "";
        }
        public static string Dibao_Posion_Pick_Trigger_Count(int idx, EventOptionInfo eventOptionInfo, object[] paras)//随机获得材料
        {
            int triggerCounter = GetAdventureParameter("triggerCounter")+1;//因为显示出此文字时triggerCounter已经在OnEventEnter中+1过了
            return ToInfo($"当前计数{triggerCounter}")
                + ToInfo($"每触发一次该事件，计数+1", 2)
                + ToInfo($"进入奇遇时造诣>=300,计数+3", 2)
                + ToInfo($"采集事件获得材料概率(从上至下判断,成功即停止)")
                + ToInfo($"三品:(0+计数*计数*35*1/100)/99.0*100.0={((0 + triggerCounter * triggerCounter * 35 * 100 / 10000) * 100.0 / 99.0).ToString("f2")}%", 2)
                + ToInfo($"四品:(10+计数*计数*35*2/100)/99.0*100.0={((10 + triggerCounter * triggerCounter * 35 * 200 / 10000) * 100.0 / 99.0).ToString("f2")}%", 2)
                + ToInfo($"五品:(20+计数*计数*35*3/100)/99.0*100.0={((20 + triggerCounter * triggerCounter * 35 * 300 / 10000) * 100.0 / 99.0).ToString("f2")}%", 2)
                + ToInfo($"六品:(30+计数*计数*35*4/100)/99.0*100.0={((30 + triggerCounter * triggerCounter * 35 * 400 / 10000) * 100.0 / 99.0).ToString("f2")}%", 2)
                + ToInfo($"七品:(40+计数*计数*35*5/100)/99.0*100.0={((40 + triggerCounter * triggerCounter * 35 * 500 / 10000) * 100.0 / 99.0).ToString("f2")}%", 2)
                + ToInfo($"八品:(50+计数*计数*35*6/100)/99.0*100.0={((50 + triggerCounter * triggerCounter * 35 * 600 / 10000) * 100.0 / 99.0).ToString("f2")}%", 2);
        }
        public static string Dibao_Posion_Pick_Step(int idx, EventOptionInfo eventOptionInfo, object[] paras)
        {
            int step = GetAdventureParameter("step");
            return ToInfo($"当前步数{step}")
                + ToInfo($"每前进一格,步数+1",2)
                + ToInfo($"进入奇遇时造诣>=300,步数+15")
                + ToInfo($"采集事件获得材料概率(从上至下判断,成功即停止)")
                + ToInfo($"三品:(0+步数*步数*2*1/100)/99.0*100.0={((0 + step * step * 200 * 1 / 10000) * 100.0 / 99.0).ToString("f2")}%", 2)
                + ToInfo($"四品:(10+步数*步数*2*2/100)/99.0*100.0={((10 + step * step * 200 * 2 / 10000) * 100.0 / 99.0).ToString("f2")}%", 2)
                + ToInfo($"五品:(20+步数*步数*2*3/100)/99.0*100.0={((20 + step * step * 200 * 3 / 10000) * 100.0 / 99.0).ToString("f2")}%", 2)
                + ToInfo($"六品:(30+步数*步数*2*4/100)/99.0*100.0={((30 + step * step * 200 * 4 / 10000) * 100.0 / 99.0).ToString("f2")}%", 2)
                + ToInfo($"七品:(40+步数*步数*2*5/100)/99.0*100.0={((40 + step * step * 200 * 5 / 10000) * 100.0 / 99.0).ToString("f2")}%", 2)
                + ToInfo($"八品:(50+步数*步数*2*6/100)/99.0*100.0={((50 + step * step * 200 * 6 / 10000) * 100.0 / 99.0).ToString("f2")}%", 2);
        }
        public static string Dibao_Posion_Eat(int idx, EventOptionInfo eventOptionInfo, object[] paras)
        {
            if (currEvent.EventOptionInfos.Count() == 1)//这个事件会连进两次，第一次显示一个废话选项，第二次显示两个有用选项
                return null;
            if (idx == 1)
                return ToInfo("获得该道具");
            var successRate = GetAdventureParameter("successRate");
            var perSuccessRate = GetAdventureParameter("perSuccessRate");
            var successRateEx = GetAdventureParameter("successRateEx");
            var grade = GetAdventureParameter("getItem");
            var perPoison = GetAdventureParameter("perPoison");
            return ToInfo($"当前基础成功率:{successRate}")
                + ToInfo($"若毒术造诣>=300:进入奇遇时获得{successRateEx}基础成功率", 2)
                + ToInfo($"中毒增加{perPoison * (2 + grade)}")
                + ToInfo($"{perPoison}*(10-品级)", 2)
                + ToInfo("可被节气效果减少", 2)
                + ToInfo("若超过毒抗:退出奇遇")
                + ToInfo($"基础成功率增加{perSuccessRate * (2 + grade)}")
                + ToInfo($"{perSuccessRate}*(10-品级)", 2)
                + ToInfo("终点获得二品材料概率=基础成功率/99.0*100.0");
        }

        public static string Dibao_BlackBear_Check(int idx, EventOptionInfo eventOptionInfo, object[] paras)
        {
            return Dibao_Food_Check(0, new string[] { "野兔", "山猪", "山羊", "蛇", "小鹿", "大象" },
                new string[] { "勇壮", "热情", "冷静", "聪颖",  "坚毅",  }, DibaoBugType.Plus0, true);
        }
        public static string Dibao_Peacock_Check(int idx, EventOptionInfo eventOptionInfo, object[] paras)
        {
            return Dibao_Food_Check(3, new string[] { "鸡蛋", "云英鸡", "绍兴麻鸭", "雁鹅", "乌骨鸡", "玲珑鹌鹑" },
                new string[] { "热情", "聪颖", "勇壮", "坚毅", "冷静" }, DibaoBugType.Plus1, false);
        }
        public static string Dibao_XunHuang_Check(int idx, EventOptionInfo eventOptionInfo, object[] paras)
        {
            return Dibao_Food_Check(2, new string[] { "草鱼", "青虾", "岩鲤", "赤蟹", "四腮", "两头" },
                new string[] { "冷静", "坚毅", "勇壮", "聪颖", "热情" }, DibaoBugType.Plus1, false);
        }
        public static string Dibao_MonkeyHead_Check(int idx, EventOptionInfo eventOptionInfo, object[] paras)
        {
            return Dibao_Food_Check(1, new string[] { "小麦", "大豆", "香菇", "芦笋", "贡莲", "银杏" },
                new string[] { "坚毅", "聪颖", "冷静", "热情", "勇壮" },DibaoBugType.Plus1, false);
        }

        public static string Dibao_Food_Check(int skillType, string[] branch_names, string[] fuxings,DibaoBugType bugType,bool speical)
        {
            if (branch_names.Count() < 6||fuxings.Count()<5)
                return null;
            var result = "";
            string skill_name = Config.LifeSkillType.Instance[skillType].Name;
            string initial_skill_name = Config.LifeSkillType.Instance[14].Name;
            var successRate = GetAdventureParameter("successRate");
            var perSuccessRate = GetAdventureParameter("perSuccessRate");
            var successRateEx = GetAdventureParameter("successRateEx");
            var base_chance = GetAdventureParameter("valueNeed");
            var add_chance = GetAdventureParameter("perValue");
            result += ToInfo($"当前基础成功率:{successRate}")
                    + ToInfo($"若{initial_skill_name}造诣>=300:进入奇遇时获得{successRateEx}基础成功率", 2)
                    + ToInfo($"道中事件检测{skill_name}造诣")
                    + ToInfo("成功:增加基础成功率", 2)
                    + ToInfo($"{branch_names[0]}(0):造诣需求{base_chance},基础成功率增加{perSuccessRate}",3)
                    + ToInfo($"{branch_names[1]}(1):造诣需求{base_chance + add_chance},基础成功率增加{perSuccessRate * 2}", 3)
                    + ToInfo($"{branch_names[2]}(2):造诣需求{base_chance + add_chance * 2},基础成功率增加{perSuccessRate * 3}", 3)
                    + ToInfo($"{branch_names[3]}(3):造诣需求{base_chance + add_chance * 3},基础成功率增加{perSuccessRate * 4}", 3)
                    + ToInfo($"{branch_names[4]}(4):造诣需求{base_chance + add_chance * 4},基础成功率增加{perSuccessRate * 5}", 3)
                    + ToInfo($"{branch_names[5]}(5):造诣需求{base_chance + add_chance * 5},基础成功率增加{perSuccessRate * 6}", 3)
                    + ToInfo("失败:无事发生", 2);
            if (speical)
                result += ToInfo("蛇失败会变成山猪", 2);
            result+=ToInfo("事件和格子赋性有关")
                + ToInfo($"{fuxings[0]}:{branch_names[0]}(0)/{branch_names[1]}(1),各50%概率出现,下同", 2)
                + ToInfo($"{fuxings[1]}:{branch_names[0]}(0)/{branch_names[2]}(2)", 2)
                + ToInfo($"{fuxings[2]}:{branch_names[0]}(0)/{branch_names[3]}(3)", 2)
                + ToInfo($"{fuxings[3]}:{branch_names[0]}(0)/{branch_names[4]}(4)", 2)
                + ToInfo($"{fuxings[4]}:{branch_names[0]}(0)/{branch_names[5]}(5)", 2);
            if (bugType == DibaoBugType.Plus1)
                result += ToInfo($"终点获得三品材料概率=(基础成功率-20)/99.0*100.0={((successRate - 20) / 99.0 * 100.0).ToString("f2")}%");
            else if (bugType == DibaoBugType.Plus0)
                result += ToInfo($"终点获得三品材料概率=基础成功率-20={successRate - 20}%");
            return result;
        }

        public static string Dibao_Common_Give(int idx, EventOptionInfo eventOptionInfo, object[] paras)//金铁木材玉石织物
        {
            if (paras == null || paras.Count() < 1)
                return null;
            var result = "";
            var successRate = GetAdventureParameter("successRate");
            var perSuccessRate = GetAdventureParameter("perSuccessRate");
            int bugtype=(int)paras[0];
            if (idx == 0)
            {
                result += ToInfo($"当前基础成功率:{successRate}");
                result += ToInfo("消耗一个材料");//467ea673-ffa0-41e3-b852-c640c8ef3fff
                result += ToInfo($"使基础成功率增加(10-品级)*{perSuccessRate}");
                result += ToInfo("有概率获得3~8品材料");
                result += ToInfo("基础成功率>=90:最高三品", 2);
                result += ToInfo("基础成功率>=60:最高四品", 2);
                result += ToInfo("基础成功率>=40:最高五品", 2);
                result += ToInfo("基础成功率>=30:最高六品", 2);
                result += ToInfo("基础成功率>=20:最高七品", 2);
                result += ToInfo("基础成功率>=10:最高八品", 2);
                result += ToInfo("各品级概率(从可以获得的最高品级开始，分别判定每级是否成功,以增加后的基础成功率计算)");
                //由于蜜汁代码用的就是>Random(0,99),所以白送了1%基础成功率(x=x*100/99)
                //5bdd667f-d3da-4d69-a1be-560864edf62b
                result += ToInfo($"三品:基础成功率*15/99={successRate * 15 / 99}%", 2);
                result += ToInfo($"四品:基础成功率*20/99={successRate * 20 / 99}%", 2);
                result += ToInfo($"五品:基础成功率*25/99={successRate * 25 / 99}%", 2);
                result += ToInfo($"六品:基础成功率*30/99={successRate * 30 / 99}%", 2);
                result += ToInfo($"七品:基础成功率*35/99={successRate * 35 / 99}%", 2);
                result += ToInfo($"八品:基础成功率*40/99={successRate * 40 / 99}%", 2);
                if (bugtype == (int)DibaoBugType.Plus20)
                    result += ToInfo("终点获得二品材料概率=基础成功率 或 (基础成功率-20)*100/99");
                else if(bugtype == (int)DibaoBugType.Plus1)
                    result += ToInfo("终点获得二品材料概率=(基础成功率-20)*100/99");
                else if (bugtype == (int)DibaoBugType.Plus0)
                    result += ToInfo("终点获得二品材料概率=基础成功率-20");
                else
                    result += ToInfo("终点获得二品材料概率=基础成功率");
            }
            else
                result += ToInfo(Nothing);
            return result;
        }
        public static string Dibao_Common_Final(int idx, EventOptionInfo eventOptionInfo, object[] paras)
        {
            if (paras == null || paras.Count() < 1)
                return null;
            var result = "";
            var successRate = GetAdventureParameter("successRate");
            var perSuccessRate = GetAdventureParameter("perSuccessRate");
            int bugtype = (int)paras[0];
            result += ToInfo($"当前基础成功率:{successRate}");
            if (idx == 0)
            {
                result += ToInfo("消耗一个材料");//467ea673-ffa0-41e3-b852-c640c8ef3fff
                result += ToInfo($"使基础成功率增加(10-品级)*{perSuccessRate}");
                if (bugtype == (int)DibaoBugType.Plus20)
                    result += ToInfo("获得二品材料概率=基础成功率(没写错)");
                else if (bugtype == (int)DibaoBugType.Plus1)
                    result += ToInfo("获得二品材料概率=(基础成功率-20)*100/99");
                else if(bugtype == (int)DibaoBugType.Plus0)
                    result += ToInfo("获得二品材料概率=基础成功率-20");
                else
                    result += ToInfo("获得二品材料概率=基础成功率");
            }
            else
            {
                if (bugtype == (int)DibaoBugType.Plus20)
                    result += ToInfo("获得二品材料概率=(基础成功率-20)*100/99");
                else if (bugtype == (int)DibaoBugType.Plus1)
                    result += ToInfo("获得二品材料概率=(基础成功率-20)*100/99");
                else if (bugtype == (int)DibaoBugType.Plus0)
                    result += ToInfo("获得二品材料概率=基础成功率-20");
                else
                    result += ToInfo("获得二品材料概率=基础成功率");
            }
            return result;
        }

    }
}