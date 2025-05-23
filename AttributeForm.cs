using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Drawing;

public class AttributeForm : Form
{
    private Label lblPartName;
    private TextBox txtPartName;
    private Label lblDescription;
    private TextBox txtDescription;
    private Label lblMaterial;
    private TextBox txtMaterial;
    private Label lblFinish;
    private TextBox txtFinish;
    private Label lblWeightDensity;
    private TextBox txtWeightDensity;
    private Label lblStockLength;
    private TextBox txtStockLength;
    private Label lblSpecialNotes;
    private TextBox txtSpecialNotes;
    private Label lblUserAttribute1;
    private TextBox txtUserAttribute1;
    private Label lblUserAttribute2;
    private TextBox txtUserAttribute2;
    private Label lblRailingHeight;
    private TextBox txtRailingHeight;

    private Button btnOk;
    private Button btnCancel;

    private string componentType;

    public Dictionary<string, string> Attributes { get; private set; }

    public AttributeForm(string componentType)
    {
        this.componentType = componentType;
        Attributes = new Dictionary<string, string>();

        // Initialize Controls
        lblPartName = new Label();
        txtPartName = new TextBox();
        lblDescription = new Label();
        txtDescription = new TextBox();
        lblMaterial = new Label();
        txtMaterial = new TextBox();
        lblFinish = new Label();
        txtFinish = new TextBox();
        lblWeightDensity = new Label();
        txtWeightDensity = new TextBox();
        lblStockLength = new Label();
        txtStockLength = new TextBox();
        lblSpecialNotes = new Label();
        txtSpecialNotes = new TextBox();
        lblUserAttribute1 = new Label();
        txtUserAttribute1 = new TextBox();
        lblUserAttribute2 = new Label();
        txtUserAttribute2 = new TextBox();
        lblRailingHeight = new Label();
        txtRailingHeight = new TextBox();
        btnOk = new Button();
        btnCancel = new Button();

        SetupControls();
    }

    private void SetupControls()
    {
        // Labels
        lblPartName.Text = "Part Name:";
        lblDescription.Text = "Description:";
        lblMaterial.Text = "Material:";
        lblFinish.Text = "Finish:";
        lblWeightDensity.Text = "Weight/Density:";
        lblStockLength.Text = "Stock Length:";
        lblSpecialNotes.Text = "Special Notes:";
        lblUserAttribute1.Text = "User Attribute 1:";
        lblUserAttribute2.Text = "User Attribute 2:";
        lblRailingHeight.Text = "Railing Height:";

        // Buttons
        btnOk.Text = "OK";
        btnCancel.Text = "Cancel";

        // Event Handlers
        btnOk.Click += btnOk_Click;
        btnCancel.Click += btnCancel_Click;

        // Basic conceptual layout (vertical stacking)
        int yPos = 10;
        int xPos = 10;
        int labelWidth = 100;
        int textBoxWidth = 200;
        int controlHeight = 20; // Approximate height for TextBox/Label
        int spacing = 5;       // Vertical spacing between control pairs

        // Part Name
        lblPartName.Location = new Point(xPos, yPos);
        lblPartName.Size = new Size(labelWidth, controlHeight);
        txtPartName.Location = new Point(xPos + labelWidth + spacing, yPos);
        txtPartName.Size = new Size(textBoxWidth, controlHeight);
        yPos += controlHeight + spacing;

        // Description
        lblDescription.Location = new Point(xPos, yPos);
        lblDescription.Size = new Size(labelWidth, controlHeight);
        txtDescription.Location = new Point(xPos + labelWidth + spacing, yPos);
        txtDescription.Size = new Size(textBoxWidth, controlHeight);
        yPos += controlHeight + spacing;

        // Material
        lblMaterial.Location = new Point(xPos, yPos);
        lblMaterial.Size = new Size(labelWidth, controlHeight);
        txtMaterial.Location = new Point(xPos + labelWidth + spacing, yPos);
        txtMaterial.Size = new Size(textBoxWidth, controlHeight);
        yPos += controlHeight + spacing;

        // Finish
        lblFinish.Location = new Point(xPos, yPos);
        lblFinish.Size = new Size(labelWidth, controlHeight);
        txtFinish.Location = new Point(xPos + labelWidth + spacing, yPos);
        txtFinish.Size = new Size(textBoxWidth, controlHeight);
        yPos += controlHeight + spacing;

        // Weight/Density
        lblWeightDensity.Location = new Point(xPos, yPos);
        lblWeightDensity.Size = new Size(labelWidth, controlHeight);
        txtWeightDensity.Location = new Point(xPos + labelWidth + spacing, yPos);
        txtWeightDensity.Size = new Size(textBoxWidth, controlHeight);
        yPos += controlHeight + spacing;

        // Stock Length
        lblStockLength.Location = new Point(xPos, yPos);
        lblStockLength.Size = new Size(labelWidth, controlHeight);
        txtStockLength.Location = new Point(xPos + labelWidth + spacing, yPos);
        txtStockLength.Size = new Size(textBoxWidth, controlHeight);
        yPos += controlHeight + spacing;

        // Special Notes
        lblSpecialNotes.Location = new Point(xPos, yPos);
        lblSpecialNotes.Size = new Size(labelWidth, controlHeight);
        txtSpecialNotes.Location = new Point(xPos + labelWidth + spacing, yPos);
        txtSpecialNotes.Size = new Size(textBoxWidth, controlHeight * 2); // Taller for notes
        txtSpecialNotes.Multiline = true;
        yPos += (controlHeight * 2) + spacing;

        // User Attribute 1
        lblUserAttribute1.Location = new Point(xPos, yPos);
        lblUserAttribute1.Size = new Size(labelWidth, controlHeight);
        txtUserAttribute1.Location = new Point(xPos + labelWidth + spacing, yPos);
        txtUserAttribute1.Size = new Size(textBoxWidth, controlHeight);
        yPos += controlHeight + spacing;

        // User Attribute 2
        lblUserAttribute2.Location = new Point(xPos, yPos);
        lblUserAttribute2.Size = new Size(labelWidth, controlHeight);
        txtUserAttribute2.Location = new Point(xPos + labelWidth + spacing, yPos);
        txtUserAttribute2.Size = new Size(textBoxWidth, controlHeight);
        yPos += controlHeight + spacing;
        
        // Railing Height
        lblRailingHeight.Location = new Point(xPos, yPos);
        lblRailingHeight.Size = new Size(labelWidth, controlHeight);
        txtRailingHeight.Location = new Point(xPos + labelWidth + spacing, yPos);
        txtRailingHeight.Size = new Size(textBoxWidth, controlHeight);
        yPos += controlHeight + spacing;

        // Buttons
        btnOk.Location = new Point(xPos + labelWidth + spacing, yPos);
        btnOk.Size = new Size(75, 23); // Standard button size
        btnCancel.Location = new Point(xPos + labelWidth + spacing + 80, yPos); // 80 = btnOk.Width + spacing
        btnCancel.Size = new Size(75, 23);
        yPos += controlHeight + spacing + 20; // Extra spacing after buttons

        // Add controls to the form
        this.Controls.Add(lblPartName);
        this.Controls.Add(txtPartName);
        this.Controls.Add(lblDescription);
        this.Controls.Add(txtDescription);
        this.Controls.Add(lblMaterial);
        this.Controls.Add(txtMaterial);
        this.Controls.Add(lblFinish);
        this.Controls.Add(txtFinish);
        this.Controls.Add(lblWeightDensity);
        this.Controls.Add(txtWeightDensity);
        this.Controls.Add(lblStockLength);
        this.Controls.Add(txtStockLength);
        this.Controls.Add(lblSpecialNotes);
        this.Controls.Add(txtSpecialNotes);
        this.Controls.Add(lblUserAttribute1);
        this.Controls.Add(txtUserAttribute1);
        this.Controls.Add(lblUserAttribute2);
        this.Controls.Add(txtUserAttribute2);
        this.Controls.Add(lblRailingHeight);
        this.Controls.Add(txtRailingHeight);
        this.Controls.Add(btnOk);
        this.Controls.Add(btnCancel);
        
        this.Size = new Size(xPos + labelWidth + spacing + textBoxWidth + spacing + 20, yPos); // Adjust form size to fit content
        this.Text = this.componentType + " Attributes"; // Set form title
    }

