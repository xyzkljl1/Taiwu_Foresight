using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LaLaLa
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
            event_loader.LoadAll();
            mainPanel.Paint += this.OnPanelPaint;
            prefixApplyButton.Click += delegate (object sender, EventArgs e)
              {
                  RefreshPanel(prefixTextBox.Text);
              };
            noteApplyButton.Click += this.OnNoteApply;
            RefreshPanel(prefixTextBox.Text);
        }

    }
}
