namespace UI
{
    partial class SuspectListControl
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanelSuspects;

        private System.Windows.Forms.SplitContainer splitContainer;
        private System.Windows.Forms.Panel panelSearch;
        private System.Windows.Forms.TextBox txtSearch;
        private System.Windows.Forms.Button btnSearch;
        // private System.Windows.Forms.FlowLayoutPanel flowLayoutPanelSuspects;
        private Panel panelTopBar;
        private Button btnEnroll;
        private Panel panelSeparator;
        private void InitializeComponent()
        {
            splitContainer = new SplitContainer();
            leftPanel = new Panel();
            flowLayoutPanelSuspects = new FlowLayoutPanel();
            panelSearch = new Panel();
            panelSearchContainer = new Panel();
            txtSearch = new TextBox();
            panelDetails = new Panel();
            panelTopBar = new Panel();
            btnEnroll = new Button();
            panelSeparator = new Panel();
            ((System.ComponentModel.ISupportInitialize)splitContainer).BeginInit();
            splitContainer.Panel1.SuspendLayout();
            splitContainer.Panel2.SuspendLayout();
            splitContainer.SuspendLayout();
            leftPanel.SuspendLayout();
            panelSearch.SuspendLayout();
            panelSearchContainer.SuspendLayout();
            panelTopBar.SuspendLayout();
            SuspendLayout();
            // 
            // splitContainer
            // 
            splitContainer.Dock = DockStyle.Fill;
            splitContainer.FixedPanel = FixedPanel.Panel1;
            splitContainer.Location = new Point(0, 41);
            splitContainer.Name = "splitContainer";
            // 
            // splitContainer.Panel1
            // 
            splitContainer.Panel1.Controls.Add(leftPanel);
            // 
            // splitContainer.Panel2
            // 
            splitContainer.Panel2.Controls.Add(panelDetails);
            splitContainer.Size = new Size(800, 409);
            splitContainer.SplitterDistance = 200;
            splitContainer.TabIndex = 0;
            // 
            // leftPanel
            // 
            leftPanel.Controls.Add(flowLayoutPanelSuspects);
            leftPanel.Controls.Add(panelSearch);
            leftPanel.Dock = DockStyle.Fill;
            leftPanel.Location = new Point(0, 0);
            leftPanel.Name = "leftPanel";
            leftPanel.Size = new Size(200, 409);
            leftPanel.TabIndex = 0;
            // 
            // flowLayoutPanelSuspects
            // 
            flowLayoutPanelSuspects.AutoScroll = true;
            flowLayoutPanelSuspects.Dock = DockStyle.Fill;
            flowLayoutPanelSuspects.FlowDirection = FlowDirection.TopDown;
            flowLayoutPanelSuspects.Location = new Point(0, 40);
            flowLayoutPanelSuspects.Name = "flowLayoutPanelSuspects";
            flowLayoutPanelSuspects.Padding = new Padding(5);
            flowLayoutPanelSuspects.Size = new Size(200, 369);
            flowLayoutPanelSuspects.TabIndex = 0;
            flowLayoutPanelSuspects.WrapContents = false;
            // 
            // panelSearch
            // 
            panelSearch.Controls.Add(panelSearchContainer);
            panelSearch.Dock = DockStyle.Top;
            panelSearch.Location = new Point(0, 0);
            panelSearch.Name = "panelSearch";
            panelSearch.Padding = new Padding(5);
            panelSearch.Size = new Size(200, 40);
            panelSearch.TabIndex = 1;
            // 
            // panelSearchContainer
            // 
            panelSearchContainer.BackColor = Color.White;
            panelSearchContainer.Controls.Add(txtSearch);
            panelSearchContainer.Dock = DockStyle.Fill;
            panelSearchContainer.Location = new Point(5, 5);
            panelSearchContainer.Name = "panelSearchContainer";
            panelSearchContainer.Padding = new Padding(5);
            panelSearchContainer.Size = new Size(190, 30);
            panelSearchContainer.TabIndex = 0;
            // 
            // txtSearch
            // 
            txtSearch.BackColor = Color.White;
            txtSearch.BorderStyle = BorderStyle.None;
            txtSearch.Dock = DockStyle.Fill;
            txtSearch.Font = new Font("Segoe UI", 10F);
            txtSearch.ForeColor = Color.Black;
            txtSearch.Location = new Point(5, 5);
            txtSearch.Margin = new Padding(0);
            txtSearch.Name = "txtSearch";
            txtSearch.PlaceholderText = "🔍 Search suspect...";
            txtSearch.Size = new Size(180, 23);
            txtSearch.TabIndex = 0;
            txtSearch.TextChanged += txtSearch_TextChanged;
            // 
            // panelDetails
            // 
            panelDetails.BackColor = Color.White;
            panelDetails.Dock = DockStyle.Fill;
            panelDetails.Location = new Point(0, 0);
            panelDetails.Name = "panelDetails";
            panelDetails.Size = new Size(596, 409);
            panelDetails.TabIndex = 0;
            // 
            // panelTopBar
            // 
            panelTopBar.BackColor = Color.WhiteSmoke;
            panelTopBar.Controls.Add(btnEnroll);
            panelTopBar.Dock = DockStyle.Top;
            panelTopBar.Location = new Point(0, 0);
            panelTopBar.Name = "panelTopBar";
            panelTopBar.Padding = new Padding(10, 5, 10, 5);
            panelTopBar.Size = new Size(800, 40);
            panelTopBar.TabIndex = 2;
            // 
            // btnEnroll
            // 
            btnEnroll.AutoSize = true;
            btnEnroll.Location = new Point(10, 8);
            btnEnroll.Margin = new Padding(0, 0, 10, 0);
            btnEnroll.Name = "btnEnroll";
            btnEnroll.Size = new Size(112, 30);
            btnEnroll.TabIndex = 0;
            btnEnroll.Text = "Enroll Suspect";
            btnEnroll.Click += btnEnroll_Click;
            // 
            // panelSeparator
            // 
            panelSeparator.BackColor = Color.Gray;
            panelSeparator.Dock = DockStyle.Top;
            panelSeparator.Location = new Point(0, 40);
            panelSeparator.Name = "panelSeparator";
            panelSeparator.Size = new Size(800, 1);
            panelSeparator.TabIndex = 1;
            // 
            // SuspectListControl
            // 
            Controls.Add(splitContainer);
            Controls.Add(panelSeparator);
            Controls.Add(panelTopBar);
            Name = "SuspectListControl";
            Size = new Size(800, 450);
            splitContainer.Panel1.ResumeLayout(false);
            splitContainer.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitContainer).EndInit();
            splitContainer.ResumeLayout(false);
            leftPanel.ResumeLayout(false);
            panelSearch.ResumeLayout(false);
            panelSearchContainer.ResumeLayout(false);
            panelSearchContainer.PerformLayout();
            panelTopBar.ResumeLayout(false);
            panelTopBar.PerformLayout();
            ResumeLayout(false);
        }

        private Panel leftPanel;
        private Panel panelSearchContainer;
    }
}