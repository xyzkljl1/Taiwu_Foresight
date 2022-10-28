using GameData.Domains.TaiwuEvent.DisplayEvent;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TaiwuModdingLib.Core.Plugin;

namespace Taiwu_Foresight
{
    using HandlerFuncType = Func<int, EventOptionInfo, object[],string>;
    public partial class Foresight : TaiwuRemakePlugin
    {
        public struct MyEventInfo
        {
            public string guid;
            public string content;
        }
        public static bool eventLoaded = false;
        private static void LogUnexpectedEvent(string name)
        {
            UnityEngine.Debug.Log($"远见：崩盘啦！！{name}事件数量不对,请上报bug");
        }
        public class MyEventHandler
        {
            public HandlerFuncType func;
            public object[] args;
            public MyEventHandler(HandlerFuncType _func, object[] _args=null)
            {
                func = _func;
                args = _args;
            }
        }
        public static Dictionary<string, MyEventHandler> EventHandlers=new Dictionary<string, MyEventHandler>();

        //恶丐窝
        public static HashSet<string> EGai_Bribe = new HashSet<string>();
        public static HashSet<string> EGai_BribeOrFood = new HashSet<string>();

        //恶人谷
        //叛徒结伙
        public static HashSet<string> Pantu_Win = new HashSet<string> { };//道中胜利
        public static HashSet<string> Pantu_Destroy = new HashSet<string> { };//分支1，和门徒合流
        public static HashSet<string> Pantu_Exit = new HashSet<string> { };//分支3，关闭
        public static HashSet<string> Pantu_Negotiate = new HashSet<string> { };//分支2，跟叛徒谈判
        //悍匪寨
        public static HashSet<string> Hanfei_Start = new HashSet<string> { };//起点交钱
        public static HashSet<string> Hanfei_ReduceNeili = new HashSet<string> { };//减少真气对话
        //乱葬岗
        public static Dictionary<string,(string,string)> Luanzang_ChoosePosion = new Dictionary<string, (string, string)> ();
        public static HashSet<string> Luanzang_ChooseSame = new HashSet<string>();
        public static HashSet<string> Luanzang_MuMen = new HashSet<string>();
        public static HashSet<string> Luanzang_Trick = new HashSet<string>();
        public static HashSet<string> Luanzang_Conq_Delay = new HashSet<string>();       


