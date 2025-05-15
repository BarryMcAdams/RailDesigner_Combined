// RailDesigner1.UI.AttributeForm.cs
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;

namespace RailDesigner1.UI
{
    public partial class AttributeForm : Form
    {
        public Dictionary<string, string> Attributes { get; private set; }
        private ComponentType _componentType;

        private TextBox txtPartName;
        private TextBox txtDescription;
        private TextBox txtMaterial;
        private TextBox txtFinish;
        private TextBox txtWeightDensity;
        private TextBox txtStockLength;
        private TextBox txtSpecialNotes;
        private TextBox txtUserAttribute1;
        private TextBox txtUserAttribute2;
        private TextBox txtRailingHeight; // General Railing Height attribute for the component itself if needed
        private TextBox txtComponentHeight; // e.g., For Pickets ("HEIGHT")
        private Label lblComponentHeight;
        private TextBox txtComponentWidth;  // e.g., For Rails ("WIDTH")
        private Label lblComponentWidth;

        public AttributeForm(ComponentType componentType)
        {
            _componentType = componentType;
            Attributes = new Dictionary<string, string>();
            InitializeComponent();
            CustomizeFieldsForComponentType();
        }

        private void InitializeComponent()
        {
            this.Text = $"Define Attributes for {_componentType}";
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
            this.Width = 450;
            this.SuspendLayout();

            int currentTop = 10;
            int labelWidth = 120;
            int inputLeft = 130;
            int inputWidth = 280;
            int spacing = 28;

            // Standard Fields
            txtPartName = AddRow("Part Name*:", "", ref currentTop, labelWidth, inputLeft, inputWidth, spacing);
            txtDescription = AddRow("Description:", "", ref currentTop, labelWidth, inputLeft, inputWidth, spacing);
            txtMaterial = AddRow("Material:", "Aluminum", ref currentTop, labelWidth, inputLeft, inputWidth, spacing);
            txtFinish = AddRow("Finish:", "Mill Finish", ref currentTop, labelWidth, inputLeft, inputWidth, spacing);
            txtWeightDensity = AddRow("Weight Density:", "0.0975", ref currentTop, labelWidth, inputLeft, inputWidth, spacing); // e.g. lbs/in^3 or lbs/in
            txtStockLength = AddRow("Stock Length:", "", ref currentTop, labelWidth, inputLeft, inputWidth, spacing);
            txtRailingHeight = AddRow("Railing Height:", "36", ref currentTop, labelWidth, inputLeft, inputWidth, spacing); // For consistency with PRD attributes

            // Component-specific dynamic fields (initially hidden or managed by CustomizeFields)
            lblComponentHeight = new Label { Text = "Picket Height*:", Top = currentTop + 3, Left = 10, Width = labelWidth, Visible = false };
            txtComponentHeight = new TextBox { Top = currentTop, Left = inputLeft, Width = inputWidth, Visible = false };
            this.Controls.Add(lblComponentHeight);
            this.Controls.Add(txtComponentHeight);
            
            lblComponentWidth = new Label { Text = "Rail Width*:", Top = currentTop + spacing + 3, Left = 10, Width = labelWidth, Visible = false };
            txtComponentWidth = new TextBox { Top = currentTop + spacing, Left = inputLeft, Width = inputWidth, Visible = false };
            this.Controls.Add(lblComponentWidth);
            this.Controls.Add(txtComponentWidth);
            currentTop += spacing * 2; // Reserve space for potentially two dynamic fields

            txtSpecialNotes = AddRow("Special Notes:", "", ref currentTop, labelWidth, inputLeft, inputWidth, spacing);
            txtUserAttribute1 = AddRow("User Attr 1:", "", ref currentTop, labelWidth, inputLeft, inputWidth, spacing);
            txtUserAttribute2 = AddRow("User Attr 2:", "", ref currentTop, labelWidth, inputLeft, inputWidth, spacing);

            Button btnOk = new Button { Text = "OK", DialogResult = DialogResult.OK, Left = inputLeft, Top = currentTop + 10, Width = 80 };
            Button btnCancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Left = inputLeft + 90, Top = currentTop + 10, Width = 80 };
            btnOk.Click += BtnOk_Click;

            this.Controls.Add(btnOk);
            this.Controls.Add(btnCancel);
            this.AcceptButton = btnOk;
            this.CancelButton = btnCancel;

            this.Height = currentTop + 80;
            this.ResumeLayout(false);
        }

        private TextBox AddRow(string labelText, string defaultValue, ref int currentTop, int labelWidth, int inputLeft, int inputWidth, int spacing)
        {
            Label lbl = new Label { Text = labelText, Top = currentTop + 3, Left = 10, Width = labelWidth };
            TextBox txt = new TextBox { Top = currentTop, Left = inputLeft, Width = inputWidth, Text = defaultValue };
            this.Controls.Add(lbl);
            this.Controls.Add(txt);
            currentTop += spacing;
            return txt;
        }
        
