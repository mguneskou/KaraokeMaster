namespace KaraokeMaster.App.Forms;

partial class ControlForm
{
    private System.ComponentModel.IContainer components = null;

    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }
        base.Dispose(disposing);
    }

    #region Windows Form Designer generated code

    private MenuStrip menuStrip;
    private ToolStripMenuItem fileMenu;
    private ToolStripMenuItem manageWatchedFoldersMenuItem;
    private ToolStripMenuItem exitMenuItem;
    private ToolStripMenuItem viewMenu;
    private ToolStripMenuItem openPerformerWindowMenuItem;
    private StatusStrip statusStrip;
    private ToolStripStatusLabel dataRootStatusLabel;
    private SplitContainer mainSplitContainer;
    private GroupBox libraryGroupBox;
    private Label libraryPlaceholderLabel;
    private SplitContainer rightSplitContainer;
    private GroupBox queueGroupBox;
    private Label queuePlaceholderLabel;
    private GroupBox nowPlayingGroupBox;
    private Label nowPlayingPlaceholderLabel;

    private void InitializeComponent()
    {
        this.menuStrip = new MenuStrip();
        this.fileMenu = new ToolStripMenuItem();
        this.manageWatchedFoldersMenuItem = new ToolStripMenuItem();
        this.exitMenuItem = new ToolStripMenuItem();
        this.viewMenu = new ToolStripMenuItem();
        this.openPerformerWindowMenuItem = new ToolStripMenuItem();
        this.statusStrip = new StatusStrip();
        this.dataRootStatusLabel = new ToolStripStatusLabel();
        this.mainSplitContainer = new SplitContainer();
        this.libraryGroupBox = new GroupBox();
        this.libraryPlaceholderLabel = new Label();
        this.rightSplitContainer = new SplitContainer();
        this.queueGroupBox = new GroupBox();
        this.queuePlaceholderLabel = new Label();
        this.nowPlayingGroupBox = new GroupBox();
        this.nowPlayingPlaceholderLabel = new Label();
        ((System.ComponentModel.ISupportInitialize)(this.mainSplitContainer)).BeginInit();
        this.mainSplitContainer.Panel1.SuspendLayout();
        this.mainSplitContainer.Panel2.SuspendLayout();
        this.mainSplitContainer.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)(this.rightSplitContainer)).BeginInit();
        this.rightSplitContainer.Panel1.SuspendLayout();
        this.rightSplitContainer.Panel2.SuspendLayout();
        this.rightSplitContainer.SuspendLayout();
        this.libraryGroupBox.SuspendLayout();
        this.queueGroupBox.SuspendLayout();
        this.nowPlayingGroupBox.SuspendLayout();
        this.SuspendLayout();
        //
        // menuStrip
        //
        this.menuStrip.Items.AddRange(new ToolStripItem[] { this.fileMenu, this.viewMenu });
        this.menuStrip.Location = new Point(0, 0);
        this.menuStrip.Name = "menuStrip";
        this.menuStrip.Size = new Size(1100, 24);
        //
        // fileMenu
        //
        this.fileMenu.DropDownItems.AddRange(new ToolStripItem[] { this.manageWatchedFoldersMenuItem, this.exitMenuItem });
        this.fileMenu.Text = "&File";
        //
        // manageWatchedFoldersMenuItem
        //
        this.manageWatchedFoldersMenuItem.Text = "Manage Watched &Folders...";
        //
        // exitMenuItem
        //
        this.exitMenuItem.Text = "E&xit";
        this.exitMenuItem.Click += (s, e) => this.Close();
        //
        // viewMenu
        //
        this.viewMenu.DropDownItems.AddRange(new ToolStripItem[] { this.openPerformerWindowMenuItem });
        this.viewMenu.Text = "&View";
        //
        // openPerformerWindowMenuItem
        //
        this.openPerformerWindowMenuItem.Text = "Open &Performer Window";
        //
        // statusStrip
        //
        this.statusStrip.Items.AddRange(new ToolStripItem[] { this.dataRootStatusLabel });
        this.statusStrip.Location = new Point(0, 578);
        this.statusStrip.Name = "statusStrip";
        this.statusStrip.Size = new Size(1100, 22);
        //
        // dataRootStatusLabel
        //
        this.dataRootStatusLabel.Text = "data root: (resolving...)";
        //
        // mainSplitContainer
        //
        this.mainSplitContainer.Dock = DockStyle.Fill;
        this.mainSplitContainer.Location = new Point(0, 24);
        this.mainSplitContainer.Name = "mainSplitContainer";
        this.mainSplitContainer.Panel1.Controls.Add(this.libraryGroupBox);
        this.mainSplitContainer.Panel2.Controls.Add(this.rightSplitContainer);
        this.mainSplitContainer.Size = new Size(1100, 554);
        this.mainSplitContainer.SplitterDistance = 620;
        this.mainSplitContainer.TabIndex = 0;
        //
        // libraryGroupBox
        //
        this.libraryGroupBox.Controls.Add(this.libraryPlaceholderLabel);
        this.libraryGroupBox.Dock = DockStyle.Fill;
        this.libraryGroupBox.Text = "Library";
        this.libraryGroupBox.Padding = new Padding(8);
        //
        // libraryPlaceholderLabel
        //
        this.libraryPlaceholderLabel.AutoSize = true;
        this.libraryPlaceholderLabel.Location = new Point(12, 28);
        this.libraryPlaceholderLabel.Text = "Search + song grid go here (watched-folder scan, milestone 3).";
        //
        // rightSplitContainer
        //
        this.rightSplitContainer.Dock = DockStyle.Fill;
        this.rightSplitContainer.Orientation = Orientation.Horizontal;
        this.rightSplitContainer.Panel1.Controls.Add(this.queueGroupBox);
        this.rightSplitContainer.Panel2.Controls.Add(this.nowPlayingGroupBox);
        this.rightSplitContainer.Size = new Size(476, 554);
        this.rightSplitContainer.SplitterDistance = 320;
        //
        // queueGroupBox
        //
        this.queueGroupBox.Controls.Add(this.queuePlaceholderLabel);
        this.queueGroupBox.Dock = DockStyle.Fill;
        this.queueGroupBox.Text = "Playlists && Up Next Queue";
        this.queueGroupBox.Padding = new Padding(8);
        //
        // queuePlaceholderLabel
        //
        this.queuePlaceholderLabel.AutoSize = true;
        this.queuePlaceholderLabel.Location = new Point(12, 28);
        this.queuePlaceholderLabel.Text = "Playlist panel + drag-reorder queue with singer names go here (milestone 9).";
        //
        // nowPlayingGroupBox
        //
        this.nowPlayingGroupBox.Controls.Add(this.nowPlayingPlaceholderLabel);
        this.nowPlayingGroupBox.Dock = DockStyle.Fill;
        this.nowPlayingGroupBox.Text = "Now Playing / Mixer";
        this.nowPlayingGroupBox.Padding = new Padding(8);
        //
        // nowPlayingPlaceholderLabel
        //
        this.nowPlayingPlaceholderLabel.AutoSize = true;
        this.nowPlayingPlaceholderLabel.Location = new Point(12, 28);
        this.nowPlayingPlaceholderLabel.Text = "Transport, pitch/tempo, mic gain/echo, device pickers go here (milestones 4-7).";
        //
        // ControlForm
        //
        this.ClientSize = new Size(1100, 600);
        this.Controls.Add(this.mainSplitContainer);
        this.Controls.Add(this.statusStrip);
        this.Controls.Add(this.menuStrip);
        this.MainMenuStrip = this.menuStrip;
        this.MinimumSize = new Size(900, 500);
        this.Name = "ControlForm";
        this.Text = "KaraokeMaster — Control";
        this.mainSplitContainer.Panel1.ResumeLayout(false);
        this.mainSplitContainer.Panel2.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)(this.mainSplitContainer)).EndInit();
        this.mainSplitContainer.ResumeLayout(false);
        this.rightSplitContainer.Panel1.ResumeLayout(false);
        this.rightSplitContainer.Panel2.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)(this.rightSplitContainer)).EndInit();
        this.rightSplitContainer.ResumeLayout(false);
        this.libraryGroupBox.ResumeLayout(false);
        this.libraryGroupBox.PerformLayout();
        this.queueGroupBox.ResumeLayout(false);
        this.queueGroupBox.PerformLayout();
        this.nowPlayingGroupBox.ResumeLayout(false);
        this.nowPlayingGroupBox.PerformLayout();
        this.ResumeLayout(false);
        this.PerformLayout();
    }

    #endregion
}
