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
        public static HashSet<string> ERenGu_Destroy = new HashSet<string> { };
        //叛徒结伙
        public static HashSet<string> Pantu_Win = new HashSet<string> { };//道中胜利
        public static HashSet<string> Pantu_Destroy = new HashSet<string> { };//分支1，和门徒合流
        public static HashSet<string> Pantu_Exit = new HashSet<string> { };//分支3，关闭
        public static HashSet<string> Pantu_Negotiate = new HashSet<string> { };//分支2，跟叛徒谈判
        //悍匪寨
        public static HashSet<string> Hanfei_Start = new HashSet<string> { };//起点交钱
        public static HashSet<string> Hanfei_ReduceNeili = new HashSet<string> { };//减少真气对话
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
                foreach (var name in new string[] { "外道-恶人谷终点1-胜处决", "外道-恶人谷终点-敌人逃脱", "外道-恶人谷终点1-胜2-1" })
                    if (myEventInfos.ContainsKey(name))
                        ERenGu_Destroy.Add(myEventInfos[name].guid);
                if (ERenGu_Destroy.Count != 3)
                    LogUnexpectedEvent("恶人谷终点");
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
        }
        public static void MatchEvents(ref HashSet<string> result, Dictionary<string, MyEventInfo> myEventInfos,string regex_str,int expect_ct,string fail_hint="")
        {
            int tmp_ct = result.Count;
            var regex=new Regex(regex_str);
            foreach (var event_pair in myEventInfos)
                if(regex.IsMatch(event_pair.Key))
                {
                    UnityEngine.Debug.Log($"Match:{regex_str}/{event_pair.Key}");
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
