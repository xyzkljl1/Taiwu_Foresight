namespace LaLaLa
{
    partial class MainForm
    {
        /// <summary>
        /// 必需的设计器变量。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 清理所有正在使用的资源。
        /// </summary>
        /// <param name="disposing">如果应释放托管资源，为 true；否则为 false。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows 窗体设计器生成的代码

        /// <summary>
        /// 设计器支持所需的方法 - 不要修改
        /// 使用代码编辑器修改此方法的内容。
        /// </summary>
        private void InitializeComponent()
        {
            this.codeTextBox = new System.Windows.Forms.TextBox();
            this.noteTextBox = new System.Windows.Forms.TextBox();
            this.mainPanel = new System.Windows.Forms.Panel();
            this.guidLabel = new System.Windows.Forms.Label();
            this.noteApplyButton = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.prefixTextBox = new System.Windows.Forms.TextBox();
            this.prefixApplyButton = new System.Windows.Forms.Button();
            this.contextLabel = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // codeTextBox
            // 
            this.codeTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.codeTextBox.Location = new System.Drawing.Point(2248, 110);
            this.codeTextBox.Multiline = true;
            this.codeTextBox.Name = "codeTextBox";
            this.codeTextBox.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.codeTextBox.Size = new System.Drawing.Size(323, 746);
            this.codeTextBox.TabIndex = 0;
            // 
            // noteTextBox
            // 
            this.noteTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.noteTextBox.Location = new System.Drawing.Point(2248, 890);
            this.noteTextBox.Multiline = true;
            this.noteTextBox.Name = "noteTextBox";
            this.noteTextBox.ScrollBars = System.Windows.Forms.ScrollBars.Horizontal;
            this.noteTextBox.Size = new System.Drawing.Size(323, 522);
            this.noteTextBox.TabIndex = 1;
            // 
            // mainPanel
            // 
            this.mainPanel.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.mainPanel.AutoScroll = true;
            this.mainPanel.Location = new System.Drawing.Point(12, 37);
            this.mainPanel.Name = "mainPanel";
            this.mainPanel.Size = new System.Drawing.Size(2230, 1404);
            this.mainPanel.TabIndex = 3;
            // 
            // guidLabel
            // 
            this.guidLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.guidLabel.AutoSize = true;
            this.guidLabel.Location = new System.Drawing.Point(2245, 9);
            this.guidLabel.Name = "guidLabel";
            this.guidLabel.Size = new System.Drawing.Size(79, 15);
            this.guidLabel.TabIndex = 4;
            this.guidLabel.Text = "GUIDLabel";
            // 
            // noteApplyButton
            // 
            this.noteApplyButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.noteApplyButton.Location = new System.Drawing.Point(2496, 1418);
            this.noteApplyButton.Name = "noteApplyButton";
            this.noteApplyButton.Size = new System.Drawing.Size(75, 23);
            this.noteApplyButton.TabIndex = 5;
            this.noteApplyButton.Text = "Apply";
            this.noteApplyButton.UseVisualStyleBackColor = true;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("宋体", 16.2F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.label1.Location = new System.Drawing.Point(2248, 859);
            this.label1.Name = "label1";
            this.label1.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.label1.Size = new System.Drawing.Size(68, 28);
            this.label1.TabIndex = 6;
            this.label1.Text = "Note";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(2248, 92);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(39, 15);
            this.label2.TabIndex = 7;
            this.label2.Text = "Code";
            // 
            // prefixTextBox
            // 
            this.prefixTextBox.Location = new System.Drawing.Point(12, 6);
            this.prefixTextBox.Name = "prefixTextBox";
            this.prefixTextBox.Size = new System.Drawing.Size(280, 25);
            this.prefixTextBox.TabIndex = 8;
            this.prefixTextBox.Text = "外道-恶人谷";
            // 
            // prefixApplyButton
            // 
            this.prefixApplyButton.Location = new System.Drawing.Point(298, 8);
            this.prefixApplyButton.Name = "prefixApplyButton";
            this.prefixApplyButton.Size = new System.Drawing.Size(126, 23);
            this.prefixApplyButton.TabIndex = 9;
            this.prefixApplyButton.Text = "SearchPrefix";
            this.prefixApplyButton.UseVisualStyleBackColor = true;
            // 
            // contextLabel
            // 
            this.contextLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.contextLabel.Location = new System.Drawing.Point(2248, 37);
            this.contextLabel.Name = "contextLabel";
            this.contextLabel.Size = new System.Drawing.Size(323, 55);
            this.contextLabel.TabIndex = 10;
            this.contextLabel.Text = "ContextLabel";
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(2583, 1453);
            this.Controls.Add(this.contextLabel);
            this.Controls.Add(this.prefixApplyButton);
            this.Controls.Add(this.prefixTextBox);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.noteApplyButton);
            this.Controls.Add(this.guidLabel);
            this.Controls.Add(this.mainPanel);
            this.Controls.Add(this.noteTextBox);
            this.Controls.Add(this.codeTextBox);
            this.Name = "MainForm";
            this.Text = "Form1";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox codeTextBox;
        private System.Windows.Forms.TextBox noteTextBox;
        private System.Windows.Forms.Panel mainPanel;
        private System.Windows.Forms.Label guidLabel;
        private System.Windows.Forms.Button noteApplyButton;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox prefixTextBox;
        private System.Windows.Forms.Button prefixApplyButton;
        private System.Windows.Forms.Label contextLabel;
    }
}

