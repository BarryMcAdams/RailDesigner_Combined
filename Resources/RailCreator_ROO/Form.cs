using System;
using System.IO;
using System.Linq;
// using System.Reflection.Emit; // This was not used in the provided Form.cs code. Consider removing if not needed project-wide.
using System.Windows.Forms;
using Autodesk.AutoCAD.ApplicationServices; // Keep if AutoCAD integration is intended
// using static Autodesk.AutoCAD.LayerManager.LayerFilter; // This was not used in the provided Form.cs code. Consider removing if not needed.

namespace RailCreator
{
    public partial class Form : System.Windows.Forms.Form
    {
        // --- Fields for UI Controls (typically in Form.Designer.cs) ---
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.ComboBox DesignComboBox;
        private System.Windows.Forms.ComboBox PostSizeComboBox;
        private System.Windows.Forms.ComboBox MountingTypeComboBox;
        private System.Windows.Forms.ComboBox TopCapComboBox;
        private System.Windows.Forms.ComboBox BottomRailComboBox;
        private System.Windows.Forms.ComboBox PicketTypeComboBox;
        private System.Windows.Forms.ComboBox PicketSizeComboBox;
        private System.Windows.Forms.ComboBox PicketPlacementComboBox;
        private System.Windows.Forms.ComboBox IntermediateRailComboBox;
        private System.Windows.Forms.ComboBox SpecialInstructionsComboBox;
        private System.Windows.Forms.TextBox DecorativeWidthTextBox;
        private System.Windows.Forms.Label DecorativeWidthLabel;
        private System.Windows.Forms.TextBox TopCapHeightTextBox;
        private System.Windows.Forms.TextBox TopCapWidthTextBox;
        private System.Windows.Forms.TextBox TopCapWallTextBox;
        private System.Windows.Forms.TextBox RailHeightTextBox;
        private System.Windows.Forms.TextBox RailNameTextBox;
        private System.Windows.Forms.Button OpenDwgButton;
        private System.Windows.Forms.PictureBox PreviewPictureBox;
        private System.Windows.Forms.Button ConfirmButton;
        private System.Windows.Forms.Button SaveCustomRailButton;
        private System.Windows.Forms.Button CancelButtonControl; // Renamed to avoid conflict with Form.CancelButton property if it exists

        private RailingData railingData;

        public Form()
        {
            InitializeComponent(); // This method is now defined below
            LoadDesigns();
            PopulateDropDowns();
            ConfigureDynamicControls();
        }

        // --- Minimal InitializeComponent (normally auto-generated in Form.Designer.cs) ---
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();

            // Instantiate controls
            this.DesignComboBox = new System.Windows.Forms.ComboBox();
            this.PostSizeComboBox = new System.Windows.Forms.ComboBox();
            this.MountingTypeComboBox = new System.Windows.Forms.ComboBox();
            this.TopCapComboBox = new System.Windows.Forms.ComboBox();
            this.BottomRailComboBox = new System.Windows.Forms.ComboBox();
            this.PicketTypeComboBox = new System.Windows.Forms.ComboBox();
            this.PicketSizeComboBox = new System.Windows.Forms.ComboBox();
            this.PicketPlacementComboBox = new System.Windows.Forms.ComboBox();
            this.IntermediateRailComboBox = new System.Windows.Forms.ComboBox();
            this.SpecialInstructionsComboBox = new System.Windows.Forms.ComboBox();
            this.DecorativeWidthTextBox = new System.Windows.Forms.TextBox();
            this.DecorativeWidthLabel = new System.Windows.Forms.Label();
            this.TopCapHeightTextBox = new System.Windows.Forms.TextBox();
            this.TopCapWidthTextBox = new System.Windows.Forms.TextBox();
            this.TopCapWallTextBox = new System.Windows.Forms.TextBox();
            this.RailHeightTextBox = new System.Windows.Forms.TextBox();
            this.RailNameTextBox = new System.Windows.Forms.TextBox();
            this.OpenDwgButton = new System.Windows.Forms.Button();
            this.PreviewPictureBox = new System.Windows.Forms.PictureBox();
            this.ConfirmButton = new System.Windows.Forms.Button();
            this.SaveCustomRailButton = new System.Windows.Forms.Button();
            this.CancelButtonControl = new System.Windows.Forms.Button(); // Use the renamed control

