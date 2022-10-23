using TaiwuModdingLib.Core.Plugin;
using HarmonyLib;
using GameData.Domains.SpecialEffect;
using System.Collections.Generic;
using System;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using GameData.Domains;
using System.Reflection;
using UICommon.Character;
using CharacterDataMonitor;
using Config;
using System.IO;
using UICommon.Character.Elements;
using System.Threading;
using UnityEngine;
using System.Text.RegularExpressions;
using GameData.Domains.TaiwuEvent.DisplayEvent;
using FrameWork;
using System.Linq;
using GameData.Utilities;
using GameData.Serializer;

namespace Taiwu_Foresight
{
    [PluginConfig("Foresight", "xyzkljl1", "1.0.0")]
    public partial class Foresight : TaiwuRemakePlugin
    {
        public static bool On = false;
        public static bool ShowOptionKey = false;
        public static readonly string MyMagicString="<size=179></size>";//用一段隐形富文本区分哪些tip是该mod加上去的
        public readonly static ushort MY_MAGIC_NUMBER_GET_PANTU = 7643;
        public readonly static ushort MY_MAGIC_NUMBER_CharacterDomain = 4;
        Harmony harmony;

        //当前奇遇/事件信息
        public static TaiwuEventDisplayData currEvent=null;
        public static AdventureItem currentAdventure=null;
        public static EnemyNestItem enemyNestCfg=null;

        public static readonly List<string> AdventureParameterKeys = new List<string>
        {
            "slip",
            "surrender",
            "dies",
            "successRate",
            "perSuccessRate",
            "neiQiDegree1",
            "neiQiDegree2",
            "neiQiDegree3"
        };
        public static Dictionary<string,int> AdventureParameters = new Dictionary<string, int>();
        public static int sectId;

        public override void Dispose()
        {
            if (harmony != null)
                harmony.UnpatchSelf();
        }

        public override void Initialize()
        {
            harmony = Harmony.CreateAndPatchAll(typeof(Foresight));
        }
        public override void OnModSettingUpdate()
        {
            //不需要showUseless
            ModManager.GetSetting(ModIdStr, "On", ref On);
            ModManager.GetSetting(ModIdStr, "ShowOptionKey", ref ShowOptionKey);

        }
       
        //获取option在当前事件中的index
        public static int GetIndex(EventOptionInfo info)
        {
            if (currEvent == null)
                return -1;
            for (int i = 0; i < currEvent.EventOptionInfos.Count; ++i) 
                if(currEvent.EventOptionInfos[i].OptionKey==info.OptionKey)
                    return i;
            return -1;
        }
        //从奇遇UI获得当前奇遇
        [HarmonyPostfix, HarmonyPatch(typeof(UI_Adventure), "OnInit")]
        public static void UI_AdventurePatch(UI_Adventure __instance,ArgumentBox argsBox)
        {
            if (!On)
                return;
            if (!__instance)
                return;
            if (argsBox == null)
                return;
            argsBox.Get("ConfigData", out currentAdventure);
            if(currentAdventure!=null)
            {
                UnityEngine.Debug.Log($"Foresight:进入奇遇{currentAdventure.Name}");
                if (EnemyNest.Instance != null&& EnemyNest.Instance.Count>0)
                {
                    try
                    {
                        enemyNestCfg = EnemyNest.Instance.First((EnemyNestItem nest) => nest.AdventureId == currentAdventure.TemplateId);
                    }
                    catch (InvalidOperationException)//没找到居然要抛异常
                    {

                    }
                    if (enemyNestCfg != null)
                        UnityEngine.Debug.Log($"Foresight:外道 {enemyNestCfg.TipDesc} {enemyNestCfg.TipTitle}");
                }
            }
            else
                enemyNestCfg = null;
        }
        public static int GetAdventureParameter(string key)
        {
            if (AdventureParameters.ContainsKey(key))
                return AdventureParameters[key];
            return -1;
        }
        //恶人谷坏结果Option_Key
        public static readonly HashSet<string> ERenGu_Bad_OptionKey = new HashSet<string> {
            //540ce8e2-cf65-4682-a6e5-399c1a99eb57(外道-恶人谷路径1制服之后-花和尚) Option_1->4b54a6f9-8988-4387-a740-ab9aef4b5855->07ce35d8-f497-474d-a598-adec599d7659->69ee1ca3-fc60-4adb-8bb6-6a184988ee17
            "Option_1977723950"
            //aa2f3f8e-5332-432f-bf6f-9d2f424284f2外道-恶人谷路径1制服之后-吃人鬼 Option_1 ->32a8bbaa-5cf4-4ead-9b3b-654acf038163  6585e64b-fd8f-47f7-9f4a-d47a61772cbf 46327882-b86d-41ae-8c86-cedb6386f090/1197a6b9-29d2-49fd-92f2-3337d10c42cf (男/女)
             ,"Option_-358284570"};
        //摧毁巢穴Option_Key到性格的映射
        //注意后端是[0-5)而eventlib里是[1,6)

