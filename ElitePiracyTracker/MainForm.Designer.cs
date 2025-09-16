namespace ElitePiracyTracker
{
    //partial class MainForm
    //{
    //    private System.ComponentModel.IContainer components = null;
    //    private System.Windows.Forms.Button clearResultsButton;
    //    private System.Windows.Forms.Panel panel1;

    //    protected override void Dispose(bool disposing)
    //    {
    //        if (disposing && (components != null))
    //        {
    //            components.Dispose();
    //        }
    //        base.Dispose(disposing);
    //    }

    //    #region Windows Form Designer generated code

    //    private void InitializeComponent()
    //    {
    //        this.resultsDataGridView = new System.Windows.Forms.DataGridView();
    //        this.statusLabel = new System.Windows.Forms.Label();
    //        this.progressBar = new System.Windows.Forms.ProgressBar();
    //        this.detailsTextBox = new System.Windows.Forms.TextBox();
    //        this.exportButton = new System.Windows.Forms.Button();
    //        this.searchButton = new System.Windows.Forms.Button();
    //        this.referenceSystemTextBox = new System.Windows.Forms.TextBox();
    //        this.maxDistanceTextBox = new System.Windows.Forms.TextBox();
    //        this.label2 = new System.Windows.Forms.Label();
    //        this.label3 = new System.Windows.Forms.Label();
    //        this.groupBox1 = new System.Windows.Forms.GroupBox();
    //        this.clearResultsButton = new System.Windows.Forms.Button();
    //        this.panel1 = new System.Windows.Forms.Panel();
    //        this.label1 = new System.Windows.Forms.Label();
    //        ((System.ComponentModel.ISupportInitialize)(this.resultsDataGridView)).BeginInit();
    //        this.groupBox1.SuspendLayout();
    //        this.panel1.SuspendLayout();
    //        this.SuspendLayout();
    //        // 
    //        // resultsDataGridView
    //        // 
    //        this.resultsDataGridView.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
    //        | System.Windows.Forms.AnchorStyles.Left)
    //        | System.Windows.Forms.AnchorStyles.Right)));
    //        this.resultsDataGridView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
    //        this.resultsDataGridView.Location = new System.Drawing.Point(12, 70);
    //        this.resultsDataGridView.Name = "resultsDataGridView";
    //        this.resultsDataGridView.RowHeadersVisible = false;
    //        this.resultsDataGridView.RowTemplate.Height = 25;
    //        this.resultsDataGridView.Size = new System.Drawing.Size(840, 200);
    //        this.resultsDataGridView.TabIndex = 2;
    //        this.resultsDataGridView.SelectionChanged += new System.EventHandler(this.resultsDataGridView_SelectionChanged);
    //        // 
    //        // statusLabel
    //        // 
    //        this.statusLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
    //        | System.Windows.Forms.AnchorStyles.Right)));
    //        this.statusLabel.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
    //        this.statusLabel.Location = new System.Drawing.Point(12, 415);
    //        this.statusLabel.Name = "statusLabel";
    //        this.statusLabel.Size = new System.Drawing.Size(840, 23);
    //        this.statusLabel.TabIndex = 3;
    //        this.statusLabel.Text = "Ready";
    //        this.statusLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
    //        // 
    //        // progressBar
    //        // 
    //        this.progressBar.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
    //        | System.Windows.Forms.AnchorStyles.Right)));
    //        this.progressBar.Location = new System.Drawing.Point(12, 385);
    //        this.progressBar.Name = "progressBar";
    //        this.progressBar.Size = new System.Drawing.Size(840, 23);
    //        this.progressBar.TabIndex = 4;
    //        this.progressBar.Visible = false;
    //        // 
    //        // detailsTextBox
    //        // 
    //        this.detailsTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
    //        | System.Windows.Forms.AnchorStyles.Right)));
    //        this.detailsTextBox.Location = new System.Drawing.Point(12, 276);
    //        this.detailsTextBox.Multiline = true;
    //        this.detailsTextBox.Name = "detailsTextBox";
    //        this.detailsTextBox.ReadOnly = true;
    //        this.detailsTextBox.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
    //        this.detailsTextBox.Size = new System.Drawing.Size(840, 100);
    //        this.detailsTextBox.TabIndex = 6;
    //        // 
    //        // exportButton
    //        // 
    //        this.exportButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
    //        this.exportButton.Location = new System.Drawing.Point(752, 12);
    //        this.exportButton.Name = "exportButton";
    //        this.exportButton.Size = new System.Drawing.Size(100, 35);
    //        this.exportButton.TabIndex = 7;
    //        this.exportButton.Text = "Export Results";
    //        this.exportButton.UseVisualStyleBackColor = true;
    //        this.exportButton.Click += new System.EventHandler(this.exportButton_Click);
    //        // 
    //        // searchButton
    //        // 
    //        this.searchButton.Location = new System.Drawing.Point(6, 19);
    //        this.searchButton.Name = "searchButton";
    //        this.searchButton.Size = new System.Drawing.Size(100, 35);
    //        this.searchButton.TabIndex = 9;
    //        this.searchButton.Text = "Search Nearby";
    //        this.searchButton.UseVisualStyleBackColor = true;
    //        this.searchButton.Click += new System.EventHandler(this.searchButton_Click);
    //        // 
    //        // referenceSystemTextBox
    //        // 
    //        this.referenceSystemTextBox.Location = new System.Drawing.Point(112, 26);
    //        this.referenceSystemTextBox.Name = "referenceSystemTextBox";
    //        this.referenceSystemTextBox.Size = new System.Drawing.Size(100, 20);
    //        this.referenceSystemTextBox.TabIndex = 10;
    //        // 
    //        // maxDistanceTextBox
    //        // 
    //        this.maxDistanceTextBox.Location = new System.Drawing.Point(218, 26);
    //        this.maxDistanceTextBox.Name = "maxDistanceTextBox";
    //        this.maxDistanceTextBox.Size = new System.Drawing.Size(50, 20);
    //        this.maxDistanceTextBox.TabIndex = 11;
    //        // 
    //        // label2
    //        // 
    //        this.label2.AutoSize = true;
    //        this.label2.Location = new System.Drawing.Point(109, 10);
    //        this.label2.Name = "label2";
    //        this.label2.Size = new System.Drawing.Size(95, 13);
    //        this.label2.TabIndex = 12;
    //        this.label2.Text = "Reference System";
    //        // 
    //        // label3
    //        // 
    //        this.label3.AutoSize = true;
    //        this.label3.Location = new System.Drawing.Point(215, 10);
    //        this.label3.Name = "label3";
    //        this.label3.Size = new System.Drawing.Size(49, 13);
    //        this.label3.TabIndex = 13;
    //        this.label3.Text = "Max LY";
    //        // 
    //        // groupBox1
    //        // 
    //        this.groupBox1.Controls.Add(this.searchButton);
    //        this.groupBox1.Controls.Add(this.label3);
    //        this.groupBox1.Controls.Add(this.referenceSystemTextBox);
    //        this.groupBox1.Controls.Add(this.label2);
    //        this.groupBox1.Controls.Add(this.maxDistanceTextBox);
    //        this.groupBox1.Location = new System.Drawing.Point(12, 12);
    //        this.groupBox1.Name = "groupBox1";
    //        this.groupBox1.Size = new System.Drawing.Size(280, 55);
    //        this.groupBox1.TabIndex = 14;
    //        this.groupBox1.TabStop = false;
    //        this.groupBox1.Text = "System Search";
    //        // 
    //        // clearResultsButton
    //        // 
    //        this.clearResultsButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
    //        this.clearResultsButton.Location = new System.Drawing.Point(646, 12);
    //        this.clearResultsButton.Name = "clearResultsButton";
    //        this.clearResultsButton.Size = new System.Drawing.Size(100, 35);
    //        this.clearResultsButton.TabIndex = 15;
    //        this.clearResultsButton.Text = "Clear Results";
    //        this.clearResultsButton.UseVisualStyleBackColor = true;
    //        this.clearResultsButton.Click += new System.EventHandler(this.clearResultsButton_Click);
    //        // 
    //        // panel1
    //        // 
    //        this.panel1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
    //        | System.Windows.Forms.AnchorStyles.Right)));
    //        this.panel1.Controls.Add(this.groupBox1);
    //        this.panel1.Controls.Add(this.clearResultsButton);
    //        this.panel1.Controls.Add(this.exportButton);
    //        this.panel1.Location = new System.Drawing.Point(0, 0);
    //        this.panel1.Name = "panel1";
    //        this.panel1.Size = new System.Drawing.Size(864, 70);
    //        this.panel1.TabIndex = 16;
    //        // 
    //        // label1
    //        // 
    //        this.label1.AutoSize = true;
    //        this.label1.Location = new System.Drawing.Point(12, 54);
    //        this.label1.Name = "label1";
    //        this.label1.Size = new System.Drawing.Size(0, 13);
    //        this.label1.TabIndex = 17;
    //        // 
    //        // MainForm
    //        // 
    //        this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
    //        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
    //        this.ClientSize = new System.Drawing.Size(864, 447);
    //        this.Controls.Add(this.label1);
    //        this.Controls.Add(this.panel1);
    //        this.Controls.Add(this.detailsTextBox);
    //        this.Controls.Add(this.progressBar);
    //        this.Controls.Add(this.statusLabel);
    //        this.Controls.Add(this.resultsDataGridView);
    //        this.MinimumSize = new System.Drawing.Size(880, 400);
    //        this.Name = "MainForm";
    //        this.Text = "Elite Dangerous Piracy Analyzer";
    //        this.Load += new System.EventHandler(this.MainForm_Load);
    //        ((System.ComponentModel.ISupportInitialize)(this.resultsDataGridView)).EndInit();
    //        this.groupBox1.ResumeLayout(false);
    //        this.groupBox1.PerformLayout();
    //        this.panel1.ResumeLayout(false);
    //        this.ResumeLayout(false);
    //        this.PerformLayout();

    //    }

    //    #endregion

    //    private System.Windows.Forms.TextBox systemTextBox;
    //    private System.Windows.Forms.Button analyzeButton;
    //    private System.Windows.Forms.DataGridView resultsDataGridView;
    //    private System.Windows.Forms.Label statusLabel;
    //    private System.Windows.Forms.ProgressBar progressBar;
    //    private System.Windows.Forms.Button clearButton;
    //    private System.Windows.Forms.TextBox detailsTextBox;
    //    private System.Windows.Forms.Button exportButton;
    //    private System.Windows.Forms.Label label1;
    //    private System.Windows.Forms.Button searchButton;
    //    private System.Windows.Forms.TextBox referenceSystemTextBox;
    //    private System.Windows.Forms.TextBox maxDistanceTextBox;
    //    private System.Windows.Forms.Label label2;
    //    private System.Windows.Forms.Label label3;
    //    private System.Windows.Forms.GroupBox groupBox1;
    //}
}