        //为了节省0.01秒的时间，每个文件分别载入并记录
        //传入文件名,返回EventName到其它信息的映射
        public static Dictionary<string, MyEventInfo> LoadEventFile(string file_name)
        {
            Dictionary<string, MyEventInfo> myEventInfos = new Dictionary<string, MyEventInfo>();//暂定不存
            string currEventName = "";
            MyEventInfo currEvent = new MyEventInfo();
            var path = Directory.GetCurrentDirectory() + "\\Event\\EventLanguages\\" + file_name;
            foreach (var line in File.ReadAllLines(path))
            {
                if (line.Length == 0) continue;
                int index = line.IndexOf(':');
                string key = line.Substring(0, index);
                string value = line.Substring(index + 2); //Skip the ':' and the whitespace
                switch (new string(key.Where(Char.IsLetter).ToArray()))
                {
                    //EventGuid总是在最顶层,每当读到EventGuid，将上一个Event塞进去
                    case "EventGuid":
                        if (currEventName != "")
                        {
                            if(myEventInfos.ContainsKey(currEventName))//重名事件实在不想处理了
                                UnityEngine.Debug.Log($"Foresight:发现重名事件-{currEventName}");
                            else
                                myEventInfos.Add(currEventName, currEvent);
                            currEvent = new MyEventInfo();
                            currEventName = "";
                        }
                        currEvent.guid = value;
                        break;
                    case "EventContent":
                        currEvent.content = value;
                        break;
                    //case "Option":
                    //break;
                    case "EventName":
                        currEventName = value;
                        break;
                }
            }
            if (currEventName != "")
            {
                if (myEventInfos.ContainsKey(currEventName))
                    UnityEngine.Debug.Log($"Foresight:发现重名事件-{currEventName}");
                else
                    myEventInfos.Add(currEventName, currEvent);
                currEvent = new MyEventInfo();
                currEventName = "";
            }
            UnityEngine.Debug.Log($"Foresight:Load {file_name}:{myEventInfos.Count()}");
            return myEventInfos;
        }
        //为了避免进行过多的字符串正则匹配(虽然也不差这点性能)，同时进行事先检查
        //初始化时根据EventName获取对应事件的Guid(并检查数量)并保存,运行时根据Guid确定是哪个事件
        //由于前端没找到EventName，直接根据Guid找事件太蠢了，所以自己读一遍event/eventlanguages下所需的文件
        //如果数据读进去之后被改过了，那也没辙
        //这个傻逼文件格式还不是yaml
        public static void LoadEvents()
        {
            if (eventLoaded)
                return;
            eventLoaded = true;
            // EventName居然tmd有重复的！因为实在不想用guid索引，所以重复的干脆弃疗忽略了
            //暂定EventInfo不存
            {
                var myEventInfos = LoadEventFile("Taiwu_EventPackage_WD_ERenGu_Language_CN.txt");
                //恶人谷摧毁
                MatchEqualEvents(Standard_Destroy, myEventInfos,new string[] { "外道-恶人谷终点1-胜处决", "外道-恶人谷终点-敌人逃脱", "外道-恶人谷终点1-胜2-1" } , "恶人谷摧毁");
                //征服分岔
                MatchEqualEvents(Standard_ConqOrDestroy, myEventInfos, "外道-恶人谷终点1-胜", "恶人谷征服Delay");
                MatchEqualEvents(Standard_ConqOrDestroy_Delay, myEventInfos, "外道-恶人谷终点1-胜1-1", "恶人谷征服");
                //垃圾选项
                MatchEqualEvents(Standard_AllSame, myEventInfos, new string[] { "外道-恶人谷转点A制服", "外道-恶人谷转点A静观", "外道-恶人谷转点B1" }, "恶人谷转点");
            }
            //叛徒结伙
            {
                int sectCt = 15;
                var myEventInfos = LoadEventFile("Taiwu_EventPackage_WD_PanTu_Language_CN.txt");
                //\p{IsCJKUnifiedIdeographs}:匹配中日韩字符
                //https://learn.microsoft.com/en-us/dotnet/standard/base-types/character-classes-in-regular-expressions
                MatchEvents(ref Pantu_Destroy, myEventInfos, "外道-叛徒结伙-终点[A-Z]-?达到-[\\p{IsCJKUnifiedIdeographs}]{2,3}1$", sectCt, "叛徒结伙终点达到");
                MatchEvents(ref Pantu_Negotiate, myEventInfos, "外道-叛徒结伙-终点通用未达到-多1[\\p{IsCJKUnifiedIdeographs}]{2,3}$", sectCt, "叛徒结伙终点未达到多");
                MatchEqualEvents(ref Pantu_Win, myEventInfos, new string[] { "外道-叛徒结伙-路径-胜" }, "叛徒结伙道中");
                MatchEvents(ref Pantu_Exit, myEventInfos, "外道-叛徒结伙-终点[A-Z]未达到-少-[\\p{IsCJKUnifiedIdeographs}]{2,3}1$", sectCt, "叛徒结伙终点未达到少");
            }
            //悍匪寨
            {
                var myEventInfos = LoadEventFile("Taiwu_EventPackage_WD_HanFei_Language_CN.txt");
                MatchEqualEvents(Standard_Destroy, myEventInfos, new string[] { "外道-悍匪砦-终点胜处决", "外道-悍匪砦-终点关押-1", "外道-悍匪砦-终点2-胜", "外道-悍匪砦-终点敌人逃脱", "外道-悍匪砦-终点胜1-2" }, "悍匪寨摧毁");
                //征服分岔
                MatchEqualEvents(Standard_ConqOrDestroy, myEventInfos, "外道-悍匪砦-终点胜1", "悍匪寨征服Delay");
                MatchEqualEvents(Standard_ConqOrDestroy_Delay, myEventInfos, "外道-悍匪砦-终点胜", "悍匪寨征服");
                //起点给钱
                MatchEqualEvents(ref Hanfei_Start, myEventInfos, "外道-悍匪砦-起点", "悍匪寨起点");
                //起点胜后固定进入分支1
                MatchEqualEvents(Standard_AllSame, myEventInfos, new string[] { "外道-悍匪砦-起点关押-1", "外道-悍匪砦-起点1-胜1", "外道-悍匪砦-起点1-胜1处决", "外道-悍匪砦-起点1-敌方逃跑" }, "悍匪寨起点分支1");
                //转点1消耗真气 4c4dd807-b41d-4170-b82d-c5b13acef5bc
                MatchEqualEvents(ref Hanfei_ReduceNeili, myEventInfos, "外道-悍匪砦-转点1-1-1-1-1", "悍匪寨转点1");
            }
            //乱葬岗
            {
                var myEventInfos = LoadEventFile("Taiwu_EventPackage_WD_LuanZang_Language_CN.txt");
                MatchEqualEvents(Standard_AllSame, myEventInfos, new string[] { "_外道乱葬-起点1" , "_外道乱葬-终点2" , "_外道乱葬-终点6收服胜利" }, "乱葬岗Same");
                if (myEventInfos.ContainsKey("_外道乱葬-转点气脉1"))
                    Luanzang_ChoosePosion.Add(myEventInfos["_外道乱葬-转点气脉1"].guid, ("腐毒", "幻毒"));
                if (myEventInfos.ContainsKey("_外道乱葬-转点水口1"))
                    Luanzang_ChoosePosion.Add(myEventInfos["_外道乱葬-转点水口1"].guid, ("烈毒", "郁毒"));
                if (myEventInfos.ContainsKey("_外道乱葬-转点明堂1"))
                    Luanzang_ChoosePosion.Add(myEventInfos["_外道乱葬-转点明堂1"].guid, ("赤毒", "寒毒"));
                if (Luanzang_ChoosePosion.Count != 3)
                    LogUnexpectedEvent("乱葬岗转点");
                MatchEqualEvents(ref Luanzang_ChooseSame, myEventInfos, new string[]{"_外道乱葬-阵中4", "_外道乱葬-破阵二1", "_外道乱葬-破阵一2"}, "乱葬岗Same2");
                MatchEqualEvents(ref Luanzang_MuMen, myEventInfos, "_外道乱葬-墓门2", "乱葬岗墓门");
                MatchEqualEvents(ref Luanzang_Trick, myEventInfos, new string[] { "_外道乱葬-天门1", "_外道乱葬-来龙1" }, "乱葬岗机关");
                MatchEqualEvents(ref Luanzang_Conq_Delay, myEventInfos, "_外道乱葬-终点3", "乱葬岗终点战前");
                MatchEqualEvents(Standard_Destroy, myEventInfos, new string[] { "_外道乱葬-终点7消灭" }, "乱葬岗摧毁");    
            }
            //恶丐
            {
                var myEventInfos = LoadEventFile("Taiwu_EventPackage_WD_EGai_Language_CN.txt");
                MatchEqualEvents(ref EGai_Bribe, myEventInfos, new string[] { "_外道恶丐-转点2" }, "恶丐转点");
                MatchEqualEvents(ref EGai_BribeOrFood, myEventInfos, new string[] { "_外道恶丐-引路1-2", "_外道恶丐-引路2-2", "_外道恶丐-引路4选择其他" }, "恶丐赠与食物或金钱");
                MatchEqualEvents(Standard_AllSame, myEventInfos, new string[] { "_外道恶丐-背景3-3", "_外道恶丐-过渡随机3-1", "_外道恶丐-过渡随机2-1", "_外道恶丐-过渡随机1-1" }, "恶丐same");
                MatchEqualEvents(Standard_ConqSame, myEventInfos, new string[] { "_外道恶丐-终点收服5胜利" }, "恶丐Conq");
                MatchEqualEvents(Standard_ConqOrDestroy_Delay, myEventInfos, new string[] { "_外道恶丐-终点收服2衣装" }, "恶丐PreConq");
            }
            //贼人
            {
                var myEventInfos = LoadEventFile("Taiwu_EventPackage_WD_ZeiRen_Language_CN.txt");
                MatchEqualEvents(Standard_AllSame, myEventInfos, new string[] { "_外道贼人-终b-1-1" });
                MatchEqualEvents(Standard_Destroy, myEventInfos, new string[] { "_外道贼人-终a-1-2-胜", "_外道贼人-终a-1-2-敌逃" });
                MatchEqualEvents(Standard_ConqOrDestroy_Delay, myEventInfos, new string[] { "_外道贼人-终a-1" });
                
                MatchEqualEvents(SimpleHandler("可获得金钱","无事发生"), myEventInfos, new string[] { "_外道贼人-路a-藏宝" });
                
                MatchEqualEvents(CheckHandler($"{Config.CombatSkillType.Instance[1].Name}造诣", Nothing, StartCombat), myEventInfos, new string[] { "_外道贼人-路c-暗害" });
                MatchEqualEvents(CheckHandler($"{Config.CombatSkillType.Instance[2].Name}造诣", Nothing, StartCombat), myEventInfos, new string[] { "_外道贼人-路c-迷雾" });
                MatchEqualEvents(CheckHandler($"{Config.CombatSkillType.Instance[2].Name}造诣", $"开战、带路或离开", LeaveNestFalse), myEventInfos, new string[] { "_外道贼人-路a-幸运-2概率-1-2低-1" });

                MatchEqualEvents(CheckOrOtherHandler($"{Config.CombatSkillType.Instance[2].Name}造诣", Nothing, LeaveNestFalse, LeaveNestFalse), myEventInfos, new string[] { "_外道贼人-路a-暗桩-1" });
                MatchEqualEvents(CheckOrOtherHandler($"{Config.CombatSkillType.Instance[0].Name}造诣", Nothing, LeaveNestFalse, LeaveNestFalse), myEventInfos, new string[] { "_外道贼人-路a-密道" });
                MatchEqualEvents(CheckOrOtherHandler($"{Config.CombatSkillType.Instance[1].Name}造诣", Nothing, $"开战、带路或离开", LeaveNestFalse), myEventInfos, new string[] { "_外道贼人-路a-幸运-2概率-1" });


                MatchEqualEvents(CheckOrOtherHandler($"{Config.LifeSkillType.Instance[6].Name}造诣", Nothing, $"75%{StartCombat}/25%{Nothing}", StartCombat), myEventInfos, new string[] { "_外道贼人-路b-机关（勇壮）", "_外道贼人-路b-机关（勇壮）-1" });
                MatchEqualEvents(CheckOrOtherHandler($"{Config.LifeSkillType.Instance[7].Name}造诣", Nothing, $"75%{StartCombat}/25%{Nothing}", StartCombat), myEventInfos, new string[] { "_外道贼人-路b-机关（细腻）-1" });
                MatchEqualEvents(CheckOrOtherHandler($"{Config.LifeSkillType.Instance[11].Name}造诣", Nothing, $"75%{StartCombat}/25%{Nothing}", StartCombat), myEventInfos, new string[] { "_外道贼人-路b-机关（坚毅）" });
                MatchEqualEvents(CheckOrOtherHandler($"{Config.LifeSkillType.Instance[10].Name}造诣", Nothing, $"75%{StartCombat}/25%{Nothing}", StartCombat), myEventInfos, new string[] { "_外道贼人-路b-机关（聪颖）" });


                MatchEqualEvents(CheckOrOtherHandler($"{Config.CombatSkillType.Instance[0].Name}造诣", Nothing, StartCombat, StartCombat), myEventInfos, new string[] { "_外道贼人-路c-包围" });

                MatchEqualEvents(SimpleHandler("进入分支1(机关多，可征服)", "进入分支2(战斗多，不可征服)"), myEventInfos, new string[] { "_外道贼人-转a-1" });

                MatchEqualEvents(Zeiren_Pickmoney, myEventInfos, new string[] { "_外道贼人-路a-幸运" });
                MatchEqualEvents(Zeiren_PickmoneyCombat, myEventInfos, new string[] { "_外道贼人-路a-幸运-2概率-1-2低-1-1高" });
            }
            //迷香阵
            {
                var myEventInfos = LoadEventFile("Taiwu_EventPackage_WD_MiXiang_Language_CN.txt");
                MatchEqualEvents(Standard_ConqOrDestroy_Delay, myEventInfos, new string[] { "外道-迷香阵-男-终点A", "外道-迷香阵-女-终点A" });
                MatchEqualEvents(Standard_ConqOrDestroyCombat, myEventInfos, new string[] { "外道-迷香阵-男-终点A1-1", "外道-迷香阵-女-终点A1-1" });
                MatchEqualEvents(Standard_Destroy, myEventInfos, new string[] { "外道-迷香阵-终点通用-敌人逃脱", "外道-迷香阵-终点通用-胜" });
                MatchEqualEvents(Standard_AllSame, myEventInfos, new string[] { "外道-迷香阵-男-转点A1", "外道-迷香阵-起点1-1-3-1", "外道-迷香阵-起点1-1-3-1-1-1-异相选择" });
                
                MatchEqualEvents(SimpleHandler("做梦分支(增加内息紊乱)", "战斗分支"), myEventInfos, new string[] { "外道-迷香阵-起点1-1-3","外道-迷香阵-女-转点A1-1-1", "外道-迷香阵-男-转点A1-1-1-1" });
                //MatchEqualEvents(SimpleHandler("前有无意义(看两句废话然后重选)", "前有无意义(看两句废话然后重选)", "前进"), myEventInfos, new string[] { "外道-迷香阵-起点1-1" });//选项已经带有眼睛图标，不需要提示

                MatchEqualEvents(MiXiang_QiDisorder3, myEventInfos, new string[] {
                    "外道-迷香阵-女-路径3-子愚1", "外道-迷香阵-女-路径3-子愚2", "外道-迷香阵-女-路径3-子愚3",
                "外道-迷香阵-女-路径3-妻蛮",
                "外道-迷香阵-男-路径3-子愚1","外道-迷香阵-男-路径3-子愚2","外道-迷香阵-男-路径3-子愚3",
                "外道-迷香阵-男-路径3-夫蛮",
                "外道-迷香阵-女-路径2-琐事1","外道-迷香阵-女-路径2-琐事2","外道-迷香阵-女-路径2-琐事3",
                "外道-迷香阵-男-路径2-礼物1","外道-迷香阵-男-路径2-礼物2","外道-迷香阵-男-路径2-礼物3",
                "外道-迷香阵-男-路径2-照顾1","外道-迷香阵-男-路径2-照顾2","外道-迷香阵-男-路径2-照顾3",
                });
                MatchEqualEvents(MiXiang_QiDisorder2, myEventInfos, new string[] {
                "外道-迷香阵-女-路径2-工作1","外道-迷香阵-女-路径2-工作2","外道-迷香阵-女-路径2-工作3",
                });
            }
            {//弃世绝境
                //重复:_外道绝境-起a-1-1-1 忽略
                var myEventInfos = LoadEventFile("Taiwu_EventPackage_WD_QiShi_Language_CN.txt");
                MatchEqualEvents(Standard_AllSame, myEventInfos, new string[] { "_外道绝境-起a-1-1-1-1-1" });
                MatchEqualEvents(Standard_CombatConqSame, myEventInfos, new string[] { "_外道绝境-终a-1-1-1-x高-1-1" });
                MatchEqualEvents(Standard_ConqSame, myEventInfos, new string[] { "_外道绝境-终a-1-1-1-x高-1-1-x-关押-1", "_外道绝境-终a-1-1-1-x高-1-1-x-战胜-1" });

                MatchEqualEvents(QiShi_FinalMultiCheck, myEventInfos, new string[] { "_外道绝境-终a-1-1-1", "_外道绝境-终a-1-1-1-1低" });
                MatchEqualEvents(QiShi_MiddleMultiCheck, myEventInfos, 
                    new string[] { "_外道绝境-转b-1-1", "_外道绝境-转b-1-1-1低", "_外道绝境-转b-1-1-2低", "_外道绝境-转b-1-1-3低" });

                MatchEqualEvents(QiShi_MiddleSingleCheck, myEventInfos,new string[] { "_外道绝境-转a" });
            }
            {//邪人死地
                var myEventInfos = LoadEventFile("Taiwu_EventPackage_WD_XieRen_Language_CN.txt");
                //初遇
                MatchEqualEvents(Xieren_FirstMet, myEventInfos, new string[] { "_外道邪人-转点0" });
                MatchEqualEvents(SimpleHandler("破解或放弃", "放弃:在每个拐点再战boss(可逃跑),终点解除限制并再战"), myEventInfos, new string[] { "_外道邪人-转点11问" });
                MatchEqualEvents(Xieren_TrickChoose, myEventInfos, new string[] { "_外道邪人-转点12破解" });

                MatchEqualEvents(Standard_AllSame, myEventInfos, new string[] { "_外道邪人-背景邪祀1", "_外道邪人-背景衣角1", "_外道邪人-背景衣角2", "_外道邪人-背景衣角3研究", "_外道邪人-背景共主1" ,
                "_外道邪人-终点收服7","_外道邪人-起点2","_外道邪人-转点8"});
                MatchEqualEvents(Standard_ConqOrDestroy, myEventInfos, new string[] { "_外道邪人-终点收服9" });
                MatchEqualEvents(Standard_Destroy, myEventInfos, new string[] { "_外道邪人-终点收服6消灭", "_外道邪人-转点再遇4胜利", "_外道邪人-转点4胜利" , "_外道邪人-终点死战7" });
                MatchEqualEvents(Standard_ConqOrDestroy_Delay, myEventInfos, new string[] { "_外道邪人-终点收服4胜利" });
            }
            {//群魔乱舞
                var myEventInfos = LoadEventFile("Taiwu_EventPackage_WD_QunMo_Language_CN.txt");
                MatchEqualEvents(Standard_AllSame, myEventInfos, new string[] { "_外道群魔-路a-将死2", "_外道群魔-路a-将死2-1", "_外道群魔-转d-错-1" , "_外道群魔-路a-将死3-1", "_外道群魔-路a-将死1", "_外道群魔-路a-将死1-1" });
                MatchEqualEvents(Standard_Destroy, myEventInfos, new string[] { "_外道群魔-终-关押", "_外道群魔-终a-战胜-2-1", "_外道群魔-终-敌逃" });
                MatchEqualEvents(Standard_ConqOrDestroy_Delay, myEventInfos, new string[] { "_外道群魔-终a-战胜" });
                MatchEqualEvents(Standard_ConqOrDestroy, myEventInfos, new string[] { "_外道群魔-终a-战胜-1-1", "_外道群魔-终b-战胜-1-1" });
                MatchEqualEvents(SimpleHandler("继续前进或离开巢穴", "离开巢穴"), myEventInfos, new string[] { "_外道群魔-转a-1-1-1" });
                MatchEqualEvents(SimpleHandler("继续前进", "离开巢穴"), myEventInfos, new string[] { "_外道群魔-转a-1-1-1(选择入魔人)" });
                MatchEqualEvents(SimpleHandler("进入分支1", "进入分支2"), myEventInfos, new string[] { "_外道群魔-转a-1-1-1-1-1-1", "_外道群魔-转b-1-1-1-1" });
            }
            {//修罗
                var myEventInfos = LoadEventFile("Taiwu_EventPackage_WD_XiuLuo_Language_CN.txt");
                MatchEqualEvents(Standard_AllSame, myEventInfos, new string[] { "_外道修罗-转d-随1-1", "_外道修罗-转d-随2-1", "_外道修罗-转d-随3-1" , "_外道修罗-路a-掉落-1" });
                MatchEqualEvents(Standard_Destroy, myEventInfos, new string[] { "_外道修罗-终a-达6-1-关押-1", "_外道修罗-转b-1-达3-1-战胜", "_外道修罗-转b-1-达3-1-敌逃", "_外道修罗-终b-1-1-1-1-关押-1", "_外道修罗-终b-1-1-1-1-战胜-2",
                "_外道修罗-终a-未达6-1-战胜"});
                MatchEqualEvents(Standard_ConqOrDestroy_Delay, myEventInfos, new string[] { "_外道修罗-终b-1-1-1-1-战胜", "_外道修罗-终b-1-1-1-1-战胜-1" });
                MatchEqualEvents(Standard_ConqOrDestroy, myEventInfos, new string[] { "_外道修罗-终b-1-1-1-1-战胜-1-1" });
                MatchEqualEvents(Xiuluo_StartChoose, myEventInfos, new string[] { "_外道修罗-转a-1-1-1" });
            }
            {//异士居
                var myEventInfos = LoadEventFile("Taiwu_EventPackage_WD_YiShi_Language_CN.txt");
                MatchEqualEvents(Standard_Destroy, myEventInfos, new string[] { "_外道异士-竹林转点12消灭胜利" });
                MatchEqualEvents(Standard_AllSame, myEventInfos, new string[] { "_外道异士-起点7胜利" });
                MatchEqualEvents(SimpleHandler("进入战斗分支", LeaveNestFalse), myEventInfos, new string[] { "_外道异士-起点7失败", "_外道异士-挑战3失败", "_外道异士-收服转点5失败" });
                MatchEqualEvents(YiShi_Challenge, myEventInfos, new string[] { "_外道异士-挑战1-1", "_外道异士-挑战1-3", "_外道异士-挑战1-2a", "_外道异士-挑战1-2c" });
                MatchEqualEvents(SameMultiLineHandler(ToInfo("开始较艺"), ToInfo("胜利:进入较艺分支(征服或摧毁巢穴)", 2), ToInfo("失败:进入战斗分支(摧毁巢穴)",2)), myEventInfos, new string[] { "_外道异士-起点4" });
                MatchEqualEvents(SameMultiLineHandler(ToInfo(LeaveNestFalse), ToInfo(AllSame)), myEventInfos, new string[] { "_外道异士-起点8失败离去" });
                MatchEqualEvents(Standard_ConqSame, myEventInfos, new string[] { "_外道异士-竹林转点9收服" });
                MatchEqualEvents(SimpleHandler("征服巢穴", "开战并摧毁巢穴"), myEventInfos, new string[] { "_外道异士-竹林转点8胜利" });
            }
            //义士堂(不是外道，但仍然是巢穴
            {
                var myEventInfos = LoadEventFile("Taiwu_EventPackage_RighteousLow_Language_CN.txt");
                MatchEqualEvents(Standard_ConqSame, myEventInfos, new string[] { "义士堂-终点a-战胜-1" });
                MatchEqualEvents(SameMultiLineHandler(ToInfo(StartCombat),ToInfo(LeaveNestTrue)), myEventInfos, new string[] { "义士堂-终点bc-择一相助" });
            }
            //木头/金铁/织物/玉石
            {
                var myEventInfos = LoadEventFile("Taiwu_EventPackage_FindJiuQu_Language_CN.txt");
                MatchEqualEvents(new MyEventHandler(Dibao_Common_Give, new object[] { DibaoBugType.Plus20 }), myEventInfos, "_地宝紫竹-引藤", "九曲紫竹");
                MatchEqualEvents(Standard_AllSame, myEventInfos, new string[] { "_地宝紫竹-起b-1-1-1" , "_地宝紫竹-起a-1-1-1" }, "九曲紫竹same");
                MatchEqualEvents(new MyEventHandler(Dibao_Common_Final, new object[] { DibaoBugType.Plus20 }), myEventInfos, "_地宝紫竹-终a-1", "九曲紫竹终点");
            }
            {
                var myEventInfos = LoadEventFile("Taiwu_EventPackage_FindZiTan_Language_CN.txt");
                MatchEqualEvents(new MyEventHandler(Dibao_Common_Give, new object[] { DibaoBugType.Plus1 }), myEventInfos, new string[] { "_地宝紫檀-伐木一" , "_地宝紫檀-伐木二" }, "紫檀");
                MatchEqualEvents(new MyEventHandler(Dibao_Common_Final, new object[] { DibaoBugType.Plus1 }), myEventInfos, "_地宝紫檀-终a-1", "紫檀终点");
            }

            {
                var myEventInfos = LoadEventFile("Taiwu_EventPackage_FindXuanTie_Language_CN.txt");
                MatchEqualEvents(new MyEventHandler(Dibao_Common_Give, new object[] { DibaoBugType.Plus0 }), myEventInfos, new string[] { "_地宝玄铁-炼铁二", "_地宝玄铁-炼铁一" }, "玄铁");
                MatchEqualEvents(Standard_AllSame, myEventInfos, new string[] { "_地宝玄铁-起a-1-1", "_地宝玄铁-起b-1-1" }, "玄铁same");
                MatchEqualEvents(new MyEventHandler(Dibao_Common_Final, new object[] { DibaoBugType.Plus0 }), myEventInfos, "_地宝玄铁-终a-1");
            }
            {
                var myEventInfos = LoadEventFile("Taiwu_EventPackage_FindChanQiao_Language_CN.txt");
                MatchEqualEvents(Standard_AllSame, myEventInfos, new string[] { "_地宝精金-高造诣起点3" , "_地宝精金-低造诣起点3" });
                MatchEqualEvents(new MyEventHandler(Dibao_Common_Give, new object[] { DibaoBugType.Plus0 }), myEventInfos, new string[] { "_地宝精金-叩石1", "_地宝精金-叩石1-（选择道具）" });
                MatchEqualEvents(new MyEventHandler(Dibao_Common_Final, new object[] { DibaoBugType.Plus0 }), myEventInfos, "_地宝精金-终点1", "_地宝精金-终点1-（选择道具）");
            }

            {
                var myEventInfos = LoadEventFile("Taiwu_EventPackage_FindChanYi_Language_CN.txt");
                MatchEqualEvents(Standard_AllSame, myEventInfos, new string[] { "_地宝金蝉-高造诣起点-1", "_地宝金蝉-低造诣起点-1进入" });
                MatchEqualEvents(new MyEventHandler(Dibao_Common_Give, new object[] { DibaoBugType.Plus0 }), myEventInfos, new string[] { "_地宝金蝉-节点-1布置灵木", "_地宝金蝉-节点-1布置灵木-（选择道具）" });
                MatchEqualEvents(new MyEventHandler(Dibao_Common_Final, new object[] { DibaoBugType.Plus0 }), myEventInfos, "_地宝金蝉-低造诣终点-1", "_地宝金蝉-终点-（选择道具）");
            }
            {
                var myEventInfos = LoadEventFile("Taiwu_EventPackage_FindTianCan_Language_CN.txt");
                MatchEqualEvents(Standard_AllSame, myEventInfos, new string[] { "_地宝天蚕-低造诣起点-1", "_地宝天蚕-高造诣起点-1" });
                MatchEqualEvents(new MyEventHandler(Dibao_Common_Give, new object[] { DibaoBugType.Plus0 }), myEventInfos, new string[] { "_地宝天蚕-低造诣节点-1安放簇具", "_地宝天蚕-高造诣节点-1安放簇具", "_地宝天蚕-低造诣节点-1安放簇具（选择道具）" });
                MatchEqualEvents(new MyEventHandler(Dibao_Common_Final, new object[] { DibaoBugType.Plus0 }), myEventInfos, "_地宝天蚕-低造诣终点-1", "_地宝天蚕-低造诣终点-1-（选择道具）");
            }
            {
                var myEventInfos = LoadEventFile("Taiwu_EventPackage_FindKunLun_Language_CN.txt");
                MatchEqualEvents(new MyEventHandler(Dibao_Common_Give, new object[] { DibaoBugType.Plus2020 }), myEventInfos, new string[] { "天材-昆仑-路径a1-1", "天材-昆仑-路径a2", "天材-昆仑-路径a-（选择道具）" });
                MatchEqualEvents(new MyEventHandler(Dibao_Common_Final, new object[] { DibaoBugType.Plus2020 }), myEventInfos, "天材-昆仑-终", "天材-昆仑-终-（选择道具）");
            }
            {
                var myEventInfos = LoadEventFile("Taiwu_EventPackage_FindShenZhao_Language_CN.txt");
                MatchEqualEvents(Standard_AllSame, myEventInfos, new string[] { "天材-神照石-起点B" , "天材-神照石-起点A" });
                MatchEqualEvents(new MyEventHandler(Dibao_Common_Give, new object[] { DibaoBugType.Plus0 }), myEventInfos, new string[] { "天材-神照石-路径A-第一1-1", "天材-神照石-路径A-寻石通-（选择道具）" , "天材-神照石-路径B-寻石", "天材-神照石-路径A-寻石通" });
                MatchEqualEvents(new MyEventHandler(Dibao_Common_Final, new object[] { DibaoBugType.Plus0 }), myEventInfos, "天材-神照石-终点1", "天材-神照石-终点1-（选择道具）");
            }
            //食物
            {
                
                var myEventInfos = LoadEventFile("Taiwu_EventPackage_FindBlackBear_Language_CN.txt");
                MatchEqualEvents(Dibao_BlackBear_Check, myEventInfos, new string[] {
                    "天材-黑熊-起点A1","天材-黑熊-起点B1",//显示太早了会读不到参数
                    "天材-黑熊-路径1-野兔", "天材-黑熊-路径1-鹿1", "天材-黑熊-路径1-象", "天材-黑熊-路径1-山羊", "天材-黑熊-路径1-山猪", "天材-黑熊-路径1-蛇" });
            }
            {
                var myEventInfos = LoadEventFile("Taiwu_EventPackage_FindPeacock_Language_CN.txt");
                MatchEqualEvents(Dibao_Peacock_Check, myEventInfos, new string[] {
                    "_地宝孔雀-起b-1","_地宝孔雀-起a-1",
                    "_地宝孔雀-画0鸡蛋", "_地宝孔雀-画1云英鸡", "_地宝孔雀-画2绍兴麻鸭", "_地宝孔雀-画5玲珑鹌鹑", "_地宝孔雀-画3雁鹅", "_地宝孔雀-画4乌骨鸡" });
            }
            {
                var myEventInfos = LoadEventFile("Taiwu_EventPackage_FindMonkeyHead_Language_CN.txt");
                MatchEqualEvents(Standard_AllSame, myEventInfos, new string[] { "_地宝猴头-低造诣起点1", "_地宝猴头-高造诣起点1" });
                MatchEqualEvents(Dibao_MonkeyHead_Check, myEventInfos, new string[] {
                    "_地宝猴头-高造诣起点3","_地宝猴头-低造诣起点3",
                    "_地宝猴头-小麦1", "_地宝猴头-香菇1", "_地宝猴头-芦笋1", "_地宝猴头-贡莲1", "_地宝猴头-大豆1", "_地宝猴头-银杏1" });
            }
            {
                var myEventInfos = LoadEventFile("Taiwu_EventPackage_FindXuHuang_Language_CN.txt");
                MatchEqualEvents(Standard_AllSame, myEventInfos, new string[] { "_地宝鲟鳇-起b", "_地宝鲟鳇-起a" });
                MatchEqualEvents(Dibao_XunHuang_Check, myEventInfos, new string[] {
                    "_地宝鲟鳇-起a-1",
                    "_地宝鲟鳇-书0草鱼", "_地宝鲟鳇-书3赤蟹", "_地宝鲟鳇-书1青虾", "_地宝鲟鳇-书2岩鲤", "_地宝鲟鳇-书4四腮", "_地宝鲟鳇-书5两头" });
            }
            //毒物
            {
                var myEventInfos = LoadEventFile("Taiwu_EventPackage_FindFaceMan_Language_CN.txt");
                MatchEqualEvents(Standard_AllSame, myEventInfos, new string[] { "_地宝人面-起a-1-1"});
                MatchEqualEvents(new MyEventHandler(Dibao_Feed_Posion_Check, new object[] { 9 }), myEventInfos, new string[] {
                    "_地宝人面-转a-1-1-1", "_地宝人面-转a-1-1-1（选择道具）"});
                MatchEqualEvents(new MyEventHandler(Dibao_Grow_Posion, new object[] { 9, 1 }), myEventInfos, "_地宝人面-生长-x-1夹竹桃-1");
                MatchEqualEvents(new MyEventHandler(Dibao_Grow_Posion, new object[] { 9, 2 }), myEventInfos, "_地宝人面-生长-x-2彼岸花-1");
                MatchEqualEvents(new MyEventHandler(Dibao_Grow_Posion, new object[] { 9, 3 }), myEventInfos, "_地宝人面-生长-x-3缚魂丝-1");
                MatchEqualEvents(new MyEventHandler(Dibao_Grow_Posion, new object[] { 9, 4 }), myEventInfos, "_地宝人面-生长-x-4金怠花-1");
                MatchEqualEvents(new MyEventHandler(Dibao_Grow_Posion, new object[] { 9, 5 }), myEventInfos, "_地宝人面-生长-x-5烟煴紫瘴-1");
                MatchEqualEvents(new MyEventHandler(Dibao_Grow_Posion, new object[] { 9, 5 }), myEventInfos, "_地宝人面-生长-x-6无寐兰-1");//三品和四品材料效果一样

                MatchEqualEvents(new MyEventHandler(Dibao_Posion_Pick_Trigger_Count, new object[] { 9}), myEventInfos, new string[] {
                    "_地宝人面-探谷-1夹竹桃", "_地宝人面-探谷-2彼岸花","_地宝人面-探谷-3缚魂丝","_地宝人面-探谷-4金怠花","_地宝人面-探谷-5烟煴紫瘴","_地宝人面-探谷-6无寐兰"});
            }
            {
                var myEventInfos = LoadEventFile("Taiwu_EventPackage_FindQingZhu_Language_CN.txt");
                MatchEqualEvents(Standard_AllSame, myEventInfos, new string[] { "_地宝青蛛-高造诣起点2" , "_地宝青蛛-低造诣起点2" });
                MatchEqualEvents(new MyEventHandler(Dibao_Feed_Posion_Check, new object[] { 9 }), myEventInfos, new string[] {
                    "_地宝青蛛-低造诣转点2", "_地宝青蛛-低造诣转点2（选择道具）"});
                MatchEqualEvents(new MyEventHandler(Dibao_Grow_Posion, new object[] { 9, 1 }), myEventInfos, "_地宝青蛛-腐尸虫事件1");
                MatchEqualEvents(new MyEventHandler(Dibao_Grow_Posion, new object[] { 9, 2 }), myEventInfos, "_地宝青蛛-蝮蛇涎事件1");
                MatchEqualEvents(new MyEventHandler(Dibao_Grow_Posion, new object[] { 9, 3 }), myEventInfos, "_地宝青蛛-散瘟草事件2");
                MatchEqualEvents(new MyEventHandler(Dibao_Grow_Posion, new object[] { 9, 4 }), myEventInfos, "_地宝青蛛-玄尸水事件2");
                MatchEqualEvents(new MyEventHandler(Dibao_Grow_Posion, new object[] { 9, 5 }), myEventInfos, "_地宝青蛛-鬼虫事件2");
                MatchEqualEvents(new MyEventHandler(Dibao_Grow_Posion, new object[] { 9, 5 }), myEventInfos, "_地宝青蛛-蛇骨事件2");//三品和四品材料效果一样

                MatchEqualEvents(Dibao_Posion_Pick_Trigger_Count, myEventInfos, new string[] {
                    "_地宝青蛛-获得腐尸虫1", "_地宝青蛛-获得蝮蛇涎1","_地宝青蛛-获得散瘟草1","_地宝青蛛-获得玄尸水1","_地宝青蛛-获得鬼虫1","_地宝青蛛-获得冥蛇骨1"});
            }
            {
                var myEventInfos = LoadEventFile("Taiwu_EventPackage_FindBingCan_Language_CN.txt");
                MatchEqualEvents(SameMultiLineHandler(ToInfo("每次前进时获得寒毒(数量随步数增长)"),
                    ToInfo("若毒术造诣>=300:只受到60%的毒"),//posionRate 5->3
                    ToInfo("抗到终点获得二品材料"),
                    ToInfo(AllSame)), myEventInfos, new string[] { "_地宝冰蚕-高造诣起点1", "_地宝冰蚕-低造诣起点1" });
                MatchEqualEvents(Dibao_Posion_Pick_Step, myEventInfos, new string[] {
                    "_地宝冰蚕-节点冽霜草1", "_地宝冰蚕-节点白蛇胆1","_地宝冰蚕-节点玄阴石1","_地宝冰蚕-节点寒玉蟾蜍1","_地宝冰蚕-节点玄冰琵琶蝎1","_地宝冰蚕-节点青蛟胆1"});
            }
            {
                var myEventInfos = LoadEventFile("Taiwu_EventPackage_FindXueChan_Language_CN.txt");
                MatchEqualEvents(SameMultiLineHandler(ToInfo("每次前进时获得赤毒(数量随步数增长)"),
                    ToInfo("若毒术造诣>=300:只受到60%的毒"),//posionRate 5->3
                    ToInfo("抗到终点获得二品材料"),
                    ToInfo(AllSame)), myEventInfos, new string[] { "天材-血蟾-起A1", "天材-血蟾-起B造诣低" });
                MatchEqualEvents(Dibao_Posion_Pick_Step, myEventInfos, new string[] {
                    "天材-血蟾-路径-1红信石1", "天材-血蟾-路径-2见血1","天材-血蟾-路径-3一品1","天材-血蟾-路径-4赤血1","天材-血蟾-路径-5孔雀1","天材-血蟾-路径-6凤凰1"});
            }
            {
                var myEventInfos = LoadEventFile("Taiwu_EventPackage_FindXieQiao_Language_CN.txt");
                MatchEqualEvents(Standard_AllSame, myEventInfos, new string[] { "天材-邪窍花-起点B1", "天材-邪窍花-起点A1" });
                MatchEqualEvents(SimpleHandler("获得道具/增加基础成功率",Nothing), myEventInfos, new string[] { 
                    "天材-邪窍花-路径1-草乌头1", "天材-邪窍花-路径1-草乌头2","天材-邪窍花-路径1-草乌头3",
                    "天材-邪窍花-路径1-相思子1","天材-邪窍花-路径1-相思子2","天材-邪窍花-路径1-相思子3",
                    "天材-邪窍花-路径1-紫蜈蜂1","天材-邪窍花-路径1-紫蜈蜂2","天材-邪窍花-路径1-紫蜈蜂3",
                    "天材-邪窍花-路径1-鬼母杜鹃1","天材-邪窍花-路径1-鬼母杜鹃2","天材-邪窍花-路径1-鬼母杜鹃3",
                    "天材-邪窍花-路径1-百眼蜈蚣1","天材-邪窍花-路径1-百眼蜈蚣2","天材-邪窍花-路径1-百眼蜈蚣3",
                    "天材-邪窍花-路径1-七彩纱娘1"});
                MatchEqualEvents(Dibao_Posion_Eat, myEventInfos, new string[] {
                    "天材-邪窍花-路径1-草乌头1-1-1-1", "天材-邪窍花-路径1-草乌头第一次选项",
                    "天材-邪窍花-路径1-相思子1-1-1-1", "天材-邪窍花-路径1-相思子选项1",
                    "天材-邪窍花-路径1-紫蜈蜂1-1-1-1", "天材-邪窍花-路径1-紫蜈蜂选项1",
                    "天材-邪窍花-路径1-鬼母杜鹃1-1-1-1","天材-邪窍花-路径1-鬼母杜鹃选项1",
                    "天材-邪窍花-路径1-百眼蜈蚣1-1-1-1","天材-邪窍花-路径1-百眼蜈蚣选项1",
                    "天材-邪窍花-路径1-七彩纱娘1-1-1-1","天材-邪窍花-路径1-七彩纱娘选项1"});
            }
            {
                var myEventInfos = LoadEventFile("Taiwu_EventPackage_FindDuanChang_Language_CN.txt");
                MatchEqualEvents(Standard_AllSame, myEventInfos, new string[] { "_地宝断肠-起a", "_地宝断肠-起b" });
                MatchEqualEvents(Dibao_Posion_Eat, myEventInfos, new string[] {
                    "_地宝断肠-尝毒-1鸩羽-1","_地宝断肠-尝毒-2雷公藤-1","_地宝断肠-尝毒-3牵机草-1","_地宝断肠-尝毒-4五煞落魂草-1","_地宝断肠-尝毒-5杏黄蛛-1","_地宝断肠-尝毒-6金蛇"});
            }
        }

