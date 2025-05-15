namespace RailCreator
{
    partial class Form
    {
        // Removed CS0108 suppression and components declaration unchanged
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
this.CancelButton = new System.Windows.Forms.Button();
            this.LogoPictureBox = new System.Windows.Forms.PictureBox();
            this.MainTabControl = new System.Windows.Forms.TabControl();
            this.DesignSelectionTab = new System.Windows.Forms.TabPage();
            this.DesignDetailsTab = new System.Windows.Forms.TabPage();
            this.SelectDesignLabel = new System.Windows.Forms.Label();
            this.DesignComboBox = new System.Windows.Forms.ComboBox();
            this.BrowseButton = new System.Windows.Forms.Button();
            this.SearchTextBox = new System.Windows.Forms.TextBox();
            this.PreviewPictureBox = new System.Windows.Forms.PictureBox();
            this.PostsGroupBox = new System.Windows.Forms.GroupBox();
            this.RailsGroupBox = new System.Windows.Forms.GroupBox();
            this.PicketsGroupBox = new System.Windows.Forms.GroupBox();
            this.GeneralGroupBox = new System.Windows.Forms.GroupBox();
            this.PostSizeLabel = new System.Windows.Forms.Label();
            this.PostSizeComboBox = new System.Windows.Forms.ComboBox();
            this.MountingTypeLabel = new System.Windows.Forms.Label();
            this.MountingTypeComboBox = new System.Windows.Forms.ComboBox();
            this.PostRotationCheckBox = new System.Windows.Forms.CheckBox();
            this.TopCapLabel = new System.Windows.Forms.Label();
            this.TopCapComboBox = new System.Windows.Forms.ComboBox();
            this.TopCapHeightLabel = new System.Windows.Forms.Label();
            this.TopCapHeightTextBox = new System.Windows.Forms.TextBox();
            this.TopCapWidthLabel = new System.Windows.Forms.Label();
            this.TopCapWidthTextBox = new System.Windows.Forms.TextBox();
            this.TopCapWallLabel = new System.Windows.Forms.Label();
            this.TopCapWallTextBox = new System.Windows.Forms.TextBox();
            this.BottomRailLabel = new System.Windows.Forms.Label();
            this.BottomRailComboBox = new System.Windows.Forms.ComboBox();
            this.IntermediateRailLabel = new System.Windows.Forms.Label();
            this.IntermediateRailComboBox = new System.Windows.Forms.ComboBox();
            this.PicketTypeLabel = new System.Windows.Forms.Label();
            this.PicketTypeComboBox = new System.Windows.Forms.ComboBox();
            this.PicketSizeLabel = new System.Windows.Forms.Label();
            this.PicketSizeComboBox = new System.Windows.Forms.ComboBox();
            this.PicketPlacementLabel = new System.Windows.Forms.Label();
            this.PicketPlacementComboBox = new System.Windows.Forms.ComboBox();
            this.DecorativeWidthLabel = new System.Windows.Forms.Label();
            this.DecorativeWidthTextBox = new System.Windows.Forms.TextBox();
            this.RailHeightLabel = new System.Windows.Forms.Label();
            this.RailHeightTextBox = new System.Windows.Forms.TextBox();
            this.RailNameLabel = new System.Windows.Forms.Label();
            this.RailNameTextBox = new System.Windows.Forms.TextBox();
            this.SpecialInstructionsLabel = new System.Windows.Forms.Label();
            this.SpecialInstructionsComboBox = new System.Windows.Forms.ComboBox();
            this.ConfirmButton = new System.Windows.Forms.Button();
            this.SaveCustomRailButton = new System.Windows.Forms.Button();
            this.OpenDwgButton = new System.Windows.Forms.Button();
            // Added 'new' keyword to fix CS0108 warning

            this.MainTabControl.SuspendLayout();
            this.DesignSelectionTab.SuspendLayout();
            this.DesignDetailsTab.SuspendLayout();
            this.PostsGroupBox.SuspendLayout();
            this.RailsGroupBox.SuspendLayout();
            this.PicketsGroupBox.SuspendLayout();
            this.GeneralGroupBox.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.PreviewPictureBox)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.LogoPictureBox)).BeginInit();
            this.SuspendLayout();

            // LogoPictureBox
            this.LogoPictureBox.Location = new System.Drawing.Point(12, 12);
            this.LogoPictureBox.Name = "LogoPictureBox";
            this.LogoPictureBox.Size = new System.Drawing.Size(200, 50);
            this.LogoPictureBox.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.LogoPictureBox.ImageLocation = "logo.png";
            this.LogoPictureBox.TabIndex = 0;
            this.LogoPictureBox.TabStop = false;

            // MainTabControl
            this.MainTabControl.Controls.Add(this.DesignSelectionTab);
            this.MainTabControl.Controls.Add(this.DesignDetailsTab);
            this.MainTabControl.Location = new System.Drawing.Point(12, 72);
            this.MainTabControl.Name = "MainTabControl";
            this.MainTabControl.SelectedIndex = 0;
            this.MainTabControl.Size = new System.Drawing.Size(760, 480);
            this.MainTabControl.TabIndex = 1;

            // DesignSelectionTab
            this.DesignSelectionTab.Controls.Add(this.SelectDesignLabel);
            this.DesignSelectionTab.Controls.Add(this.DesignComboBox);
            this.DesignSelectionTab.Controls.Add(this.BrowseButton);
            this.DesignSelectionTab.Controls.Add(this.SearchTextBox);
            this.DesignSelectionTab.Controls.Add(this.PreviewPictureBox);
            this.DesignSelectionTab.Location = new System.Drawing.Point(4, 22);
            this.DesignSelectionTab.Name = "DesignSelectionTab";
            this.DesignSelectionTab.Padding = new System.Windows.Forms.Padding(3);
            this.DesignSelectionTab.Size = new System.Drawing.Size(752, 454);
            this.DesignSelectionTab.TabIndex = 0;
            this.DesignSelectionTab.Text = "Design Selection";

            // DesignDetailsTab
            this.DesignDetailsTab.Controls.Add(this.PostsGroupBox);
            this.DesignDetailsTab.Controls.Add(this.RailsGroupBox);
            this.DesignDetailsTab.Controls.Add(this.PicketsGroupBox);
            this.DesignDetailsTab.Controls.Add(this.GeneralGroupBox);
            this.DesignDetailsTab.Location = new System.Drawing.Point(4, 22);
            this.DesignDetailsTab.Name = "DesignDetailsTab";
            this.DesignDetailsTab.Padding = new System.Windows.Forms.Padding(3);
            this.DesignDetailsTab.Size = new System.Drawing.Size(752, 454);
            this.DesignDetailsTab.TabIndex = 1;
            this.DesignDetailsTab.Text = "Design Details";

            // SelectDesignLabel
            this.SelectDesignLabel.AutoSize = true;
            // Fixed typo: Removed stray 'this'
            this.SelectDesignLabel.Location = new System.Drawing.Point(10, 40);
            this.SelectDesignLabel.Name = "SelectDesignLabel";
            this.SelectDesignLabel.Size = new System.Drawing.Size(75, 15);
            this.SelectDesignLabel.TabIndex = 0;
            this.SelectDesignLabel.Text = "Select Design";

            // DesignComboBox
            this.DesignComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.DesignComboBox.FormattingEnabled = true;
            this.DesignComboBox.Location = new System.Drawing.Point(90, 37);
            this.DesignComboBox.Name = "DesignComboBox";
            this.DesignComboBox.Size = new System.Drawing.Size(200, 23);
            this.DesignComboBox.TabIndex = 1;

            // BrowseButton
            this.BrowseButton.Location = new System.Drawing.Point(300, 36);
            this.BrowseButton.Name = "BrowseButton";
            this.BrowseButton.Size = new System.Drawing.Size(75, 25);
            this.BrowseButton.TabIndex = 2;
            this.BrowseButton.Text = "Browse";
            this.BrowseButton.UseVisualStyleBackColor = true;

            // SearchTextBox
            this.SearchTextBox.Location = new System.Drawing.Point(90, 10);
            this.SearchTextBox.Name = "SearchTextBox";
            this.SearchTextBox.Size = new System.Drawing.Size(200, 23);
            this.SearchTextBox.TabIndex = 3;
            this.SearchTextBox.Text = "Search Designs";

            // PreviewPictureBox
            this.PreviewPictureBox.Location = new System.Drawing.Point(90, 70);
            this.PreviewPictureBox.Name = "PreviewPictureBox";
            this.PreviewPictureBox.Size = new System.Drawing.Size(300, 240);
            this.PreviewPictureBox.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.PreviewPictureBox.TabIndex = 4;
            this.PreviewPictureBox.TabStop = false;

            // PostsGroupBox
            this.PostsGroupBox.Controls.Add(this.PostSizeLabel);
            this.PostsGroupBox.Controls.Add(this.PostSizeComboBox);
            this.PostsGroupBox.Controls.Add(this.MountingTypeLabel);
            this.PostsGroupBox.Controls.Add(this.MountingTypeComboBox);
            this.PostsGroupBox.Controls.Add(this.PostRotationCheckBox);
            this.PostsGroupBox.Location = new System.Drawing.Point(10, 10);
            this.PostsGroupBox.Name = "PostsGroupBox";
            this.PostsGroupBox.Size = new System.Drawing.Size(730, 100);
            this.PostsGroupBox.TabIndex = 0;
            this.PostsGroupBox.Text = "Posts";

            // RailsGroupBox
            this.RailsGroupBox.Controls.Add(this.TopCapLabel);
            this.RailsGroupBox.Controls.Add(this.TopCapComboBox);
            this.RailsGroupBox.Controls.Add(this.TopCapHeightLabel);
            this.RailsGroupBox.Controls.Add(this.TopCapHeightTextBox);
            this.RailsGroupBox.Controls.Add(this.TopCapWidthLabel);
            this.RailsGroupBox.Controls.Add(this.TopCapWidthTextBox);
            this.RailsGroupBox.Controls.Add(this.TopCapWallLabel);
            this.RailsGroupBox.Controls.Add(this.TopCapWallTextBox);
            this.RailsGroupBox.Controls.Add(this.BottomRailLabel);
            this.RailsGroupBox.Controls.Add(this.BottomRailComboBox);
            this.RailsGroupBox.Controls.Add(this.IntermediateRailLabel);
            this.RailsGroupBox.Controls.Add(this.IntermediateRailComboBox);
            this.RailsGroupBox.Location = new System.Drawing.Point(10, 120);
            this.RailsGroupBox.Name = "RailsGroupBox";
            this.RailsGroupBox.Size = new System.Drawing.Size(730, 120);
            this.RailsGroupBox.TabIndex = 1;
            this.RailsGroupBox.Text = "Rails";

            // PicketsGroupBox
            this.PicketsGroupBox.Controls.Add(this.PicketTypeLabel);
            this.PicketsGroupBox.Controls.Add(this.PicketTypeComboBox);
            this.PicketsGroupBox.Controls.Add(this.PicketSizeLabel);
            this.PicketsGroupBox.Controls.Add(this.PicketSizeComboBox);
            this.PicketsGroupBox.Controls.Add(this.PicketPlacementLabel);
            this.PicketsGroupBox.Controls.Add(this.PicketPlacementComboBox);
            this.PicketsGroupBox.Controls.Add(this.DecorativeWidthLabel);
            this.PicketsGroupBox.Controls.Add(this.DecorativeWidthTextBox);
            this.PicketsGroupBox.Location = new System.Drawing.Point(10, 250);
            this.PicketsGroupBox.Name = "PicketsGroupBox";
            this.PicketsGroupBox.Size = new System.Drawing.Size(730, 100);
            this.PicketsGroupBox.TabIndex = 2;
            this.PicketsGroupBox.Text = "Pickets";

            // GeneralGroupBox
            this.GeneralGroupBox.Controls.Add(this.RailHeightLabel);
            this.GeneralGroupBox.Controls.Add(this.RailHeightTextBox);
            this.GeneralGroupBox.Controls.Add(this.RailNameLabel);
            this.GeneralGroupBox.Controls.Add(this.RailNameTextBox);
            this.GeneralGroupBox.Controls.Add(this.SpecialInstructionsLabel);
            this.GeneralGroupBox.Controls.Add(this.SpecialInstructionsComboBox);
            this.GeneralGroupBox.Location = new System.Drawing.Point(10, 360);
            this.GeneralGroupBox.Name = "GeneralGroupBox";
            this.GeneralGroupBox.Size = new System.Drawing.Size(730, 80);
            this.GeneralGroupBox.TabIndex = 3;
            this.GeneralGroupBox.Text = "General";

            // PostSizeLabel
            this.PostSizeLabel.AutoSize = true;
            this.PostSizeLabel.Location = new System.Drawing.Point(10, 30);
            this.PostSizeLabel.Name = "PostSizeLabel";
            this.PostSizeLabel.Size = new System.Drawing.Size(60, 15);
            this.PostSizeLabel.TabIndex = 0;
            this.PostSizeLabel.Text = "Post Size";

            // PostSizeComboBox
            this.PostSizeComboBox.FormattingEnabled = true;
            this.PostSizeComboBox.Location = new System.Drawing.Point(90, 27);
            this.PostSizeComboBox.Name = "PostSizeComboBox";
            this.PostSizeComboBox.Size = new System.Drawing.Size(150, 23);
            this.PostSizeComboBox.TabIndex = 1;

            // MountingTypeLabel
            this.MountingTypeLabel.AutoSize = true;
            this.MountingTypeLabel.Location = new System.Drawing.Point(260, 30);
            this.MountingTypeLabel.Name = "MountingTypeLabel";
            this.MountingTypeLabel.Size = new System.Drawing.Size(85, 15);
            this.MountingTypeLabel.TabIndex = 2;
            this.MountingTypeLabel.Text = "Mounting Type";

            // MountingTypeComboBox
            this.MountingTypeComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.MountingTypeComboBox.FormattingEnabled = true;
            this.MountingTypeComboBox.Location = new System.Drawing.Point(350, 27);
            this.MountingTypeComboBox.Name = "MountingTypeComboBox";
            this.MountingTypeComboBox.Size = new System.Drawing.Size(150, 23);
            this.MountingTypeComboBox.TabIndex = 3;

            // PostRotationCheckBox
            this.PostRotationCheckBox.AutoSize = true;
            this.PostRotationCheckBox.Location = new System.Drawing.Point(510, 29);
            this.PostRotationCheckBox.Name = "PostRotationCheckBox";
            this.PostRotationCheckBox.Size = new System.Drawing.Size(85, 19);
            this.PostRotationCheckBox.TabIndex = 4;
            this.PostRotationCheckBox.Text = "Rotate 90°";
            this.PostRotationCheckBox.UseVisualStyleBackColor = true;

            // TopCapLabel
            this.TopCapLabel.AutoSize = true;
            this.TopCapLabel.Location = new System.Drawing.Point(10, 30);
            this.TopCapLabel.Name = "TopCapLabel";
            this.TopCapLabel.Size = new System.Drawing.Size(50, 15);
            this.TopCapLabel.TabIndex = 0;
            this.TopCapLabel.Text = "TopCap";

            // TopCapComboBox
            this.TopCapComboBox.FormattingEnabled = true;
            this.TopCapComboBox.Location = new System.Drawing.Point(90, 27);
            this.TopCapComboBox.Name = "TopCapComboBox";
            this.TopCapComboBox.Size = new System.Drawing.Size(150, 23);
            this.TopCapComboBox.TabIndex = 1;

            // TopCapHeightLabel
            this.TopCapHeightLabel.AutoSize = true;
            this.TopCapHeightLabel.Location = new System.Drawing.Point(260, 30);
            this.TopCapHeightLabel.Name = "TopCapHeightLabel";
            this.TopCapHeightLabel.Size = new System.Drawing.Size(75, 15);
            this.TopCapHeightLabel.TabIndex = 2;
            this.TopCapHeightLabel.Text = "TopCap Height";

            // TopCapHeightTextBox
            this.TopCapHeightTextBox.Location = new System.Drawing.Point(350, 27);
            this.TopCapHeightTextBox.Name = "TopCapHeightTextBox";
            this.TopCapHeightTextBox.Size = new System.Drawing.Size(100, 23);
            this.TopCapHeightTextBox.TabIndex = 3;

            // TopCapWidthLabel
            this.TopCapWidthLabel.AutoSize = true;
            this.TopCapWidthLabel.Location = new System.Drawing.Point(460, 30);
            this.TopCapWidthLabel.Name = "TopCapWidthLabel";
            this.TopCapWidthLabel.Size = new System.Drawing.Size(70, 15);
            this.TopCapWidthLabel.TabIndex = 4;
            this.TopCapWidthLabel.Text = "TopCap Width";

            // TopCapWidthTextBox
            this.TopCapWidthTextBox.Location = new System.Drawing.Point(540, 27);
            this.TopCapWidthTextBox.Name = "TopCapWidthTextBox";
            this.TopCapWidthTextBox.Size = new System.Drawing.Size(100, 23);
            this.TopCapWidthTextBox.TabIndex = 5;

            // TopCapWallLabel
            this.TopCapWallLabel.AutoSize = true;
            this.TopCapWallLabel.Location = new System.Drawing.Point(10, 60);
            this.TopCapWallLabel.Name = "TopCapWallLabel";
            this.TopCapWallLabel.Size = new System.Drawing.Size(65, 15);
            this.TopCapWallLabel.TabIndex = 6;
            this.TopCapWallLabel.Text = "TopCap Wall";

            // TopCapWallTextBox
            this.TopCapWallTextBox.Location = new System.Drawing.Point(90, 57);
            this.TopCapWallTextBox.Name = "TopCapWallTextBox";
            this.TopCapWallTextBox.Size = new System.Drawing.Size(100, 23);
            this.TopCapWallTextBox.TabIndex = 7;

            // BottomRailLabel
            this.BottomRailLabel.AutoSize = true;
            this.BottomRailLabel.Location = new System.Drawing.Point(260, 60);
            this.BottomRailLabel.Name = "BottomRailLabel";
            this.BottomRailLabel.Size = new System.Drawing.Size(70, 15);
            this.BottomRailLabel.TabIndex = 8;
            this.BottomRailLabel.Text = "Bottom Rail";

            // BottomRailComboBox
            this.BottomRailComboBox.FormattingEnabled = true;
            this.BottomRailComboBox.Location = new System.Drawing.Point(350, 57);
            this.BottomRailComboBox.Name = "BottomRailComboBox";
            this.BottomRailComboBox.Size = new System.Drawing.Size(150, 23);
            this.BottomRailComboBox.TabIndex = 9;

            // IntermediateRailLabel
            this.IntermediateRailLabel.AutoSize = true;
            this.IntermediateRailLabel.Location = new System.Drawing.Point(510, 60);
            this.IntermediateRailLabel.Name = "IntermediateRailLabel";
            this.IntermediateRailLabel.Size = new System.Drawing.Size(95, 15);
            this.IntermediateRailLabel.TabIndex = 10;
            this.IntermediateRailLabel.Text = "Intermediate Rail";

            // IntermediateRailComboBox
            this.IntermediateRailComboBox.FormattingEnabled = true;
            this.IntermediateRailComboBox.Location = new System.Drawing.Point(610, 57);
            this.IntermediateRailComboBox.Name = "IntermediateRailComboBox";
            this.IntermediateRailComboBox.Size = new System.Drawing.Size(100, 23);
            this.IntermediateRailComboBox.TabIndex = 11;

            // PicketTypeLabel
            this.PicketTypeLabel.AutoSize = true;
            this.PicketTypeLabel.Location = new System.Drawing.Point(10, 30);
            this.PicketTypeLabel.Name = "PicketTypeLabel";
            this.PicketTypeLabel.Size = new System.Drawing.Size(70, 15);
            this.PicketTypeLabel.TabIndex = 0;
            this.PicketTypeLabel.Text = "Picket Type";

            // PicketTypeComboBox
            this.PicketTypeComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.PicketTypeComboBox.FormattingEnabled = true;
            this.PicketTypeComboBox.Location = new System.Drawing.Point(90, 27);
            this.PicketTypeComboBox.Name = "PicketTypeComboBox";
            this.PicketTypeComboBox.Size = new System.Drawing.Size(150, 23);
            this.PicketTypeComboBox.TabIndex = 1;

            // PicketSizeLabel
            this.PicketSizeLabel.AutoSize = true;
            this.PicketSizeLabel.Location = new System.Drawing.Point(260, 30);
            this.PicketSizeLabel.Name = "PicketSizeLabel";
            this.PicketSizeLabel.Size = new System.Drawing.Size(65, 15);
            this.PicketSizeLabel.TabIndex = 2;
            this.PicketSizeLabel.Text = "Picket Size";

            // PicketSizeComboBox
            this.PicketSizeComboBox.FormattingEnabled = true;
            this.PicketSizeComboBox.Location = new System.Drawing.Point(350, 27);
            this.PicketSizeComboBox.Name = "PicketSizeComboBox";
            this.PicketSizeComboBox.Size = new System.Drawing.Size(150, 23);
            this.PicketSizeComboBox.TabIndex = 3;

            // PicketPlacementLabel
            this.PicketPlacementLabel.AutoSize = true;
            this.PicketPlacementLabel.Location = new System.Drawing.Point(510, 30);
            this.PicketPlacementLabel.Name = "PicketPlacementLabel";
            this.PicketPlacementLabel.Size = new System.Drawing.Size(90, 15);
            this.PicketPlacementLabel.TabIndex = 4;
            this.PicketPlacementLabel.Text = "Picket Placement";

            // PicketPlacementComboBox
            this.PicketPlacementComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.PicketPlacementComboBox.FormattingEnabled = true;
            this.PicketPlacementComboBox.Location = new System.Drawing.Point(610, 27);
            this.PicketPlacementComboBox.Name = "PicketPlacementComboBox";
            this.PicketPlacementComboBox.Size = new System.Drawing.Size(100, 23);
            this.PicketPlacementComboBox.TabIndex = 5;

            // DecorativeWidthLabel
            this.DecorativeWidthLabel.AutoSize = true;
            this.DecorativeWidthLabel.Location = new System.Drawing.Point(10, 60);
            this.DecorativeWidthLabel.Name = "DecorativeWidthLabel";
            this.DecorativeWidthLabel.Size = new System.Drawing.Size(90, 15);
            this.DecorativeWidthLabel.TabIndex = 6;
            this.DecorativeWidthLabel.Text = "Decorative Width";

            // DecorativeWidthTextBox
            this.DecorativeWidthTextBox.Location = new System.Drawing.Point(90, 57);
            this.DecorativeWidthTextBox.Name = "DecorativeWidthTextBox";
            this.DecorativeWidthTextBox.Size = new System.Drawing.Size(100, 23);
            this.DecorativeWidthTextBox.TabIndex = 7;

            // RailHeightLabel
            this.RailHeightLabel.AutoSize = true;
            this.RailHeightLabel.Location = new System.Drawing.Point(10, 30);
            this.RailHeightLabel.Name = "RailHeightLabel";
            this.RailHeightLabel.Size = new System.Drawing.Size(65, 15);
            this.RailHeightLabel.TabIndex = 0;
            this.RailHeightLabel.Text = "Rail Height";

            // RailHeightTextBox
            this.RailHeightTextBox.Location = new System.Drawing.Point(90, 27);
            this.RailHeightTextBox.Name = "RailHeightTextBox";
            this.RailHeightTextBox.Size = new System.Drawing.Size(100, 23);
            this.RailHeightTextBox.TabIndex = 1;

            // RailNameLabel
            this.RailNameLabel.AutoSize = true;
            // Fixed typo: Corrected duplicate RailHeightLabel reference
            this.RailNameLabel.Location = new System.Drawing.Point(260, 30);
            this.RailNameLabel.Name = "RailNameLabel";
            this.RailNameLabel.Size = new System.Drawing.Size(60, 15);
            this.RailNameLabel.TabIndex = 2;
            this.RailNameLabel.Text = "Rail Name";

            // RailNameTextBox
            this.RailNameTextBox.Location = new System.Drawing.Point(350, 27);
            this.RailNameTextBox.Name = "RailNameTextBox";
            this.RailNameTextBox.Size = new System.Drawing.Size(150, 23);
            this.RailNameTextBox.TabIndex = 3;

            // SpecialInstructionsLabel
            this.SpecialInstructionsLabel.AutoSize = true;
            this.SpecialInstructionsLabel.Location = new System.Drawing.Point(510, 30);
            this.SpecialInstructionsLabel.Name = "SpecialInstructionsLabel";
            this.SpecialInstructionsLabel.Size = new System.Drawing.Size(105, 15);
            this.SpecialInstructionsLabel.TabIndex = 4;
            this.SpecialInstructionsLabel.Text = "Special Instructions";

            // SpecialInstructionsComboBox
            this.SpecialInstructionsComboBox.FormattingEnabled = true;
            this.SpecialInstructionsComboBox.Location = new System.Drawing.Point(620, 27);
            this.SpecialInstructionsComboBox.Name = "SpecialInstructionsComboBox";
            this.SpecialInstructionsComboBox.Size = new System.Drawing.Size(100, 23);
            this.SpecialInstructionsComboBox.TabIndex = 5;

            // ConfirmButton
            this.ConfirmButton.Location = new System.Drawing.Point(12, 580);
            this.ConfirmButton.Name = "ConfirmButton";
            this.ConfirmButton.Size = new System.Drawing.Size(100, 30);
            this.ConfirmButton.TabIndex = 2;
            this.ConfirmButton.Text = "Confirm";
            this.ConfirmButton.UseVisualStyleBackColor = true;
            this.ConfirmButton.Click += new System.EventHandler(this.ConfirmButton_Click);

            // SaveCustomRailButton
            this.SaveCustomRailButton.Location = new System.Drawing.Point(122, 580);
            this.SaveCustomRailButton.Name = "SaveCustomRailButton";
            this.SaveCustomRailButton.Size = new System.Drawing.Size(120, 30);
            this.SaveCustomRailButton.TabIndex = 3;
            this.SaveCustomRailButton.Text = "Save Custom Rail";
            this.SaveCustomRailButton.UseVisualStyleBackColor = true;
            this.SaveCustomRailButton.Click += new System.EventHandler(this.SaveCustomRailButton_Click);

            // OpenDwgButton
            this.OpenDwgButton.Location = new System.Drawing.Point(252, 580);
            this.OpenDwgButton.Name = "OpenDwgButton";
            this.OpenDwgButton.Size = new System.Drawing.Size(100, 30);
            this.OpenDwgButton.TabIndex = 4;
            this.OpenDwgButton.Text = "Open DWG";
            this.OpenDwgButton.UseVisualStyleBackColor = true;
            this.OpenDwgButton.Click += new System.EventHandler(this.OpenDwgButton_Click);

            // CancelButton
