using MaterialSkin.Controls;
using MaterialSkin;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace Face_Matcher_UI
{
    public partial class ModernPage : MaterialSkin.Controls.MaterialForm
    {
        public ModernPage()
        {
            InitializeComponent();

            var materialSkinManager = MaterialSkin.MaterialSkinManager.Instance;
            materialSkinManager.AddFormToManage(this);
            materialSkinManager.Theme = MaterialSkinManager.Themes.LIGHT;
            materialSkinManager.ColorScheme = new ColorScheme(
                Primary.Blue500, Primary.Blue700,
                Primary.Blue200, Accent.LightBlue200,
                TextShade.WHITE);

            // Add menu controls manually
            var menu = new MaterialTabControl
            {
                Dock = DockStyle.Top,
                Height = 50
            };
            menu.TabPages.Add("Dashboard");
            menu.TabPages.Add("Reports");
            menu.TabPages.Add("Settings");

            Controls.Add(menu);
        }
    }

}
