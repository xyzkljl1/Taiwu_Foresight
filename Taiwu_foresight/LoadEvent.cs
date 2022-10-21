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
        public static HashSet<string> Standard_Destroy = new HashSet<string> { };//六个选项的通用摧毁巢穴选项
        public static HashSet<string> Standard_ConqOrDestroy_Delay = new HashSet<string> { };//征服前听bossBB两句
        public static HashSet<string> Standard_ConqOrDestroy = new HashSet<string> { };//征服或摧毁
        public static HashSet<string> Standard_AllSame = new HashSet<string> { };//毫无意义的选项
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
        //天材地宝
        public static HashSet<string> Dibao_Give = new HashSet<string>();
        public static HashSet<string> Dibao_Final = new HashSet<string>();//最终节点
        public static HashSet<string> Dibao_Final_XieXieQiezi = new HashSet<string>();//因为bug白送了成功率的分支
        public static HashSet<string> Dibao_Final_FuckQiezi = new HashSet<string>();//因为茄子抽风没有写bug，而失去了获赠的1%成功率的分支



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
            //暂定EventInfo不存
            {
                var myEventInfos = LoadEventFile("Taiwu_EventPackage_WD_ERenGu_Language_CN.txt");
                //恶人谷摧毁
                MatchEqualEvents(ref Standard_Destroy, myEventInfos,new string[] { "外道-恶人谷终点1-胜处决", "外道-恶人谷终点-敌人逃脱", "外道-恶人谷终点1-胜2-1" } , "恶人谷摧毁");
                //征服分岔
                MatchEqualEvents(ref Standard_ConqOrDestroy, myEventInfos, "外道-恶人谷终点1-胜", "恶人谷征服Delay");
                MatchEqualEvents(ref Standard_ConqOrDestroy_Delay, myEventInfos, "外道-恶人谷终点1-胜1-1", "恶人谷征服");
                //垃圾选项
                MatchEqualEvents(ref Standard_AllSame, myEventInfos, new string[] { "外道-恶人谷转点A制服", "外道-恶人谷转点A静观", "外道-恶人谷转点B1" }, "恶人谷转点");
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
                MatchEqualEvents(ref Standard_Destroy, myEventInfos, new string[] { "外道-悍匪砦-终点胜处决", "外道-悍匪砦-终点关押-1", "外道-悍匪砦-终点2-胜", "外道-悍匪砦-终点敌人逃脱", "外道-悍匪砦-终点胜1-2" }, "悍匪寨摧毁");
                //征服分岔
                MatchEqualEvents(ref Standard_ConqOrDestroy, myEventInfos, "外道-悍匪砦-终点胜1", "悍匪寨征服Delay");
                MatchEqualEvents(ref Standard_ConqOrDestroy_Delay, myEventInfos, "外道-悍匪砦-终点胜", "悍匪寨征服");
                //起点给钱
                MatchEqualEvents(ref Hanfei_Start, myEventInfos, "外道-悍匪砦-起点", "悍匪寨起点");
                //起点胜后固定进入分支1
                MatchEqualEvents(ref Standard_AllSame, myEventInfos, new string[] { "外道-悍匪砦-起点关押-1", "外道-悍匪砦-起点1-胜1", "外道-悍匪砦-起点1-胜1处决", "外道-悍匪砦-起点1-敌方逃跑" }, "悍匪寨起点分支1");
                //转点1消耗真气 4c4dd807-b41d-4170-b82d-c5b13acef5bc
                MatchEqualEvents(ref Hanfei_ReduceNeili, myEventInfos, "外道-悍匪砦-转点1-1-1-1-1", "悍匪寨转点1");
            }
            //乱葬岗
            {
                var myEventInfos = LoadEventFile("Taiwu_EventPackage_WD_LuanZang_Language_CN.txt");
                MatchEqualEvents(ref Standard_AllSame, myEventInfos, new string[] { "_外道乱葬-起点1" , "_外道乱葬-终点2" , "_外道乱葬-终点6收服胜利" }, "乱葬岗Same");
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
                MatchEqualEvents(ref Standard_Destroy, myEventInfos, new string[] { "_外道乱葬-终点7消灭" }, "乱葬岗摧毁");    
            }
            {
                var myEventInfos = LoadEventFile("Taiwu_EventPackage_FindJiuQu_Language_CN.txt");
                MatchEqualEvents(ref Dibao_Give, myEventInfos, "_地宝紫竹-引藤", "九曲紫竹");
                MatchEqualEvents(ref Standard_AllSame, myEventInfos, new string[] { "_地宝紫竹-起b-1-1-1" , "_地宝紫竹-起a-1-1-1" }, "九曲紫竹same");
                MatchEqualEvents(ref Dibao_Final_XieXieQiezi, myEventInfos, "_地宝紫竹-终a-1", "九曲紫竹终点");
            }
            {
                var myEventInfos = LoadEventFile("Taiwu_EventPackage_FindZiTan_Language_CN.txt");
                MatchEqualEvents(ref Dibao_Give, myEventInfos, new string[] { "_地宝紫檀-伐木一" , "_地宝紫檀-伐木二" }, "紫檀");
                MatchEqualEvents(ref Dibao_Final, myEventInfos, "_地宝紫檀-终a-1", "紫檀终点");
            }

            {
                var myEventInfos = LoadEventFile("Taiwu_EventPackage_FindXuanTie_Language_CN.txt");
                MatchEqualEvents(ref Dibao_Give, myEventInfos, new string[] { "_地宝玄铁-炼铁二", "_地宝玄铁-炼铁一" }, "玄铁");
                MatchEqualEvents(ref Standard_AllSame, myEventInfos, new string[] { "_地宝玄铁-起a-1-1", "_地宝玄铁-起b-1-1" }, "玄铁same");
                MatchEqualEvents(ref Dibao_Final_FuckQiezi, myEventInfos, "_地宝玄铁-终a-1", "玄铁终点");
            }
            {
                //var myEventInfos = LoadEventFile("Taiwu_EventPackage_FindChanQiao_Language_CN.txt");
                //MatchEqualEvents(ref Dibao_Give, myEventInfos, new string[] { "_地宝玄铁-炼铁二", "_地宝玄铁-炼铁一" }, "玄铁");
                //MatchEqualEvents(ref Standard_AllSame, myEventInfos, new string[] { "_地宝玄铁-起a-1-1", "_地宝玄铁-起b-1-1" }, "玄铁same");
                //MatchEqualEvents(ref Dibao_Final_FuckQiezi, myEventInfos, "_地宝玄铁-终a-1", "玄铁终点");
            }


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
                LogUnexpectedEvent(fail_hint);
        }
        public static void MatchEqualEvents(ref HashSet<string> result, Dictionary<string, MyEventInfo> myEventInfos, string name, string fail_hint = "")
        {
            MatchEqualEvents(ref result, myEventInfos,new string[] { name }, fail_hint);
        }
    }
}
