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
        public class ForwardTarget
        {
            public enum TargetType
            {
                ExitAdventure,
                StartCombat,
                DestroyEnemyNest,
                ConquerEnemyNest,
                SelectAdventureBranch,
                SetAdventureParameter,
                Empty,
                Event,
                Dummy
            }
            public TargetType type;
            public string guid="";
            public string para="";
            public ForwardTarget(TargetType _type, string _guid = "")
            {
                this.type = _type;
                this.guid = _guid;
                this.para = "";
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
            public string text="";
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
            else if (target.type == ForwardTarget.TargetType.Dummy)
            {
                if (!events.ContainsKey(target.guid))
                    return "未导入的事件";
                return events[target.guid].name + "(Dummy)";
            }
            else if (target.type == ForwardTarget.TargetType.StartCombat)
                return $"开战(跳转{target.para})";
            else if (target.type == ForwardTarget.TargetType.ExitAdventure)
                return $"离开巢穴(关闭{target.para})";
            else if (target.type == ForwardTarget.TargetType.DestroyEnemyNest)
                return $"摧毁巢穴(立场{target.para})";
            else if (target.type == ForwardTarget.TargetType.ConquerEnemyNest)
                return $"征服巢穴";
            else if (target.type != ForwardTarget.TargetType.SelectAdventureBranch)
                return $"设置分支{target.para}";
            else if (target.type != ForwardTarget.TargetType.SetAdventureParameter)
                return $"设置参数{target.para}";
            else if (target.type == ForwardTarget.TargetType.Empty)
                return "结束对话";
            return "";
        }
        public HashSet<ForwardTarget> GetForwardTargetsFromMethod(BaseMethodDeclarationSyntax root)
        {
            var result = new HashSet<ForwardTarget>();
            var queue = new Queue<SyntaxNode>();
            foreach (var node in root.ChildNodes())
                queue.Enqueue(node);
            if (root.ToFullString().Contains("string.Empty"))
                result.Add(new ForwardTarget(ForwardTarget.TargetType.Empty));
            while (queue.Count > 0)
            {
                var node = queue.Dequeue();
                if (node.IsKind(SyntaxKind.StringLiteralExpression))//字符串常量
                {
                    Guid guid;
                    var tmp = node.ToString().Replace("\"", "");
                    if (Guid.TryParse(tmp, out guid))
                        result.Add(new ForwardTarget(ForwardTarget.TargetType.Event, tmp));
                }
                else if (node.IsKind(SyntaxKind.InvocationExpression))
                {                    
                    var text = node.ToString();
                    ForwardTarget target=null;
                    var para = GetParameterFromInvoke(node as InvocationExpressionSyntax);
                    if (text.Contains("StartCombat"))
                    {
                        target = new ForwardTarget(ForwardTarget.TargetType.StartCombat);
                        target.para = para.Count >= 5 ? para[2]:"";//跳转
                    }
                    else if (text.Contains("ExitAdventure"))
                    {
                        target = new ForwardTarget(ForwardTarget.TargetType.ExitAdventure);
                        target.para = para.Count >= 1 ? para[0] : "";//是否关闭
                    }
                    else if (text.Contains("DestroyEnemyNest"))
                    {
                        target = new ForwardTarget(ForwardTarget.TargetType.DestroyEnemyNest);
                        target.para = para.Count >= 2 ? para[1] : "";//性格
                    }
                    else if (text.Contains("ConquerEnemyNest"))
                        target = new ForwardTarget(ForwardTarget.TargetType.ConquerEnemyNest);
                    else if (text.Contains("SelectAdventureBranch"))
                    {
                        target = new ForwardTarget(ForwardTarget.TargetType.SelectAdventureBranch);
                        target.para = para.Count >= 1 ? para[0] : "";//分支
                        if (target.para == "")
                            Console.WriteLine();
                    }
                    else if (text.Contains("SetAdventureParameter"))
                    {
                        target = new ForwardTarget(ForwardTarget.TargetType.SetAdventureParameter);
                        target.para = String.Join(",", para);
                    }
                    if(target!=null)
                    {
                        target.para = target.para.Replace("\"", "");
                        result.Add(target);
                    }
                }
                foreach (var _n in node.ChildNodes())
                    queue.Enqueue(_n);
            }
            return result;
        }
        public List<string> GetParameterFromInvoke(InvocationExpressionSyntax root)
        {
            var result = new List<string>();
            var queue = new Queue<SyntaxNode>();
            foreach(var node in root.ChildNodes())
                queue.Enqueue(node);
            while (queue.Count > 0)
            {
                var node = queue.Dequeue();
                if(node.IsKind(SyntaxKind.ArgumentList))
                    foreach (var _n in node.ChildNodes())
                        queue.Enqueue(_n);
                else if (node.IsKind(SyntaxKind.Argument))
                    result.Add(node.ToString());
            }
            return result;
        }
        public List<string> GetOptionKeyFromMethod(BaseMethodDeclarationSyntax root)
        {
            var result = new List<string>();
            var queue = new Queue<SyntaxNode>();
            foreach (var _n in root.ChildNodes())
                queue.Enqueue(_n);
            var optionKeyRegex = new Regex("\"Option_[0-9-]*\"");
            while (queue.Count > 0)
            {
                var node = queue.Dequeue();
                foreach (var _n in node.ChildNodes())
                    queue.Enqueue(_n);
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
            var queue = new Queue<SyntaxNode>();
            foreach (var _n in root.ChildNodes())
                queue.Enqueue(_n);
            {
                var class_name = root.Identifier.Text;
                class_name=class_name.Replace("TaiwuEvent_", "");
                Guid guid;
                if (!Guid.TryParseExact(class_name, "N",out guid))//每个dll有一个总的类
                    return null;
                result.guid = guid.ToString("D");
            }
            var optionSelectRegex = new Regex("^OnOption([0-9]+)Select$");
            while (queue.Count > 0)
            {
                var node = queue.Dequeue();
                var s = node.ToFullString();
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
                    foreach (var _n in node.ChildNodes())
                        queue.Enqueue(_n);
                }
            }
            return result;
        }
        //填充event和option name，要在LoadEventLib后
        void LoadEventLanguage(string path)
        {
            EventInfo currEvent = null;
            var optionRegex = new Regex("Option_([0-9]+)");
            foreach (var line in File.ReadAllLines(path))
            {
                if (line.Length == 0) continue;
                int index = line.IndexOf(':');
                string key = line.Substring(0, index);
                string value = line.Substring(index + 2); //Skip the ':' and the whitespace
                switch (new string(key.Where(Char.IsLetter).ToArray()))
                {
                    //EventGuid总是在最顶层
                    case "EventGuid":
                        if (events.ContainsKey(value))
                            currEvent = events[value];
                        else
                            currEvent = null;
                        break;
                    case "EventContent":
                        if (currEvent != null)
                            currEvent.text = value;
                        break;
                    case "EventName":
                        if(currEvent != null)
                            currEvent.name = value;
                        break;
                    default:
                        if (currEvent != null)
                        {
                            var matches=optionRegex.Match(key);
                            if (matches.Success)
                            {
                                int idx=Int32.Parse(matches.Groups[1].Value)-1;//Language中是从1开始
                                while (currEvent.options.Count < idx + 1)
                                    currEvent.options.Add(new Option());
                                currEvent.options[idx].text = value;
                            }

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
            var queue = new Queue<SyntaxNode>();
            queue.Enqueue(syntax_tree.GetRoot());
            while (queue.Count > 0)
            {
                var node = queue.Dequeue();
                if (node.IsKind(SyntaxKind.NamespaceDeclaration) || node.IsKind(SyntaxKind.CompilationUnit))
                    foreach (var _n in node.ChildNodes())
                        queue.Enqueue(_n);
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