        public static MyEventHandler SimpleHandler(params object[] options)
        {
            return new MyEventHandler(Standard_Simple, options);
        }
        public static MyEventHandler SameMultiLineHandler(params string[] lines)
        {
            return new MyEventHandler(Standard_AllSame, lines.ToArray<object>());
        }
        public static MyEventHandler MutltiCheckOrGiveUpHandler(string name, string option1, string option2)
        {
            return new MyEventHandler(Standard_MultiCheckOrGiveUp, new object[] { name, option1, option2 });
        }

        public static MyEventHandler CheckHandler(string name,string option1, string option2)
        {
            return new MyEventHandler(Standard_Check2, new object[] { name, option1, option2 });
        }
        public static MyEventHandler CheckOrOtherHandler(string name, string option1, string option2,string otherwise)
        {
            return new MyEventHandler(Standard_Check2OrOther, new object[] { name, option1, option2, otherwise });
        }

        public static void MatchEqualEvents(MyEventHandler handler, Dictionary<string, MyEventInfo> myEventInfos, string name, string fail_hint = "")
        {
            MatchEqualEvents(handler, myEventInfos, new string[] { name},fail_hint);
        }
        public static void MatchEqualEvents(MyEventHandler handler, Dictionary<string, MyEventInfo> myEventInfos, string[] names, string fail_hint = "")
        {
            int ct = 0;
            foreach (var name in names)
                if (myEventInfos.ContainsKey(name))
                    if (!EventHandlers.ContainsKey(myEventInfos[name].guid))
                    {
                        EventHandlers.Add(myEventInfos[name].guid, handler);
                        ct++;
                    }
                    else
                        UnityEngine.Debug.Log($"远见：重复Handler-{myEventInfos[name].guid} {name}");
            if (ct != names.Count())
                LogUnexpectedEvent(fail_hint == "" ? String.Join("/", names) : fail_hint);
        }