            // Basic Form Properties
            this.SuspendLayout();
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F); // Example values
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 600); // Example size
            this.Name = "RailCreatorForm";
            this.Text = "Rail Creator";

            // Set control names (useful for debugging and if accessed by name)
            this.DesignComboBox.Name = "DesignComboBox";
            this.PostSizeComboBox.Name = "PostSizeComboBox";
            this.MountingTypeComboBox.Name = "MountingTypeComboBox";
            this.TopCapComboBox.Name = "TopCapComboBox";
            this.BottomRailComboBox.Name = "BottomRailComboBox";
            this.PicketTypeComboBox.Name = "PicketTypeComboBox";
            this.PicketSizeComboBox.Name = "PicketSizeComboBox";
            this.PicketPlacementComboBox.Name = "PicketPlacementComboBox";
            this.IntermediateRailComboBox.Name = "IntermediateRailComboBox";
            this.SpecialInstructionsComboBox.Name = "SpecialInstructionsComboBox";
            this.DecorativeWidthTextBox.Name = "DecorativeWidthTextBox";
            this.DecorativeWidthLabel.Name = "DecorativeWidthLabel";
            // ... (set names for all other controls if needed) ...
            this.PreviewPictureBox.Name = "PreviewPictureBox";
            ((System.ComponentModel.ISupportInitialize)(this.PreviewPictureBox)).BeginInit();


            this.ConfirmButton.Name = "ConfirmButton";
            this.SaveCustomRailButton.Name = "SaveCustomRailButton";
            this.OpenDwgButton.Name = "OpenDwgButton";
            this.CancelButtonControl.Name = "CancelButtonControl"; // Use the renamed control

            // Wire up event handlers defined in this file
            // Note: DesignComboBox and PicketTypeComboBox SelectedIndexChanged are wired in ConfigureDynamicControls()
            this.ConfirmButton.Click += new System.EventHandler(this.ConfirmButton_Click);
            this.SaveCustomRailButton.Click += new System.EventHandler(this.SaveCustomRailButton_Click);
            this.OpenDwgButton.Click += new System.EventHandler(this.OpenDwgButton_Click);
            this.CancelButtonControl.Click += new System.EventHandler(this.CancelButton_Click); // Connect to renamed control

            // Add controls to the form's Controls collection
            // This is essential for them to be visible.
            // The actual layout (position, size) would be set here by the designer.
            // For this example, they are just added.
            this.Controls.Add(this.DesignComboBox);
            this.Controls.Add(this.PostSizeComboBox);
            this.Controls.Add(this.MountingTypeComboBox);
            this.Controls.Add(this.TopCapComboBox);
            this.Controls.Add(this.BottomRailComboBox);
            this.Controls.Add(this.PicketTypeComboBox);
            this.Controls.Add(this.PicketSizeComboBox);
            this.Controls.Add(this.PicketPlacementComboBox);
            this.Controls.Add(this.IntermediateRailComboBox);
            this.Controls.Add(this.SpecialInstructionsComboBox);
            this.Controls.Add(this.DecorativeWidthTextBox);
            this.Controls.Add(this.DecorativeWidthLabel);
            this.Controls.Add(this.TopCapHeightTextBox);
            this.Controls.Add(this.TopCapWidthTextBox);
            this.Controls.Add(this.TopCapWallTextBox);
            this.Controls.Add(this.RailHeightTextBox);
            this.Controls.Add(this.RailNameTextBox);
            this.Controls.Add(this.OpenDwgButton);
            this.Controls.Add(this.PreviewPictureBox);
            this.Controls.Add(this.ConfirmButton);
            this.Controls.Add(this.SaveCustomRailButton);
            this.Controls.Add(this.CancelButtonControl); // Add the renamed control

            ((System.ComponentModel.ISupportInitialize)(this.PreviewPictureBox)).EndInit();
            this.ResumeLayout(false);
            // this.PerformLayout(); // If layout changes were made that need immediate application
        }

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        // --- Your existing Form Logic ---
        private void LoadDesigns()
        {
            try
            {
                railingData = Data.LoadRailingData(); // Assumes Data class and its methods exist
                DesignComboBox.Items.Clear();
                if (railingData?.Designs == null || !railingData.Designs.Any())
                {
                    MessageBox.Show("No designs found. Please ensure RailDesigns.json exists and is accessible.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                var sortedDesigns = railingData.Designs.OrderBy(d => d.DesignName).ToList();
                foreach (var design in sortedDesigns)
                {
                    DesignComboBox.Items.Add(design.DesignName);
                }
                if (DesignComboBox.Items.Count > 0)
                {
                    DesignComboBox.SelectedIndex = 0;
                    // UpdateFormFields will be called by SelectedIndexChanged event if wired up before this,
                    // or explicitly if not. Given ConfigureDynamicControls wires it, it should be fine.
                    // If an issue, call explicitly: UpdateFormFields(sortedDesigns[0]);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading designs: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void PopulateDropDowns()
        {
            if (railingData == null || railingData.Designs == null) return;

            // Make sure railingData.Designs has elements before trying to Select from it.
            if (!railingData.Designs.Any()) return;

            PostSizeComboBox.Items.Clear();
            PostSizeComboBox.Items.AddRange(railingData.Designs.Select(d => d.PostSize).Distinct().OrderBy(s => s).ToArray());

            MountingTypeComboBox.Items.Clear();
            MountingTypeComboBox.Items.AddRange(railingData.Designs.Select(d => d.MountingType).Distinct().OrderBy(s => s).ToArray());

            TopCapComboBox.Items.Clear();
            TopCapComboBox.Items.AddRange(railingData.Designs.Select(d => d.TopCap).Distinct().OrderBy(s => s).ToArray());

            BottomRailComboBox.Items.Clear();
            BottomRailComboBox.Items.AddRange(railingData.Designs.Select(d => d.BottomRail).Distinct().OrderBy(s => s).ToArray());

            PicketTypeComboBox.Items.Clear();
            PicketTypeComboBox.Items.AddRange(railingData.Designs.Select(d => d.PicketType).Distinct().OrderBy(s => s).ToArray());

            PicketSizeComboBox.Items.Clear();
            PicketSizeComboBox.Items.AddRange(railingData.Designs.Select(d => d.PicketSize).Distinct().OrderBy(s => s).ToArray());

            PicketPlacementComboBox.Items.Clear();
            PicketPlacementComboBox.Items.AddRange(railingData.Designs.Select(d => d.PicketPlacement).Distinct().OrderBy(s => s).ToArray());

            IntermediateRailComboBox.Items.Clear();
            IntermediateRailComboBox.Items.AddRange(railingData.Designs.Select(d => d.IntermediateRail).Distinct().OrderBy(s => s).ToArray());

            SpecialInstructionsComboBox.Items.Clear();
            SpecialInstructionsComboBox.Items.AddRange(railingData.Designs.Select(d => d.SpecialInstructions).Distinct().OrderBy(s => s).ToArray());
        }

        private void ConfigureDynamicControls()
        {
            DecorativeWidthTextBox.Visible = false;
            DecorativeWidthLabel.Visible = false;

            DesignComboBox.SelectedIndexChanged += (s, e) =>
            {
                if (DesignComboBox.SelectedItem != null && railingData != null && railingData.Designs != null) // Added null check for railingData.Designs
                {
                    var selectedDesign = railingData.Designs.Find(d => d.DesignName == DesignComboBox.SelectedItem.ToString());
                    if (selectedDesign != null)
                    {
                        UpdateFormFields(selectedDesign);
                    }
                }
            };

            PicketTypeComboBox.SelectedIndexChanged += (s, e) =>
            {
                bool isDeco = PicketTypeComboBox.SelectedItem?.ToString() == "DECO";
                DecorativeWidthTextBox.Visible = isDeco;
                DecorativeWidthLabel.Visible = isDeco;
            };
        }

        private void UpdateFormFields(RailingDesign selectedDesign) // Assumes RailingDesign class exists
        {
            PostSizeComboBox.SelectedItem = selectedDesign.PostSize;
            MountingTypeComboBox.SelectedItem = selectedDesign.MountingType;
            TopCapComboBox.SelectedItem = selectedDesign.TopCap;
            TopCapHeightTextBox.Text = selectedDesign.TopCapHeight.ToString();
            TopCapWidthTextBox.Text = selectedDesign.TopCapWidth.ToString();
            TopCapWallTextBox.Text = selectedDesign.TopCapWall.ToString();
            BottomRailComboBox.SelectedItem = selectedDesign.BottomRail;
            PicketTypeComboBox.SelectedItem = selectedDesign.PicketType;
            PicketSizeComboBox.SelectedItem = selectedDesign.PicketSize;
            PicketPlacementComboBox.SelectedItem = selectedDesign.PicketPlacement;
            RailHeightTextBox.Text = selectedDesign.RailHeight.ToString();
            IntermediateRailComboBox.SelectedItem = selectedDesign.IntermediateRail;
            RailNameTextBox.Text = selectedDesign.DesignName;
            SpecialInstructionsComboBox.SelectedItem = selectedDesign.SpecialInstructions;

            DecorativeWidthTextBox.Visible = selectedDesign.PicketType == "DECO";
            DecorativeWidthLabel.Visible = selectedDesign.PicketType == "DECO";
            if (selectedDesign.PicketType == "DECO")
            {
                DecorativeWidthTextBox.Text = selectedDesign.DecorativeWidth.HasValue ? selectedDesign.DecorativeWidth.Value.ToString() : string.Empty;
            }
            else
            {
                DecorativeWidthTextBox.Text = string.Empty; // Clear if not DECO
            }


            OpenDwgButton.Enabled = !string.IsNullOrEmpty(selectedDesign.DwgLink) && selectedDesign.DwgLink != "N/A" && File.Exists(selectedDesign.DwgLink);
            PreviewPictureBox.ImageLocation = !string.IsNullOrEmpty(selectedDesign.ImageLink) && selectedDesign.ImageLink != "N/A" && File.Exists(selectedDesign.ImageLink) ? selectedDesign.ImageLink : null;
            if (PreviewPictureBox.ImageLocation == null)
            {
                PreviewPictureBox.Image = null; // Clear image if not found
            }
        }

        private void ConfirmButton_Click(object sender, EventArgs e)
        {
            if (!ValidateInputs())
            {
                MessageBox.Show("Please correct invalid inputs.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var design = new RailingDesign // Assumes RailingDesign class exists and has these properties
            {
                DesignName = RailNameTextBox.Text,
                PostSize = PostSizeComboBox.SelectedItem?.ToString() ?? "",
                MountingType = MountingTypeComboBox.SelectedItem?.ToString() ?? "",
                TopCap = TopCapComboBox.SelectedItem?.ToString() ?? "",
                TopCapHeight = double.Parse(TopCapHeightTextBox.Text),
                TopCapWidth = double.Parse(TopCapWidthTextBox.Text),
                TopCapWall = double.Parse(TopCapWallTextBox.Text),
                BottomRail = BottomRailComboBox.SelectedItem?.ToString() ?? "",
                PicketType = PicketTypeComboBox.SelectedItem?.ToString() ?? "",
                PicketSize = PicketSizeComboBox.SelectedItem?.ToString() ?? "",
                PicketPlacement = PicketPlacementComboBox.SelectedItem?.ToString() ?? "",
                RailHeight = double.Parse(RailHeightTextBox.Text),
                IntermediateRail = IntermediateRailComboBox.SelectedItem?.ToString() ?? "",
                ImageLink = "N/A", // Consider updating this if PreviewPictureBox.ImageLocation is set
                DwgLink = "N/A",   // Consider updating this if a DWG is associated
                SpecialInstructions = SpecialInstructionsComboBox.SelectedItem?.ToString() ?? "",
                DecorativeWidth = PicketTypeComboBox.SelectedItem?.ToString() == "DECO" && double.TryParse(DecorativeWidthTextBox.Text, out double width) ? (double?)width : null
            };

            // It's unusual to set DialogResult and then call another method that might take time or interact with AutoCAD.
            // Typically, DialogResult is set just before closing.
            // Generator.GenerateRailing(design); // This might take time or throw exceptions.

            try
            {
                Generator.GenerateRailing(design); // Assumes Generator class and its methods exist
                this.DialogResult = System.Windows.Forms.DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error during railing generation: {ex.Message}", "Generation Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                // Optionally, do not close the form or set DialogResult to something else.
            }
        }

        private void SaveCustomRailButton_Click(object sender, EventArgs e)
        {
            if (!ValidateInputs() || string.IsNullOrWhiteSpace(RailNameTextBox.Text))
            {
                MessageBox.Show("Please provide a valid RailName and correct any invalid inputs.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            bool saved = false;
            string railName = RailNameTextBox.Text;

            while (!saved)
            {
                try
                {
                    var newDesign = new RailingDesign // Assumes RailingDesign class exists
                    {
                        DesignName = railName,
                        PostSize = PostSizeComboBox.SelectedItem?.ToString() ?? "",
                        MountingType = MountingTypeComboBox.SelectedItem?.ToString() ?? "",
                        TopCap = TopCapComboBox.SelectedItem?.ToString() ?? "",
                        TopCapHeight = double.Parse(TopCapHeightTextBox.Text),
                        TopCapWidth = double.Parse(TopCapWidthTextBox.Text),
                        TopCapWall = double.Parse(TopCapWallTextBox.Text),
                        BottomRail = BottomRailComboBox.SelectedItem?.ToString() ?? "",
                        PicketType = PicketTypeComboBox.SelectedItem?.ToString() ?? "",
                        PicketSize = PicketSizeComboBox.SelectedItem?.ToString() ?? "",
                        PicketPlacement = PicketPlacementComboBox.SelectedItem?.ToString() ?? "",
                        RailHeight = double.Parse(RailHeightTextBox.Text),
                        IntermediateRail = IntermediateRailComboBox.SelectedItem?.ToString() ?? "",
                        ImageLink = PreviewPictureBox.ImageLocation ?? "N/A", // Save current image link
                        DwgLink = "N/A", // Needs a way to associate/save DWG link if custom
                        SpecialInstructions = SpecialInstructionsComboBox.SelectedItem?.ToString() ?? "",
                        DecorativeWidth = PicketTypeComboBox.SelectedItem?.ToString() == "DECO" && double.TryParse(DecorativeWidthTextBox.Text, out double width) ? (double?)width : null
                    };

                    Data.SaveCustomDesign(newDesign); // Assumes Data class and its methods exist
                    MessageBox.Show($"Custom rail '{newDesign.DesignName}' saved successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);

                    LoadDesigns(); // Reload designs to include the new one
                    DesignComboBox.SelectedItem = newDesign.DesignName; // Select the newly saved design
                    saved = true;
                }
                catch (Exception ex)
                {
                    if (ex.Message.Contains("A design with the name")) // More robust check might be needed
                    {
                        // Using a temporary form for input
                        using (var prompt = new System.Windows.Forms.Form // Ensure prompt is disposed
                        {
                            Width = 350, // Adjusted width
                            Height = 180, // Adjusted height
                            Text = "Duplicate Design Name",
                            FormBorderStyle = FormBorderStyle.FixedDialog,
                            StartPosition = FormStartPosition.CenterParent,
                            MaximizeBox = false,
                            MinimizeBox = false
                        })
                        {
                            var label = new System.Windows.Forms.Label { Left = 10, Top = 20, Width = 300, Text = $"A design named '{railName}' already exists.\nEnter a new name or press Cancel:" };
                            var textBox = new System.Windows.Forms.TextBox { Left = 10, Top = 60, Width = 310, Text = railName };
                            var okButton = new Button { Text = "OK", Left = 150, Width = 80, Top = 90, DialogResult = System.Windows.Forms.DialogResult.OK };
                            var cancelButton = new Button { Text = "Cancel", Left = 240, Width = 80, Top = 90, DialogResult = System.Windows.Forms.DialogResult.Cancel };

                            okButton.Click += (s, args) => { if (string.IsNullOrWhiteSpace(textBox.Text)) { MessageBox.Show("Name cannot be empty.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning); textBox.Focus(); prompt.DialogResult = DialogResult.None; } else { prompt.Close(); } };
                            cancelButton.Click += (s, args) => prompt.Close();
                            prompt.Controls.Add(label);
                            prompt.Controls.Add(textBox);
                            prompt.Controls.Add(okButton);
                            prompt.Controls.Add(cancelButton);
                            prompt.AcceptButton = okButton;
                            prompt.CancelButton = cancelButton;

                            var result = prompt.ShowDialog(this);
                            if (result == System.Windows.Forms.DialogResult.OK)
                            {
                                railName = textBox.Text;
                                RailNameTextBox.Text = railName; // Update the main form's textbox as well
                            }
                            else
                            {
                                MessageBox.Show("Save operation canceled.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                return; // Exit the SaveCustomRailButton_Click method
                            }
                        }
                    }
                    else
                    {
                        MessageBox.Show($"Error saving custom rail: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return; // Exit the SaveCustomRailButton_Click method
                    }
                }
            }
        }

        private void OpenDwgButton_Click(object sender, EventArgs e)
        {
            RailingDesign selectedDesign = null;
            if (railingData != null && railingData.Designs != null && DesignComboBox.SelectedItem != null) // Added checks
            {
                selectedDesign = railingData.Designs.Find(d => d.DesignName == DesignComboBox.SelectedItem.ToString());
            }

            if (selectedDesign == null || string.IsNullOrEmpty(selectedDesign.DwgLink) || selectedDesign.DwgLink == "N/A" || !File.Exists(selectedDesign.DwgLink))
            {
                OpenDwgButton.Enabled = false; // Ensure button state is correct
                MessageBox.Show("No DWG file available for this design or file not found.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                // It's generally better not to copy to Desktop\TEMP without user consent or clear purpose.
                // Consider opening directly or using a proper app temp folder.
                // For this example, retaining original logic:
                string tempFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "TEMP_RailCreatorDWGs"); // Unique temp folder
                if (!Directory.Exists(tempFolder))
                {
                    Directory.CreateDirectory(tempFolder);
                }

                string tempDwgPath = Path.Combine(tempFolder, Path.GetFileName(selectedDesign.DwgLink));
                File.Copy(selectedDesign.DwgLink, tempDwgPath, true);

                string escapedPath = tempDwgPath.Replace("\\", "/"); // AutoCAD lisp often prefers forward slashes

                // Check if AutoCAD is running and has an active document
                Document doc = Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument;
                if (doc == null)
                {
                    // Attempt to open in a new AutoCAD instance or a running one if possible
                    // This can be complex. Simplest is to ask user to have AutoCAD open.
                    // For robust solution, consider Process.Start with the DWG path.
                    // System.Diagnostics.Process.Start(tempDwgPath);
                    // MessageBox.Show($"DWG copied to: {tempDwgPath}\nPlease open it manually in AutoCAD.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);

                    // Using SendStringToExecute requires an active document.
                    // If no doc, try to open it via AutoCAD's command line if AutoCAD is associated with .dwg
                    // This is more reliable than SendStringToExecute to a non-existent document.
                    try
                    {
                        System.Diagnostics.Process.Start(tempDwgPath);
                        MessageBox.Show($"Attempting to open DWG: {tempDwgPath}", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception procEx)
                    {
                        MessageBox.Show($"Could not automatically open DWG. Please open manually: {tempDwgPath}\nError: {procEx.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    return;
                }

                // Using (command) to ensure it runs in command context
                string command = $"(command \"_.OPEN\" \"{escapedPath}\") ";
                doc.SendStringToExecute(command, true, false, false); // Last parameter true can be problematic for OPEN

                MessageBox.Show($"Opened DWG: {tempDwgPath}", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening DWG: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void CancelButton_Click(object sender, EventArgs e) // Event handler for CancelButtonControl
        {
            this.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.Close();
        }

        private bool ValidateInputs()
        {
            // Rail Height
            if (!double.TryParse(RailHeightTextBox.Text, out double railHeight) || railHeight <= 0)
            {
                MessageBox.Show("Rail Height must be a positive number.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                RailHeightTextBox.Focus();
                return false;
            }

            // Top Cap Height
            if (!double.TryParse(TopCapHeightTextBox.Text, out double topCapHeight) || topCapHeight < 0)
            {
                MessageBox.Show("Top Cap Height must be a non-negative number.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                TopCapHeightTextBox.Focus();
                return false;
            }

            // Top Cap Width
            if (!double.TryParse(TopCapWidthTextBox.Text, out double topCapWidth) || topCapWidth < 0)
            {
                MessageBox.Show("Top Cap Width must be a non-negative number.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                TopCapWidthTextBox.Focus();
                return false;
            }

            // Top Cap Wall
            if (!double.TryParse(TopCapWallTextBox.Text, out double topCapWall) || topCapWall < 0)
            {
                MessageBox.Show("Top Cap Wall must be a non-negative number.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                TopCapWallTextBox.Focus();
                return false;
            }

            // Decorative Width (if PicketType is DECO)
            if (PicketTypeComboBox.SelectedItem?.ToString() == "DECO")
            {
                if (!double.TryParse(DecorativeWidthTextBox.Text, out double decorativeWidth) || decorativeWidth <= 0)
                {
                    MessageBox.Show("Decorative Width must be a positive number when Picket Type is DECO.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    DecorativeWidthTextBox.Focus();
                    return false;
                }
            }

            // Check for null or empty selections in ComboBoxes if they are mandatory
            if (PostSizeComboBox.SelectedItem == null)
            {
                MessageBox.Show("Please select a Post Size.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                PostSizeComboBox.Focus();
                return false;
            }
            // ... add similar checks for other mandatory ComboBoxes ...

            return true;
        }
    }
}