        //TODO 如何更自动的找到Option_Key和选项的对应关系？如何避免OptionKey变化？
        //根据选项返回远见内容，返回null表示未处理，返回""和返回非空字符串同样表示结果有效
        //参考event目录
        //event的点击实现在EventModel
        //参考AdventureBranchEditor
        //StartCombat:type 1 恶斗 type2死斗
        //CombatResult: 0 胜利 1失败 2 逃跑 3 敌方逃跑 4 我方挂了 5 处决
        public static string GetEventText(EventOptionInfo event_info)
        {
            //OptionContent是文本
            //OptionKey形如Option_14116135，在上一级的TaiwuEvent_XXX的构造函数中的EventOptions
            var optionKey=event_info.OptionKey;
            var result = "";
            string currEventGuid = "";
            if(currEvent!=null)
                currEventGuid = currEvent.EventGuid;
            //这里有三种处理方式:根据EventHandlers找到handler，根据EventGuid处理，根据OptionKey处理
            //应当都使用第一种，但是旧的代码懒得改了，所以保留
            //选项隐藏会使得Idx变化！！！！
            int idx = GetIndex(event_info);
            if (EventHandlers.ContainsKey(currEventGuid))
            {
                var handler =EventHandlers[currEventGuid];
                result += handler.func(idx, handler.args);
            }
            //恶丐
            else if (EGai_Bribe.Contains(currEventGuid))
            {
                if (idx == 0)
                    result += ToInfo("连续给钱三次则和平通过,否则开战");
                else
                    result += ToInfo("开战");
            }
            else if (EGai_BribeOrFood.Contains(currEventGuid))
            {
                if (idx == 0)
                    result += ToInfo("给钱并和平通过");
                else if (idx == 1)
                {
                    result += ToInfo("给食物");
                    result += ToInfo("如果有毒且被对方发现则开战，否则和平通过");
                }
                else if (idx == 2)
                    result += ToInfo("开战");
            }
            //叛徒结伙
            else if (Pantu_Destroy.Contains(currEventGuid))//叛徒结伙，达到分支
            {
                //叛徒结伙分支1选项，全都一样
                result += GetDestroyNestText(-1);
                result += ToInfo("(别看了，所有选项都一样)");
            }
            else if(Pantu_Negotiate.Contains(currEventGuid))//叛徒结伙，谈判分支
            {
                if(idx==0)
                {
                    result += ToInfo("获得一本品级加权随机的功法书");
                    result += GetConquerNestText();
                }
                else
                {
                    result += ToInfo("开战");
                    result += ToInfo("击杀或关押:关闭巢穴(无奖励)");
                }
            }
            else if (Pantu_Negotiate.Contains(currEventGuid))
            {
                result += ToInfo("关闭巢穴(无奖励)");
            }
            else if(Pantu_Win.Contains(currEventGuid))//道中胜利
            {
                //无对话直接分叉的代码在OnEventEnter中
                //叛徒结伙，每次击杀/绑缚/放过增加 EventHelper.GetAdventureParameter([dies/surrender/slip])计数
                //起点时随机门派(CurrentAdventureSiteInitData)
                //终点2aed95dd-beb8-439b-82fb-a7e6bf7a2ae2按门派分叉，然后按计数分叉
                //不同门派的计数要求不一致
                //分叉1进入"达到"分支，分叉2进入"未达到-多"，分叉3进入"未达到-少"，达到时摧毁巢穴，未达到-多时则可以和叛徒谈判并征服巢穴，未达到-少会Exit(true)即离开并关闭巢穴(不获得摧毁奖励)
                //如果和叛徒开战获胜或关押，均是无奖励关闭巢穴
                var dies = GetAdventureParameter("dies");
                var surrender = GetAdventureParameter("surrender");
                var slip = GetAdventureParameter("slip");
                if (sectId >= 0)
                {
                    bool kill = optionKey == "Option_-414627742";
                    bool boundage = optionKey == "Option_700800025";
                    bool letgo = optionKey == "Option_1199181697";
                    var sect = Config.Organization.Instance[sectId];
                    result += ToInfo($"{sect.Name}叛徒结伙计数");
                    result += kill ? ToInfo($"击杀:{dies}->{dies + 1}", 2) : ToInfo($"击杀:{dies}", 2);
                    result += boundage ? ToInfo($"捆绑:{surrender}->{surrender + 1}", 2) : ToInfo($"捆绑:{surrender}", 2);
                    result += letgo ? ToInfo($"放过:{slip}->{slip + 1}", 2) : ToInfo($"放过:{slip}", 2);
                    result += "\n";
                    result += ToInfo("计数影响终点分支(自上至下判断,条件和门派相关)");
                    var require_text = new string[2];
                    {
                        if (sectId == 1 //b9f2163e-bedb-457b-b952-5c56ee47905a
                            || sectId == 2 //0e318de7-2161-4862-a8f9-abd8d8f53edc
                            || sectId == 3 //128e3185-febb-41bd-9730-03bda20cba6c
                            || sectId == 4//b132e0f2-b1fd-43e5-a912-57cacb986caf
                            || sectId == 5//8f1e658f-1fe0-4e5b-b4bd-4b59822512bc

                            )
                        {
                            require_text[0] = "捆绑>=5";
                            require_text[1] = "放过>=击杀+捆绑";
                        }
                        else if (sectId == 6 //1f6d72af-049e-40c6-8020-f716a04062f4
                            || sectId == 7 //b0a6269c-2548-44aa-b975-a7166ae959a2
                            || sectId == 8 //484673b7-5a0c-4ccd-98dd-3f92d621c5ba
                            || sectId == 9 //8bdf40ab-5df9-4da3-b3f3-4712c3b6a6d1
                            || sectId == 10//56343395-7712-48ac-b4ea-09685ea2ca2c
                            )
                        {
                            require_text[0] = "捆绑+击杀>=5";
                            require_text[1] = "放过>=击杀+捆绑";
                        }
                        else if (sectId == 11 //87eb5f2d-33a6-4ebf-89a6-d0f5774ef23f
                            || sectId == 12 //958c82d3-3c1a-49ba-961a-dec026ee7bd6
                            || sectId == 13 //6851bee3-5a45-4535-8a88-cb2889bc4dd0
                            || sectId == 14 //38572054-9b70-4cc9-b5ea-8d9b0382c2ac                            
                            )
                        {
                            require_text[0] = "击杀>=5";
                            require_text[1] = "放过>=击杀+捆绑";
                        }
                        else //635a9d21-3243-420b-be66-d60b050654d5
                        {
                            require_text[0] = "击杀>=5";
                            require_text[1] = "放过>=击杀+捆绑";
                        }
                    }
                    result += ToInfo($"若{require_text[0]}: <摧毁巢穴>", 2);
                    result += ToInfo($"若{require_text[1]}: 和叛徒谈判，可<获得秘籍并征服巢穴>或<关闭巢穴(无奖励)>", 2);
                    result += ToInfo($"均不满足: <关闭巢穴(无奖励)>", 2);
                }
            }
            //悍匪寨
            else if(Hanfei_Start.Contains(currEventGuid))
            {
                if (idx == 0)
                {
                    result += ToInfo("失去500块钱");//248b4d584acf414f87a98c7662ec196d
                    result += ToInfo("开战(恶斗)");
                    result += ToInfo("战胜/关押/地方逃跑：进入分支1", 2);
                    result += ToInfo("战败或逃跑：关闭巢穴", 2);
                    //关押b36e0bb9-f8e1-4401-b3b5-b68e63678e4c->f24481d8-a259-4e8b-868f-9b549bbbdfae  EventHelper.SelectAdventureBranch("1");
                    //战败3e519081-ea1f-4944-a414-fbeb48602b9b 跑路
                    //0 edb0771d-c2df-46ab-ba2b-6a2279995c78 ->ebfe4276-47a0-42ad-91ed-a528a9aa2319 EventHelper.SelectAdventureBranch("1");
                    //5： 9d499117-00db-4349-bd4d-fa36bea430b9  EventHelper.SelectAdventureBranch("1");
                    //4： 37ca0b21-c1c5-49be-9e95-4038764e9927 挂了
                    //2： 91684453-0555-4198-87b5-c6469a5cc404 跑路
                    //3： 49d7dbd2-d6eb-4efa-acf5-cfcad01be101 敌方逃跑->ebfe4276-47a0-42ad-91ed-a528a9aa2319
                }
                else
                {
                    result += ToInfo("开战(死斗)");
                    result += ToInfo("战胜/关押/敌方逃跑：前进", 2);//进入分支1
                    result += ToInfo("战败或逃跑：关闭巢穴", 2);
                    //关押 b36e0bb9-f8e1-4401-b3b5-b68e63678e4c
                    //战败3b211baa-4207-4d47-b74e-504259829641
                    //0 edb0771d-c2df-46ab-ba2b-6a2279995c78
                    //5 9d499117-00db-4349-bd4d-fa36bea430b9
                    //4 3b211baa-4207-4d47-b74e-504259829641 
                    //2 91684453-0555-4198-87b5-c6469a5cc404
                    //3 49d7dbd2-d6eb-4efa-acf5-cfcad01be101
                }
            }
            else if (Hanfei_Start.Contains(currEventGuid))
            {
                result += ToInfo("该项真气减少上限的一半");
                result += ToInfo("减少量只取决于<现在>的真气数量，你懂的",2);
                result += ToInfo("开战(恶斗)");//c8db854a-d1d0-4562-bd50-f271595dd382
                result += ToInfo("战败/关押/处决:强攻入寨", 2);//分支3
                result += ToInfo("战胜/敌人逃脱:前进", 2);//分支2

                //关押 0d02ef0d-e97d-453c-b8ea-2ce66dcede54
                //战败 6edfa100-2686-47fa-9b96-e297676783c6
                //处决 5fc92eac-df67-4e7a-9991-4679e6d470c5
                //战胜/敌人逃脱 d02e5579-4d55-4857-a9d2-09fe6324e15e
            }
            //乱葬岗
            else if(Luanzang_ChoosePosion.ContainsKey(currEventGuid))
            {
                if(idx == 0)
                {
                    result += ToInfo($"每次前进增加{Luanzang_ChoosePosion[currEventGuid].Item1},随机减少郁/寒/幻毒中的一种(只随机一次)");
                }
                else
                {
                    result += ToInfo($"每次前进增加{Luanzang_ChoosePosion[currEventGuid].Item2},随机减少赤/烈/腐毒中的一种(只随机一次)");
                }
            }
            else if (Luanzang_ChooseSame.Contains(currEventGuid))
            {
                if(idx == 0)
                    result += ToInfo("进入水口分支(烈/郁毒)");
                else if(idx == 1)
                    result += ToInfo("进入明堂分支(赤/寒毒)");
                else
                    result += ToInfo("进入气脉分支(腐/幻毒)");
                result += ToInfo("三个分支只有中毒类型有区别");
            }
            else if (Luanzang_MuMen.Contains(currEventGuid))
            {
                result += ToInfo("战斗");
                result += ToInfo("胜利/关押/敌方逃跑:破阵计数+1",2);
                result += ToInfo("失败/逃跑:离开巢穴",2);
                result += ToInfo("破阵计数>=3:可以退出巢穴;进入终点分支");
                result += ToInfo("破阵计数<3:重复水口/明堂/气脉");
            }
            else if (Luanzang_Trick.Contains(currEventGuid))
            {
                result += ToInfo("破阵计数+1");
                result += ToInfo("破阵计数>=3:可以退出巢穴;进入终点分支");
                result += ToInfo("破阵计数<3:重复水口/明堂/气脉");
            }
            else if (Luanzang_Conq_Delay.Contains(currEventGuid))
            {
                if (idx == 0)
                {
                    result += ToInfo("摧毁或征服巢穴");
                    result += ToInfo("开战");
                    result += ToInfo("胜利/敌方逃跑:征服巢穴",2);
                    result += ToInfo("关押/处决:征服巢穴", 2);
                    result += ToInfo("逃跑:离开巢穴", 2);
                }
                else
                {
                    result += ToInfo("摧毁巢穴");
                    result += ToInfo("开战");
                    result += ToInfo("胜利/敌方逃跑/关押/处决:摧毁巢穴", 2);
                    result += ToInfo("逃跑:离开巢穴", 2);
                }
            }
            //	天材地宝
            else if(Dibao_Give.Contains(currEventGuid))
            {
                var successRate = GetAdventureParameter("successRate");
                var perSuccessRate = GetAdventureParameter("perSuccessRate");
                if (idx == 0)
                {
                    result += ToInfo($"当前成功率:{successRate}");
                    result += ToInfo("消耗一个材料");//467ea673-ffa0-41e3-b852-c640c8ef3fff
                    result += ToInfo($"使成功率增加(10-品级)*{perSuccessRate}");
                    result += ToInfo("有概率获得3~8品材料");
                    result += ToInfo("成功率>=90:最高三品", 2);
                    result += ToInfo("成功率>=60:最高四品", 2);
                    result += ToInfo("成功率>=40:最高五品", 2);
                    result += ToInfo("成功率>=30:最高六品", 2);
                    result += ToInfo("成功率>=20:最高七品", 2);
                    result += ToInfo("成功率>=10:最高八品", 2);
                    result += ToInfo("各品级概率(从可以获得的最高品级开始，分别判定每级是否成功,以增加后的成功率计算)");
                    //由于蜜汁代码用的就是>Random(0,99),所以白送了1%成功率(x=x*100/99)
                    //5bdd667f-d3da-4d69-a1be-560864edf62b
                    result += ToInfo($"三品:成功率*15/99={successRate * 15 / 99}%", 2);
                    result += ToInfo($"四品:成功率*20/99={successRate * 20 / 99}%", 2);
                    result += ToInfo($"五品:成功率*25/99={successRate * 25 / 99}%", 2);
                    result += ToInfo($"六品:成功率*30/99={successRate * 30 / 99}%", 2);
                    result += ToInfo($"七品:成功率*35/99={successRate * 35 / 99}%", 2);
                    result += ToInfo($"八品:成功率*40/99={successRate * 40 / 99}%", 2);
                }
                else
                {
                    result += ToInfo("什么都不会发生");
                }
            }
            else if (Dibao_Final.Contains(currEventGuid))
            {
                var successRate = GetAdventureParameter("successRate");
                var perSuccessRate = GetAdventureParameter("perSuccessRate");
                if (idx == 0)
                {
                    result += ToInfo($"当前成功率:{successRate}");
                    result += ToInfo("消耗一个材料");
                    result += ToInfo($"使成功率增加(10-品级)*{perSuccessRate}");
                    //3feffb60-48c6-4681-9c8d-83558736f2cd
                    result += ToInfo("获得二品材料概率=(成功率-20)*100/99");
                    result += ToInfo("如果选择材料时取消,则等同选项2");

                }
                else
                {
                    //41ae8f52-3f71-413e-a931-63154f30ec79
                    //这里平白无故减了20成功率，还因为Random(0,99)送了1
                    result += ToInfo($"获得二品材料概率=(成功率-20)*100/99={(successRate-20)*100/99}");
                }
            }
            else if (Dibao_Final_XieXieQiezi.Contains(currEventGuid))
            {
                var successRate = GetAdventureParameter("successRate");
                var perSuccessRate = GetAdventureParameter("perSuccessRate");
                if (idx == 0)
                {
                    result += ToInfo($"当前成功率:{successRate}");
                    result += ToInfo("消耗一个材料");
                    result += ToInfo($"使成功率增加(10-品级)*{perSuccessRate}");
                    //79f01cba-ba3f-4f38-9471-afff79c62067
                    //因为茄子的bug，这里没有白送1%,但是也没有减20，谢谢茄子
                    result += ToInfo("获得二品材料概率=成功率");
                    result += ToInfo("如果选择材料时取消,则等同选项2");
                }
                else
                {
                    //41ae8f52-3f71-413e-a931-63154f30ec79
                    //这里平白无故减了20成功率，还因为Random(0,99)送了1
                    result += ToInfo($"获得二品材料概率=(成功率-20)*100/99={(successRate - 20)*100/99}");
                }
            }
            else if (Dibao_Final_FuckQiezi.Contains(currEventGuid))
            {
                var successRate = GetAdventureParameter("successRate");
                var perSuccessRate = GetAdventureParameter("perSuccessRate");
                if (idx == 0)
                {
                    result += ToInfo($"当前成功率:{successRate}");
                    result += ToInfo("消耗一个材料");
                    result += ToInfo($"使成功率增加(10-品级)*{perSuccessRate}");
                    //f7c073cd-eec3-4886-b708-f237df807a54
                    //这里没有bug
                    result += ToInfo("获得二品材料概率=成功率-20");
                    result += ToInfo("如果选择材料时取消,则等同选项2");
                }
                else
                {
                    result += ToInfo($"获得二品材料概率=成功率-20={successRate - 20}");
                }
            }
            else if (optionKey == "Option_698382396")//bad63f08-115a-45aa-970c-fa203dd85e2b Option_12 : （背恩绝情……）
            {
                //Character.ApplyBreakupWithBoyOrGirlFriend
                result += ToInfo("结束我方对对方的爱慕关系")
                    + ToInfo("降低我方心情")
                    + ToInfo("可能结束对方对我方的爱慕关系")
                    + ToInfo("降低对方心情", 2)
                    + ToInfo("双向降低好感")
                    + ToInfo("产生秘闻")
                    + ToInfo("我方可能产生爱慕需求")
                    + ToInfo("可能变为敌人");
            }
            else if (ERenGu_Bad_OptionKey.Contains(optionKey))
            {
                result += ToInfo("坏结果");
            }
            else if (!ShowOptionKey)//开启ShowOptionKey时，对于任何选项都不返回null，并且总是加上optionKey
                return null;
            if(ShowOptionKey)
                result += (result.Length>0?"\n":"") + $"\n({currEventGuid}|{optionKey})";
            return result;
        }
        //选项左侧的问号，只有一个，如果原有文本则接在后面
        [HarmonyPostfix, HarmonyPatch(typeof(UI_EventWindow), "ProcessSpecialEventOption")]
        public static void ProcessSpecialEventOptionPatch(UI_EventWindow __instance, EventOptionInfo info, Refers refers)
        {
            if (!On)
                return;
            if (!__instance)
                return;
            if (refers == null)
                return;
            var tip = refers.CGet<MouseTipDisplayer>("OptionHelp");
            if (tip != null)
            {
                var text =GetEventText(info);
                if (text == null)//为""时仍然替换
                    return ;
                text = MyMagicString + text;
                //UnityEngine.Debug.Log($"远见发动：{text}");
                if (tip.PresetParam == null)
                    tip.PresetParam = new string[2] { MyMagicString+ "远见", text };
                else if(tip.PresetParam.Length > 1)
                {
                    var tmp = tip.PresetParam[1];
                    if (!tip.gameObject.activeSelf)//不显示说明原有的文本是无效的
                        tmp = "";
                    //var match= Regex.Match(tmp, MyMagicString);
                    //if(match.Success)
                    //tmp=tmp.Substring(0, match.Index);
                    if (tmp.Length > 0)
                        tmp += MyMagicString+"\n\n";
                    else
                        tmp += MyMagicString;
                    tip.PresetParam[1] = tmp + text;
                    tip.PresetParam[0] = tip.PresetParam[0]+MyMagicString+ "(远见)";
                }
                else if (tip.PresetParam.Length > 0)
                    tip.PresetParam[0] += "a"+text;
                else
                    tip.PresetParam = new string[2] { MyMagicString+ "远见", text };
                tip.NeedRefresh = true;
                tip.gameObject.SetActive(true);
            }
        }
        //因为我加的tips不一定会被原函数清空，用MyMagicString做分隔符在赋值前清空
        [HarmonyPrefix, HarmonyPatch(typeof(UI_EventWindow), "ProcessSpecialEventOption")]
        public static void ProcessSpecialEventOptionPrePatch(UI_EventWindow __instance, Refers refers)
        {
            if (!On)
                return;
            if (!__instance)
                return;
            if (refers == null)
                return;
            var tip = refers.CGet<MouseTipDisplayer>("OptionHelp");
            if (tip != null&&tip.PresetParam!=null)
                for(int i = 0; i < tip.PresetParam.Length;++i)
                {
                    var match = Regex.Match(tip.PresetParam[i], MyMagicString);
                    if (match.Success)
                        tip.PresetParam[i] = tip.PresetParam[i].Substring(0, match.Index);
                }
        }
        [HarmonyPrefix, HarmonyPatch(typeof(UI_EventWindow), "UpdateOptionScroll")]
        public static void UpdateOptionScrollPrePatch(UI_EventWindow __instance)
        {
            LoadEvents();
            if (!On)
                return;
            if (!__instance)
                return;
            //记录当前事件
            {
                Type type = __instance.GetType();
                var propertyInfo = type.GetProperty("Data", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
                if(propertyInfo != null)
                {
                    object obj = propertyInfo.GetValue(__instance);
                    if (obj != null)
                        currEvent = (TaiwuEventDisplayData)obj;
                    else
                        currEvent = null;
                }
                else
                    currEvent=null;
            }
            //TODO:这序列化也太蠢了，怎么让前后端共用一个数据结构？
            __instance.AsynchMethodCall(MY_MAGIC_NUMBER_CharacterDomain, MY_MAGIC_NUMBER_GET_PANTU, delegate (int offset, RawDataPool dataPool)
            {
                List<int> results = new List<int> ();
                //收发顺序相反
                offset += Serializer.Deserialize(dataPool, offset, ref results);
                offset+=Serializer.Deserialize(dataPool, offset, ref sectId);
                //UnityEngine.Debug.Log($"AAA{results.Count} {String.Join(".", results)}");
                if (results!=null&&results.Count>=AdventureParameterKeys.Count)
                {
                    //UnityEngine.Debug.Log($"Foresight: Update Adventure Info{results.Join()}");
                    for(int i=0;i<AdventureParameterKeys.Count;i++)
                    {
                        AdventureParameters[AdventureParameterKeys[i]]=results[i];
                        //UnityEngine.Debug.Log($"SetParameter{AdventureParameterKeys[i]}={results[i]}");
                    }
                }
                else
                    UnityEngine.Debug.Log("Foresight: Booooooom!!");
            });
        }
        
    }
}