    private void btnOk_Click(object sender, EventArgs e)
    {
        // Validation
        if (string.IsNullOrWhiteSpace(txtPartName.Text))
        {
            MessageBox.Show("PARTNAME cannot be empty.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            txtPartName.Focus();
            return;
        }

        if (string.IsNullOrWhiteSpace(txtRailingHeight.Text))
        {
            MessageBox.Show("RailingHeight cannot be empty.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            txtRailingHeight.Focus();
            return;
        }

        double tempDouble;
        if (!string.IsNullOrWhiteSpace(txtWeightDensity.Text) && !double.TryParse(txtWeightDensity.Text, out tempDouble))
        {
            MessageBox.Show("WEIGHT_DENSITY must be a valid number.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            txtWeightDensity.Focus();
            return;
        }

        if (!string.IsNullOrWhiteSpace(txtStockLength.Text) && !double.TryParse(txtStockLength.Text, out tempDouble))
        {
            MessageBox.Show("STOCK_LENGTH must be a valid number.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            txtStockLength.Focus();
            return;
        }
        
        // RailingHeight is already checked for empty, now check for valid double
        if (!double.TryParse(txtRailingHeight.Text, out tempDouble)) 
        {
            MessageBox.Show("RailingHeight must be a valid number.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            txtRailingHeight.Focus();
            return;
        }

        // Populate Attributes
        Attributes["COMPONENTTYPE"] = this.componentType;
        Attributes["PARTNAME"] = txtPartName.Text;
        Attributes["DESCRIPTION"] = txtDescription.Text;
        Attributes["MATERIAL"] = txtMaterial.Text;
        Attributes["FINISH"] = txtFinish.Text;
        Attributes["WEIGHT_DENSITY"] = txtWeightDensity.Text;
        Attributes["STOCK_LENGTH"] = txtStockLength.Text;
        Attributes["SPECIAL_NOTES"] = txtSpecialNotes.Text;
        Attributes["USER_ATTRIBUTE_1"] = txtUserAttribute1.Text;
        Attributes["USER_ATTRIBUTE_2"] = txtUserAttribute2.Text;
        Attributes["RailingHeight"] = txtRailingHeight.Text;

        this.DialogResult = DialogResult.OK;
        this.Close();
    }

    private void btnCancel_Click(object sender, EventArgs e)
    {
        this.DialogResult = DialogResult.Cancel;
        this.Close();
    }
}
