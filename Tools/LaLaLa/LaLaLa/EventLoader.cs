using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace LaLaLa
{
    internal class EventLoader
    {
        public readonly static string EventSourceDir = "E:\\TaiwuMod\\Source\\EventLib\\";
        public readonly static string EventLanguageDir = "G:\\Steam\\steamapps\\common\\The Scroll Of Taiwu\\Event\\EventLanguages\\";
        public readonly static string EventNoteDir = "E:\\TaiwuMod\\Source\\Note\\";
        public struct ForwardTarget
        {
            public enum TargetType
            {
                ExitAdventure,
                StartCombat,
                DestroyEnemyNest,
                ConquerEnemyNest,
                Empty,
                Event,
                Dummy
            }
            public TargetType type;
            public string guid;
            public ForwardTarget(TargetType _type, string _guid = "")
            {
                this.type = _type;
                this.guid = _guid;
            }
        }

        public class Option
        {
            public string key;
            public string text;
            public string code="";
            public HashSet<ForwardTarget> forward = new HashSet<ForwardTarget>();
        }
        public class EventInfo
        {
            public string guid;
            public string name;
            public string note="";
            public string code="";
            public List<Option> options = new List<Option>();
            public HashSet<ForwardTarget> forward = new HashSet<ForwardTarget>();//event guid
            //cache
            public HashSet<ForwardTarget> all_forwards_cache=new HashSet<ForwardTarget>();
            public void Prepare()
            {
                all_forwards_cache.UnionWith(forward);
                foreach(var option in options)
                    all_forwards_cache.UnionWith(option.forward);
                //去掉换行后的空格和\t
                {
                    var regex = new Regex("\n([\t ]+)");
                    code=regex.Replace(code, "\n");
                    foreach(var option in options)
                        option.code = regex.Replace(option.code, "\n");
                }
                parent_ct = 0;
            }
            //tmp value            
            public int parent_ct = 0;
        }
        public Dictionary<string, EventInfo> events=new Dictionary<string, EventInfo>();
        public List<List<string>> events_layer =new List<List<string>>();
        public bool IsValidEvent(ForwardTarget target)
        {
            return target.type == ForwardTarget.TargetType.Event && events.ContainsKey(target.guid);
        }
        public string GetForwardTargetText(ForwardTarget target)
        {
            if (target.type == ForwardTarget.TargetType.Event)
            {
                if (!events.ContainsKey(target.guid))
                    return "未导入的事件";
                return events[target.guid].name;
            }
            else if(target.type == ForwardTarget.TargetType.Dummy)
            {
                if (!events.ContainsKey(target.guid))
                    return "未导入的事件";
                return events[target.guid].name+"(Dummy)";
            }
            else if (target.type == ForwardTarget.TargetType.StartCombat)
                return "开战";
            else if (target.type == ForwardTarget.TargetType.ExitAdventure)
                return "离开巢穴";
            else if (target.type == ForwardTarget.TargetType.DestroyEnemyNest)
                return "摧毁巢穴";
            else if (target.type == ForwardTarget.TargetType.ConquerEnemyNest)
                return "征服巢穴";
            else if (target.type == ForwardTarget.TargetType.Empty)
                return "结束对话";
            return "";
        }
        public HashSet<ForwardTarget> GetForwardTargetsFromMethod(BaseMethodDeclarationSyntax root)
        {
            var result = new HashSet<ForwardTarget>();
            var queue = new List<SyntaxNode>();
            queue.AddRange(root.ChildNodes());
            if (root.ToFullString().Contains("string.Empty"))
                result.Add(new ForwardTarget(ForwardTarget.TargetType.Empty));

            while (queue.Count > 0)
            {
                var node = queue.Take(1).First();
                queue.Remove(node);
                queue.AddRange((IEnumerable<SyntaxNode>)node.ChildNodes());
                if (node.IsKind(SyntaxKind.StringLiteralExpression))//字符串常量
                {
                    Guid guid;
                    var tmp = node.ToString().Replace("\"", "");
                    if (Guid.TryParse(tmp, out guid))
                        result.Add(new ForwardTarget(ForwardTarget.TargetType.Event, tmp));
                }
                //else if(node.IsKind(SyntaxKind.ReturnStatement))//return
                //{}
                else if (node.IsKind(SyntaxKind.InvocationExpression))
                {
                    var text = node.ToString();
                    if (text.Contains("StartCombat"))
                        result.Add(new ForwardTarget(ForwardTarget.TargetType.StartCombat));
                    else if (text.Contains("ExitAdventure"))
                        result.Add(new ForwardTarget(ForwardTarget.TargetType.ExitAdventure));
                    else if (text.Contains("DestroyEnemyNest"))
                        result.Add(new ForwardTarget(ForwardTarget.TargetType.DestroyEnemyNest));
                    else if (text.Contains("ConquerEnemyNest"))
                        result.Add(new ForwardTarget(ForwardTarget.TargetType.ConquerEnemyNest));
                }
            }
            return result;
        }
        public List<string> GetOptionKeyFromMethod(BaseMethodDeclarationSyntax root)
        {
            var result = new List<string>();
            var queue = new List<SyntaxNode>();
            queue.AddRange(root.ChildNodes());
            var optionKeyRegex = new Regex("\"Option_[0-9-]*\"");
            while (queue.Count > 0)
            {
                var node = queue.Take(1).First();
                queue.Remove(node);
                queue.AddRange((IEnumerable<SyntaxNode>)node.ChildNodes());
                if (node.IsKind(SyntaxKind.StringLiteralExpression))//字符串常量
                    if(optionKeyRegex.IsMatch(node.ToString()))
                    {
                        var tmp = node.ToString().Replace("\"", "");
                        result.Add(tmp);
                    }
            }
            return result;
        }

        public string GetSourceFromMethod(BaseMethodDeclarationSyntax node)
        {
            foreach (var child in node.ChildNodes())
                if (child.IsKind(SyntaxKind.Block))
                    return (child as BlockSyntax).ToFullString();
            return "";
        }
        public EventInfo LoadEventClass(ClassDeclarationSyntax root)
        {
            var name = root.GetFirstToken().Text;
            var result = new EventInfo();
            var queue = new List<SyntaxNode>();
            queue.AddRange(root.ChildNodes());
            {
                var class_name = root.Identifier.Text;
                class_name=class_name.Replace("TaiwuEvent_", "");
                Guid guid;
                if (!Guid.TryParseExact(class_name, "N",out guid))//每个dll有一个总的类
                    return null;
                result.guid = guid.ToString("D");
            }
            if(result.guid== "002b306d-2d5d-43d2-8fe9-52a1e6e2a75e")
            {
                Console.WriteLine("");
            }
            var optionSelectRegex = new Regex("^OnOption([0-9]+)Select$");
            while (queue.Count > 0)
            {
                var node = queue.Take(1).First();
                queue.Remove(node);
                var s = node.ToFullString();
                Console.WriteLine(s);
                if (node.IsKind(SyntaxKind.ConstructorDeclaration))
                {
                    ConstructorDeclarationSyntax method_node = node as ConstructorDeclarationSyntax;
                    //OptionKey
                    var keys = GetOptionKeyFromMethod(method_node);
                    for(int i=0;i<keys.Count;i++)
                    {
                        while (result.options.Count < i + 1)
                            result.options.Add(new Option());
                        result.options[i].key=keys[i];
                    }
                }
                else if (node.IsKind(SyntaxKind.MethodDeclaration))
                {
                    MethodDeclarationSyntax method_node = node as MethodDeclarationSyntax;
                    var method_name = method_node.Identifier.Text;
                    var code = GetSourceFromMethod(method_node);
                    {//OptionSelect
                        var matches = optionSelectRegex.Match(method_name);
                        if (matches.Success)
                        {
                            int optionIdx = Int32.Parse(matches.Groups[1].Value)-1;//从1开始
                            while (result.options.Count < optionIdx + 1)
                                result.options.Add(new Option());
                            var option = result.options[optionIdx];
                            option.code = code;
                            option.forward = GetForwardTargetsFromMethod(method_node);
                        }
                    }
                    if (method_name== "OnEventEnter"||method_name== "OnEventExit"||method_name== "OnCheckEventCondition")
                    {
                        var tmp = GetForwardTargetsFromMethod(method_node);
                        if (tmp.Count > 0)
                        {
                            result.forward.UnionWith(tmp);
                            result.code += "\n" + method_node.ToFullString();
                        }
                    }
                    queue.AddRange(node.ChildNodes());
                }
            }
            return result;
        }
        //填充event和option name，要在LoadEventLib后
        void LoadEventLanguage(string path)
        {
            EventInfo currEvent = null;
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
                        if (events.ContainsKey(value))
                            currEvent = events[value];
                        else
                            currEvent = null;
                        break;
                    //case "EventContent":
                    //    break;
                    //case "Option":
                    //break;
                    case "EventName":
                        if(currEvent != null)
                            currEvent.name = value;
                        break;
                    default:
                        if (currEvent != null)
                            if (key.StartsWith("Option_"))
                            {
                                int idx=Int32.Parse(value.Replace("Option_", ""))-1;//从1开始//应该不会抛异常吧
                                while (currEvent.options.Count < idx + 1)
                                    currEvent.options.Add(new Option());
                                currEvent.options[idx].text=value;
                            }
                        break;
                }
            }
        }
        //载入并初始化eventInfo，需要最先执行
        void LoadEventLib(string path)
        {
            var text = File.ReadAllText(path);
            var syntax_tree = CSharpSyntaxTree.ParseText(text);
            var queue = new List<SyntaxNode>();
            queue.Add(syntax_tree.GetRoot());
            while (queue.Count > 0)
            {
                var node = queue.Take(1).First();
                queue.Remove(node);
                if (node.IsKind(SyntaxKind.NamespaceDeclaration) || node.IsKind(SyntaxKind.CompilationUnit))
                    queue.AddRange(node.ChildNodes());
                else if (node.IsKind(SyntaxKind.ClassDeclaration))
                {
                    var eventInfo = LoadEventClass((ClassDeclarationSyntax)node);
                    if (eventInfo != null)
                        events.Add(eventInfo.guid, eventInfo);
                }
            }
        }
        //填充Note，要在LoadEventLib后
        void LoadNote(string guid, string path)
        {
            if (!events.ContainsKey(guid))
                return;
            events[guid].note = File.ReadAllText(path);
        }
        public void SaveNote(string guid)
        {
            if (!events.ContainsKey(guid))
                return;
            var path = EventNoteDir + guid + ".txt";
            File.WriteAllText(path, events[guid].note);
        }
        public void LoadAll()
        {
            //源码，搜索Dir/*/*.cs
            foreach(var dir in Directory.GetDirectories(EventSourceDir))
            {
                Console.WriteLine($"Load Source From{dir}");
                foreach (var file in Directory.GetFiles(dir))
                    if (file.EndsWith(".cs"))
                        LoadEventLib(file);
            }
            //Language,搜索Dir/*.txt
            foreach (var file in Directory.GetFiles(EventLanguageDir))
                if (file.EndsWith(".txt"))
                    LoadEventLanguage(file);
            //Note,搜索Dir/{guid}.txt
            foreach (var file in Directory.GetFiles(EventNoteDir))
                if (file.EndsWith(".txt"))
                {
                    FileInfo fileInfo=new FileInfo(file);
                    var name=fileInfo.Name.Replace(".txt", "");
                    Guid guid;
                    if(Guid.TryParse(name, out guid))
                        LoadNote(name,file);
                }
            //处理一下
            foreach (var eventInfo in events.Values)
                eventInfo.Prepare();
            //找到根节点
            foreach (var eventInfo in events.Values)
                foreach (var forward in eventInfo.all_forwards_cache)
                    if (IsValidEvent(forward))
                        events[forward.guid].parent_ct++;
        }
    }
}
