using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static LaLaLa.EventLoader;

namespace LaLaLa
{
    public partial class MainForm : Form
    {
        public Dictionary<string, Control> event_controls=new Dictionary<string, Control>();
        public string currentEventGuid = "";
        public int grid_height = 40;
        public int grid_width = 250;
        public int label_height = 20;
        public int label_width = 150;

        SolidBrush textBrush = new SolidBrush(Color.Black);
        Font textFont = new Font("Arial", 8);
        Pen linePen = new Pen(Color.Red);
        EventLoader event_loader = new EventLoader();
        public struct Line
        {
            public Control start;
            public Control end;
            public string text;
            public Color color;
        }
        public List<Line> lines=new List<Line>();
        public void RefreshPanel(string prefix)
        {
            mainPanel.SuspendLayout();
            lines.Clear();
            event_controls.Clear();
            mainPanel.Controls.Clear();
            currentEventGuid = "";
            int currentLine = 0;            
            foreach (var eventInfo in event_loader.events.Values)
                if (eventInfo.parent_ct == 0)
                    if(prefix==""||eventInfo.name.StartsWith(prefix))
                    {
                        (_, currentLine) = FitIn(new ForwardTarget(ForwardTarget.TargetType.Event,eventInfo.guid),0, currentLine, prefix);
                    }
            mainPanel.ResumeLayout();
            mainPanel.PerformLayout();
        }
        protected void OnNoteApply(object sender, EventArgs e)
        {
            if(event_loader.events.ContainsKey(currentEventGuid))
            {
                event_loader.events[currentEventGuid].note = noteTextBox.Text;
                event_loader.SaveNote(currentEventGuid);
            }
        }
        protected void OnPanelPaint(object sender, PaintEventArgs e)
        {
            linePen.Width = 1;
            linePen.DashStyle = DashStyle.Solid;
            linePen.EndCap=LineCap.ArrowAnchor;
            foreach (var line in lines)
            {
                linePen.Color=line.color;
                int start_x, start_y;
                int end_x, end_y;
                //总是从右边中点
                start_x = line.start.Location.X + line.start.Width;
                start_y = line.start.Location.Y + line.start.Height / 2;
                if (line.start.Location.X < line.end.Location.X)//左到右时总是指向左边中点
                {
                    //左边中点
                    end_x=line.end.Location.X;
                    end_y=line.end.Location.Y+line.end.Height/2;
                }
                else if(line.start.Location.Y>line.end.Location.Y)//回溯时指向上下边中点
                {
                    end_x=line.end.Location.X+line.end.Width/2;
                    end_y = line.end.Location.Y;
                }
                else
                {
                    end_x = line.end.Location.X + line.end.Width / 2;
                    end_y = line.end.Location.Y+line.end.Height;
                }
                e.Graphics.DrawLine(linePen, start_x, start_y, end_x, end_y);
                if(line.text!="")
                    e.Graphics.DrawString(line.text, textFont, textBrush, new PointF((start_x + end_x) / 2, (start_y + end_y) / 2));
            }
        }

        //返回该节点控件(用于画线)和currentLine(用于绘制兄弟)，currentRow不需要返回
        (Control,int) FitIn(ForwardTarget root_node,int currentRow, int currentLine,string prefix)
        {
            //当前节点
            Label control = null;
            control = new Label();
            control.BorderStyle = BorderStyle.FixedSingle;
            control.Text = event_loader.GetForwardTargetText(root_node);
            control.Text.Replace(prefix, "");
            control.Location = new System.Drawing.Point(currentRow*grid_width, currentLine*grid_height);
            control.Size = new System.Drawing.Size(label_width, label_height);
            mainPanel.Controls.Add(control);
            if (!event_loader.IsValidEvent(root_node))
                return (control, currentLine + 1);
            //子节点
            var currEvent = event_loader.events[root_node.guid];            
            control.MouseClick += delegate (object sender, MouseEventArgs e) { this.OnClickEvent(root_node.guid); };
            event_controls.Add(root_node.guid, control);
            int initial_line = currentLine;
            for (int i = 0; i < currEvent.options.Count; i++)
            {
                var option = currEvent.options[i];
                foreach (var forward in option.forward)
                {
                    var line=new Line();
                    line.start = control;
                    line.text = $"选项{i+1}";
                    line.color= Color.Red;
                    if (event_loader.IsValidEvent(root_node)&&event_controls.ContainsKey(forward.guid))//已有则创建Dummy
                    {
                        (line.end, currentLine) = FitIn(new ForwardTarget(ForwardTarget.TargetType.Dummy, forward.guid), currentRow + 1, currentLine, prefix);
                        {//dummy line
                            var dummy_line=new Line();
                            dummy_line.start = line.end;
                            dummy_line.end= event_controls[forward.guid];
                            dummy_line.text = "Dummy";
                            dummy_line.color= Color.Blue;
                            lines.Add(dummy_line);
                        }
                    }
                    else//否则把它放到下一层
                        (line.end,currentLine) = FitIn(forward, currentRow + 1, currentLine, prefix);
                    lines.Add(line);
                }
            }
            foreach (var forward in currEvent.forward)
            {
                var line = new Line();
                line.start = control;
                line.text = $"";
                line.color = Color.Green;
                if (event_loader.IsValidEvent(root_node) && event_controls.ContainsKey(forward.guid))//已有则创建Dummy
                {
                    (line.end, currentLine) = FitIn(new ForwardTarget(ForwardTarget.TargetType.Dummy, forward.guid), currentRow + 1, currentLine,prefix);
                    {//dummy line
                        var dummy_line = new Line();
                        dummy_line.start = line.end;
                        dummy_line.end = event_controls[forward.guid];
                        dummy_line.text = "Dummy";
                        dummy_line.color = Color.Blue;
                        lines.Add(dummy_line);
                    }
                }
                else//否则把它放到下一层
                    (line.end, currentLine) = FitIn(forward, currentRow + 1, currentLine, prefix);
                lines.Add(line);
            }
            return (control, Math.Max(currentLine, initial_line + 1));//至少+1行
        }
        public void OnClickEvent(string guid)
        {
            var code_text = "";
            var note_text = "";
            var guid_text = "";
            if(event_loader.events.ContainsKey(guid))
            {
                var currEvent=event_loader.events[guid];
                currentEventGuid = guid;
                code_text = currEvent.code;
                for(int i=0;i<currEvent.options.Count;i++)
                {
                    code_text += $"\nOption{i+1}-{currEvent.options[i].text}\n";
                    code_text += currEvent.options[i].code;
                }
                note_text=currEvent.note;
                guid_text=currEvent.guid;
            }
            this.codeTextBox.Text = code_text;
            this.noteTextBox.Text = note_text;
            this.guidLabel.Text = guid_text;
        }

    }
}
