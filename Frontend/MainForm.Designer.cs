namespace SonicRetro.KensSharp.Frontend
{
    partial class MainForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

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

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.destinationLabel = new System.Windows.Forms.Label();
            this.sourceLabel = new System.Windows.Forms.Label();
            this.goButton = new System.Windows.Forms.Button();
            this.compressRadioButton = new System.Windows.Forms.RadioButton();
            this.modeGroupBox = new System.Windows.Forms.GroupBox();
            this.decompressRadioButton = new System.Windows.Forms.RadioButton();
            this.formatGroupBox = new System.Windows.Forms.GroupBox();
            this.formatListBox = new System.Windows.Forms.ListBox();
            this.parametersGroupBox = new System.Windows.Forms.GroupBox();
            this.endiannessComboBox = new System.Windows.Forms.ComboBox();
            this.endiannessLabel = new System.Windows.Forms.Label();
            this.sizeParameterHexCheckBox = new System.Windows.Forms.CheckBox();
            this.sizeParameterNumericUpDown = new System.Windows.Forms.NumericUpDown();
            this.sizeParameterLabel = new System.Windows.Forms.Label();
            this.sourceFileSelector = new SonicRetro.KensSharp.Frontend.FileSelector();
            this.destinationFileSelector = new SonicRetro.KensSharp.Frontend.FileSelector();
            this.modeGroupBox.SuspendLayout();
            this.formatGroupBox.SuspendLayout();
            this.parametersGroupBox.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.sizeParameterNumericUpDown)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.sourceFileSelector)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.destinationFileSelector)).BeginInit();
            this.SuspendLayout();
            // 
            // destinationLabel
            // 
            this.destinationLabel.AutoSize = true;
            this.destinationLabel.Location = new System.Drawing.Point(150, 126);
            this.destinationLabel.Name = "destinationLabel";
            this.destinationLabel.Size = new System.Drawing.Size(65, 13);
            this.destinationLabel.TabIndex = 5;
            this.destinationLabel.Text = "&Destination:";
            // 
            // sourceLabel
            // 
            this.sourceLabel.AutoSize = true;
            this.sourceLabel.Location = new System.Drawing.Point(150, 97);
            this.sourceLabel.Name = "sourceLabel";
            this.sourceLabel.Size = new System.Drawing.Size(44, 13);
            this.sourceLabel.TabIndex = 3;
            this.sourceLabel.Text = "&Source:";
            // 
            // goButton
            // 
            this.goButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.goButton.Enabled = false;
            this.goButton.Location = new System.Drawing.Point(498, 92);
            this.goButton.Name = "goButton";
            this.goButton.Size = new System.Drawing.Size(74, 54);
            this.goButton.TabIndex = 7;
            this.goButton.Text = "Go";
            this.goButton.UseVisualStyleBackColor = true;
            this.goButton.Click += new System.EventHandler(this.goButton_Click);
            // 
            // compressRadioButton
            // 
            this.compressRadioButton.AutoSize = true;
            this.compressRadioButton.Checked = true;
            this.compressRadioButton.Location = new System.Drawing.Point(6, 20);
            this.compressRadioButton.Name = "compressRadioButton";
            this.compressRadioButton.Size = new System.Drawing.Size(72, 17);
            this.compressRadioButton.TabIndex = 0;
            this.compressRadioButton.TabStop = true;
            this.compressRadioButton.Text = "&Compress";
            this.compressRadioButton.UseVisualStyleBackColor = true;
            this.compressRadioButton.CheckedChanged += new System.EventHandler(this.modeRadioButton_CheckedChanged);
            // 
            // modeGroupBox
            // 
            this.modeGroupBox.Controls.Add(this.compressRadioButton);
            this.modeGroupBox.Controls.Add(this.decompressRadioButton);
            this.modeGroupBox.Location = new System.Drawing.Point(150, 12);
            this.modeGroupBox.Name = "modeGroupBox";
            this.modeGroupBox.Size = new System.Drawing.Size(173, 43);
            this.modeGroupBox.TabIndex = 1;
            this.modeGroupBox.TabStop = false;
            this.modeGroupBox.Text = "Mode";
            // 
            // decompressRadioButton
            // 
            this.decompressRadioButton.AutoSize = true;
            this.decompressRadioButton.Location = new System.Drawing.Point(84, 20);
            this.decompressRadioButton.Name = "decompressRadioButton";
            this.decompressRadioButton.Size = new System.Drawing.Size(83, 17);
            this.decompressRadioButton.TabIndex = 1;
            this.decompressRadioButton.TabStop = true;
            this.decompressRadioButton.Text = "&Decompress";
            this.decompressRadioButton.CheckedChanged += new System.EventHandler(this.modeRadioButton_CheckedChanged);
            // 
            // formatGroupBox
            // 
            this.formatGroupBox.Controls.Add(this.formatListBox);
            this.formatGroupBox.Location = new System.Drawing.Point(12, 12);
            this.formatGroupBox.Name = "formatGroupBox";
            this.formatGroupBox.Size = new System.Drawing.Size(132, 120);
            this.formatGroupBox.TabIndex = 0;
            this.formatGroupBox.TabStop = false;
            this.formatGroupBox.Text = "Format";
            // 
            // formatListBox
            // 
            this.formatListBox.Items.AddRange(new object[] {
            "Kosinski",
            "Moduled Kosinski",
            "Enigma",
            "Nemesis",
            "Saxman (with size)",
            "Saxman (without size)",
            "Comper"});
            this.formatListBox.Location = new System.Drawing.Point(6, 20);
            this.formatListBox.Name = "formatListBox";
            this.formatListBox.Size = new System.Drawing.Size(120, 95);
            this.formatListBox.TabIndex = 0;
            this.formatListBox.SelectedIndexChanged += new System.EventHandler(this.formatListBox_SelectedIndexChanged);
            // 
            // parametersGroupBox
            // 
            this.parametersGroupBox.Controls.Add(this.endiannessComboBox);
            this.parametersGroupBox.Controls.Add(this.endiannessLabel);
            this.parametersGroupBox.Controls.Add(this.sizeParameterHexCheckBox);
            this.parametersGroupBox.Controls.Add(this.sizeParameterNumericUpDown);
            this.parametersGroupBox.Controls.Add(this.sizeParameterLabel);
            this.parametersGroupBox.Location = new System.Drawing.Point(329, 12);
            this.parametersGroupBox.Name = "parametersGroupBox";
            this.parametersGroupBox.Size = new System.Drawing.Size(243, 74);
            this.parametersGroupBox.TabIndex = 2;
            this.parametersGroupBox.TabStop = false;
            this.parametersGroupBox.Text = "Parameters";
            // 
            // endiannessComboBox
            // 
            this.endiannessComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.endiannessComboBox.FormattingEnabled = true;
            this.endiannessComboBox.Items.AddRange(new object[] {
            "Big endian",
            "Little endian"});
            this.endiannessComboBox.Location = new System.Drawing.Point(77, 47);
            this.endiannessComboBox.Name = "endiannessComboBox";
            this.endiannessComboBox.Size = new System.Drawing.Size(160, 21);
            this.endiannessComboBox.TabIndex = 4;
            // 
            // endiannessLabel
            // 
            this.endiannessLabel.AutoSize = true;
            this.endiannessLabel.Location = new System.Drawing.Point(6, 50);
            this.endiannessLabel.Name = "endiannessLabel";
            this.endiannessLabel.Size = new System.Drawing.Size(65, 13);
            this.endiannessLabel.TabIndex = 3;
            this.endiannessLabel.Text = "&Endianness:";
            // 
            // sizeParameterHexCheckBox
            // 
            this.sizeParameterHexCheckBox.AutoSize = true;
            this.sizeParameterHexCheckBox.Enabled = false;
            this.sizeParameterHexCheckBox.Location = new System.Drawing.Point(192, 21);
            this.sizeParameterHexCheckBox.Name = "sizeParameterHexCheckBox";
            this.sizeParameterHexCheckBox.Size = new System.Drawing.Size(45, 17);
            this.sizeParameterHexCheckBox.TabIndex = 2;
            this.sizeParameterHexCheckBox.Text = "Hex";
            this.sizeParameterHexCheckBox.UseVisualStyleBackColor = true;
            this.sizeParameterHexCheckBox.CheckedChanged += new System.EventHandler(this.sizeParameterHexCheckBox_CheckedChanged);
            // 
            // sizeParameterNumericUpDown
            // 
            this.sizeParameterNumericUpDown.Enabled = false;
            this.sizeParameterNumericUpDown.Location = new System.Drawing.Point(42, 20);
            this.sizeParameterNumericUpDown.Maximum = new decimal(new int[] {
            65535,
            0,
            0,
            0});
            this.sizeParameterNumericUpDown.Name = "sizeParameterNumericUpDown";
            this.sizeParameterNumericUpDown.Size = new System.Drawing.Size(144, 21);
            this.sizeParameterNumericUpDown.TabIndex = 1;
            // 
            // sizeParameterLabel
            // 
            this.sizeParameterLabel.AutoSize = true;
            this.sizeParameterLabel.Enabled = false;
            this.sizeParameterLabel.Location = new System.Drawing.Point(6, 22);
            this.sizeParameterLabel.Name = "sizeParameterLabel";
            this.sizeParameterLabel.Size = new System.Drawing.Size(30, 13);
            this.sizeParameterLabel.TabIndex = 0;
            this.sizeParameterLabel.Text = "S&ize:";
            // 
            // sourceFileSelector
            // 
            this.sourceFileSelector.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.sourceFileSelector.DefaultExt = "bin";
            this.sourceFileSelector.FileName = "";
            this.sourceFileSelector.Filter = "All Files (*.*)|*.*";
            this.sourceFileSelector.Location = new System.Drawing.Point(221, 92);
            this.sourceFileSelector.Name = "sourceFileSelector";
            this.sourceFileSelector.Size = new System.Drawing.Size(271, 24);
            this.sourceFileSelector.TabIndex = 4;
            this.sourceFileSelector.FileNameChanged += new System.EventHandler(this.fileSelector_FileNameChanged);
            // 
            // destinationFileSelector
            // 
            this.destinationFileSelector.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.destinationFileSelector.DefaultExt = "bin";
            this.destinationFileSelector.FileName = "";
            this.destinationFileSelector.Filter = "All Files (*.*)|*.*";
            this.destinationFileSelector.Location = new System.Drawing.Point(221, 122);
            this.destinationFileSelector.Mode = SonicRetro.KensSharp.Frontend.FileSelectorMode.Save;
            this.destinationFileSelector.Name = "destinationFileSelector";
            this.destinationFileSelector.Size = new System.Drawing.Size(271, 24);
            this.destinationFileSelector.TabIndex = 6;
            this.destinationFileSelector.FileNameChanged += new System.EventHandler(this.fileSelector_FileNameChanged);
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(584, 158);
            this.Controls.Add(this.parametersGroupBox);
            this.Controls.Add(this.formatGroupBox);
            this.Controls.Add(this.modeGroupBox);
            this.Controls.Add(this.sourceLabel);
            this.Controls.Add(this.sourceFileSelector);
            this.Controls.Add(this.destinationLabel);
            this.Controls.Add(this.destinationFileSelector);
            this.Controls.Add(this.goButton);
            this.Font = new System.Drawing.Font("Tahoma", 8.25F);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.Name = "MainForm";
            this.Text = "KENSSharp";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.MainForm_FormClosed);
            this.modeGroupBox.ResumeLayout(false);
            this.modeGroupBox.PerformLayout();
            this.formatGroupBox.ResumeLayout(false);
            this.parametersGroupBox.ResumeLayout(false);
            this.parametersGroupBox.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.sizeParameterNumericUpDown)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.sourceFileSelector)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.destinationFileSelector)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label destinationLabel;
        private System.Windows.Forms.Label sourceLabel;
        private System.Windows.Forms.Button goButton;
        private System.Windows.Forms.RadioButton compressRadioButton;
        private System.Windows.Forms.GroupBox modeGroupBox;
        private System.Windows.Forms.RadioButton decompressRadioButton;
        private System.Windows.Forms.GroupBox formatGroupBox;
        private FileSelector sourceFileSelector;
        private FileSelector destinationFileSelector;
        private System.Windows.Forms.ListBox formatListBox;
        private System.Windows.Forms.GroupBox parametersGroupBox;
        private System.Windows.Forms.Label sizeParameterLabel;
        private System.Windows.Forms.NumericUpDown sizeParameterNumericUpDown;
        private System.Windows.Forms.CheckBox sizeParameterHexCheckBox;
        private System.Windows.Forms.ComboBox endiannessComboBox;
        private System.Windows.Forms.Label endiannessLabel;

    }
}