        private void CustomizeFieldsForComponentType()
        {
            // Adjust visibility and labels based on component type
            if (_componentType == ComponentType.Picket)
            {
                lblComponentHeight.Text = "Picket Height*:";
                lblComponentHeight.Visible = true;
                txtComponentHeight.Visible = true;
            }
            else if (_componentType == ComponentType.TopCap || 
                     _componentType == ComponentType.BottomRail || 
                     _componentType == ComponentType.IntermediateRail)
            {
                lblComponentWidth.Text = "Rail Width*:";
                lblComponentWidth.Visible = true;
                txtComponentWidth.Visible = true;
            }
            // Add other customizations if needed
        }

        private void BtnOk_Click(object sender, EventArgs e)
        {
            if (!ValidateInput())
            {
                this.DialogResult = DialogResult.None; // Keep form open
                return;
            }

            Attributes["PARTNAME"] = txtPartName.Text.Trim();
            Attributes["DESCRIPTION"] = txtDescription.Text.Trim();
            Attributes["MATERIAL"] = txtMaterial.Text.Trim();
            Attributes["FINISH"] = txtFinish.Text.Trim();
            Attributes["WEIGHT_DENSITY"] = txtWeightDensity.Text.Trim();
            Attributes["STOCK_LENGTH"] = txtStockLength.Text.Trim();
            Attributes["SPECIAL_NOTES"] = txtSpecialNotes.Text.Trim();
            Attributes["USER_ATTRIBUTE_1"] = txtUserAttribute1.Text.Trim();
            Attributes["USER_ATTRIBUTE_2"] = txtUserAttribute2.Text.Trim();
            Attributes["RAILINGHEIGHT"] = txtRailingHeight.Text.Trim(); // PRD mentioned "RailingHeight" as a general attribute

            // Handle dynamic fields based on type
            if (_componentType == ComponentType.Picket && txtComponentHeight.Visible)
            {
                Attributes["HEIGHT"] = txtComponentHeight.Text.Trim(); // "HEIGHT" for Picket PRD
            }
            if ((_componentType == ComponentType.TopCap || _componentType == ComponentType.BottomRail || _componentType == ComponentType.IntermediateRail) && txtComponentWidth.Visible)
            {
                Attributes["WIDTH"] = txtComponentWidth.Text.Trim(); // "WIDTH" for Rail PRD
            }
            
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private bool ValidateInput()
        {
            if (string.IsNullOrWhiteSpace(txtPartName.Text))
            {
                MessageBox.Show("Part Name is required.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                txtPartName.Focus();
                return false;
            }

            if (!string.IsNullOrWhiteSpace(txtWeightDensity.Text) && !double.TryParse(txtWeightDensity.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out _))
            {
                MessageBox.Show("Weight Density must be a valid number.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                txtWeightDensity.Focus();
                return false;
            }
            // Validate RailingHeight as number
            if (!string.IsNullOrWhiteSpace(txtRailingHeight.Text) && !double.TryParse(txtRailingHeight.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out _))
            {
                MessageBox.Show("Railing Height must be a valid number.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                txtRailingHeight.Focus();
                return false;
            }
            
            // Validate PICKET_HEIGHT if visible and Picket
            if (_componentType == ComponentType.Picket && txtComponentHeight.Visible)
            {
                if (string.IsNullOrWhiteSpace(txtComponentHeight.Text) || !double.TryParse(txtComponentHeight.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out double h) || h <=0)
                {
                    MessageBox.Show("Picket Height must be a valid positive number.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    txtComponentHeight.Focus();
                    return false;
                }
            }

            // Validate RAIL_WIDTH if visible and a Rail type
            if ((_componentType == ComponentType.TopCap || _componentType == ComponentType.BottomRail || _componentType == ComponentType.IntermediateRail) && txtComponentWidth.Visible)
            {
                 if (string.IsNullOrWhiteSpace(txtComponentWidth.Text) || !double.TryParse(txtComponentWidth.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out double w) || w <=0)
                {
                    MessageBox.Show("Rail Width must be a valid positive number.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    txtComponentWidth.Focus();
                    return false;
                }
            }

            // Add more validation as needed (e.g., stock length positive)
             if (!string.IsNullOrWhiteSpace(txtStockLength.Text))
            {
                if(!double.TryParse(txtStockLength.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out double sl) || sl <=0)
                {
                     MessageBox.Show("Stock Length must be a positive number if specified.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    txtStockLength.Focus();
                    return false;
                }
            }

            return true;
        }
    }
}