this.CancelButton = new System.Windows.Forms.Button();
            this.CancelButton.Location = new System.Drawing.Point(362, 580);
            this.CancelButton.Name = "CancelButton";
            this.CancelButton.Size = new System.Drawing.Size(100, 30);
            this.CancelButton.TabIndex = 5;
            this.CancelButton.Text = "Cancel";
            this.CancelButton.UseVisualStyleBackColor = true;
            this.CancelButton.Click += new System.EventHandler(this.CancelButton_Click);

            // Form Properties
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(240)))), ((int)(((byte)(240)))), ((int)(((byte)(240)))));
            this.ClientSize = new System.Drawing.Size(784, 642);
            this.Controls.Add(this.LogoPictureBox);
            this.Controls.Add(this.ConfirmButton);
            this.Controls.Add(this.SaveCustomRailButton);
            this.Controls.Add(this.OpenDwgButton);
            this.Controls.Add(this.CancelButton);
            this.Controls.Add(this.MainTabControl);
            this.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Name = "Form";
            this.Text = "Railing Designer";
            this.MainTabControl.ResumeLayout(false);
            this.DesignSelectionTab.ResumeLayout(false);
            this.DesignSelectionTab.PerformLayout();
            this.DesignDetailsTab.ResumeLayout(false);
            this.PostsGroupBox.ResumeLayout(false);
            this.PostsGroupBox.PerformLayout();
            this.RailsGroupBox.ResumeLayout(false);
            this.RailsGroupBox.PerformLayout();
            this.PicketsGroupBox.ResumeLayout(false);
            this.PicketsGroupBox.PerformLayout();
            this.GeneralGroupBox.ResumeLayout(false);
            this.GeneralGroupBox.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.PreviewPictureBox)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.LogoPictureBox)).EndInit();
            this.ResumeLayout(false);
        }

        private System.Windows.Forms.PictureBox LogoPictureBox;
        private System.Windows.Forms.TabControl MainTabControl;
        private System.Windows.Forms.TabPage DesignSelectionTab;
        private System.Windows.Forms.TabPage DesignDetailsTab;
        private System.Windows.Forms.Label SelectDesignLabel;
        private System.Windows.Forms.ComboBox DesignComboBox;
        private System.Windows.Forms.Button BrowseButton;
        private System.Windows.Forms.TextBox SearchTextBox;
        private System.Windows.Forms.PictureBox PreviewPictureBox;
        private System.Windows.Forms.GroupBox PostsGroupBox;
        private System.Windows.Forms.GroupBox RailsGroupBox;
        private System.Windows.Forms.GroupBox PicketsGroupBox;
        private System.Windows.Forms.GroupBox GeneralGroupBox;
        private System.Windows.Forms.Label PostSizeLabel;
        private System.Windows.Forms.ComboBox PostSizeComboBox;
        private System.Windows.Forms.Label MountingTypeLabel;
        private System.Windows.Forms.ComboBox MountingTypeComboBox;
        private System.Windows.Forms.CheckBox PostRotationCheckBox;
        private System.Windows.Forms.Label TopCapLabel;
        private System.Windows.Forms.ComboBox TopCapComboBox;
        private System.Windows.Forms.Label TopCapHeightLabel;
        private System.Windows.Forms.TextBox TopCapHeightTextBox;
        private System.Windows.Forms.Label TopCapWidthLabel;
        private System.Windows.Forms.TextBox TopCapWidthTextBox;
        private System.Windows.Forms.Label TopCapWallLabel;
        private System.Windows.Forms.TextBox TopCapWallTextBox;
        private System.Windows.Forms.Label BottomRailLabel;
        private System.Windows.Forms.ComboBox BottomRailComboBox;
        private System.Windows.Forms.Label IntermediateRailLabel;
        private System.Windows.Forms.ComboBox IntermediateRailComboBox;
        private System.Windows.Forms.Label PicketTypeLabel;
        private System.Windows.Forms.ComboBox PicketTypeComboBox;
        private System.Windows.Forms.Label PicketSizeLabel;
        private System.Windows.Forms.ComboBox PicketSizeComboBox;
        private System.Windows.Forms.Label PicketPlacementLabel;
        private System.Windows.Forms.ComboBox PicketPlacementComboBox;
        private System.Windows.Forms.Label DecorativeWidthLabel;
        private System.Windows.Forms.TextBox DecorativeWidthTextBox;
        private System.Windows.Forms.Label RailHeightLabel;
        private System.Windows.Forms.TextBox RailHeightTextBox;
        private System.Windows.Forms.Label RailNameLabel;
        private System.Windows.Forms.TextBox RailNameTextBox;
        private System.Windows.Forms.Label SpecialInstructionsLabel;
        private System.Windows.Forms.ComboBox SpecialInstructionsComboBox;
        private System.Windows.Forms.Button ConfirmButton;
        private System.Windows.Forms.Button SaveCustomRailButton;
        private System.Windows.Forms.Button OpenDwgButton;
        private new System.Windows.Forms.Button CancelButton;
    }
}