        public static void MatchEqualEvents(HandlerFuncType handler,Dictionary<string, MyEventInfo> myEventInfos, string[] names, string fail_hint = "")
        {
            MatchEqualEvents(new MyEventHandler(handler),myEventInfos, names, fail_hint);
        }
        public static void MatchEqualEvents(HandlerFuncType handler, Dictionary<string, MyEventInfo> myEventInfos, string name, string fail_hint = "")
        {
            MatchEqualEvents(handler, myEventInfos, new string[] { name }, fail_hint);
        }
        public static void MatchEvents(MyEventHandler handler, Dictionary<string, MyEventInfo> myEventInfos, string regex_str, int expect_ct, string fail_hint = "")
        {
            int tmp_ct = EventHandlers.Count;
            var regex = new Regex(regex_str);
            foreach (var event_pair in myEventInfos)
                if (regex.IsMatch(event_pair.Key) && !EventHandlers.ContainsKey(event_pair.Value.guid))
                {
                    //UnityEngine.Debug.Log($"Match:{regex_str}/{event_pair.Key}");
                    EventHandlers.Add(event_pair.Value.guid, handler);
                }
            if (EventHandlers.Count - tmp_ct != expect_ct)
                LogUnexpectedEvent(fail_hint == "" ? regex_str : fail_hint);
        }
        public static void MatchEvents(HandlerFuncType handler, Dictionary<string, MyEventInfo> myEventInfos, string regex_str, int expect_ct, string fail_hint = "")
        {
            MatchEvents(new MyEventHandler(handler),myEventInfos,regex_str,expect_ct,fail_hint);
        }

