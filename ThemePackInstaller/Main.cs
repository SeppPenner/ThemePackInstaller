using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;

namespace ThemePackInstaller
{
    public partial class Main : Form
    {
        public Main()
        {
            InitializeComponent();
        }

        protected override CreateParams CreateParams
        {
            get
            {
                var cp = base.CreateParams;
                cp.ExStyle |= 0x80;
                return cp;
            }
        }

        private void Main_Load(object sender, EventArgs e)
        {
            Process.Start(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Anti-BVB.themepack"));
            Visible = false;
        }
    }
}