        public static void MatchEvents(ref HashSet<string> result, Dictionary<string, MyEventInfo> myEventInfos,string regex_str,int expect_ct,string fail_hint="")
        {
            int tmp_ct = result.Count;
            var regex=new Regex(regex_str);
            foreach (var event_pair in myEventInfos)
                if(regex.IsMatch(event_pair.Key))
                {
                    //UnityEngine.Debug.Log($"Match:{regex_str}/{event_pair.Key}");
                    result.Add(event_pair.Value.guid);
                }
            if (result.Count - tmp_ct!=expect_ct)
                LogUnexpectedEvent(fail_hint);
        }

        public static void MatchEqualEvents(ref HashSet<string> result, Dictionary<string, MyEventInfo> myEventInfos, string[] names, string fail_hint = "")
        {
            int tmp_ct = result.Count;
            foreach (var name in names)
                if (myEventInfos.ContainsKey(name))
                    result.Add(myEventInfos[name].guid);//不考虑重复
            if (result.Count - tmp_ct != names.Count())
            {
                LogUnexpectedEvent(fail_hint);

            }
        }
        public static void MatchEqualEvents(ref HashSet<string> result, Dictionary<string, MyEventInfo> myEventInfos, string name, string fail_hint = "")
        {
            MatchEqualEvents(ref result, myEventInfos,new string[] { name }, fail_hint);
        }
    }
}
