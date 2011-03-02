namespace SonicRetro.KensSharp.Comp
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
            this.components = new System.ComponentModel.Container();
            this.destinationLabel = new System.Windows.Forms.Label();
            this.sourceLabel = new System.Windows.Forms.Label();
            this.goButton = new System.Windows.Forms.Button();
            this.enigmaRadioButton = new System.Windows.Forms.RadioButton();
            this.compressRadioButton = new System.Windows.Forms.RadioButton();
            this.nemesisRadioButton = new System.Windows.Forms.RadioButton();
            this.moduledKosinskiRadioButton = new System.Windows.Forms.RadioButton();
            this.kosinskiRadioButton = new System.Windows.Forms.RadioButton();
            this.modeGroupBox = new System.Windows.Forms.GroupBox();
            this.decompressRadioButton = new System.Windows.Forms.RadioButton();
            this.formatGroupBox = new System.Windows.Forms.GroupBox();
            this.errorProvider = new System.Windows.Forms.ErrorProvider(this.components);
            this.sourceFileSelector = new SonicRetro.KensSharp.Comp.FileSelector();
            this.destinationFileSelector = new SonicRetro.KensSharp.Comp.FileSelector();
            this.modeGroupBox.SuspendLayout();
            this.formatGroupBox.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.errorProvider)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.sourceFileSelector)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.destinationFileSelector)).BeginInit();
            this.SuspendLayout();
            // 
            // destinationLabel
            // 
            this.destinationLabel.AutoSize = true;
            this.destinationLabel.Location = new System.Drawing.Point(12, 94);
            this.destinationLabel.Name = "destinationLabel";
            this.destinationLabel.Size = new System.Drawing.Size(65, 13);
            this.destinationLabel.TabIndex = 4;
            this.destinationLabel.Text = "D&estination:";
            // 
            // sourceLabel
            // 
            this.sourceLabel.AutoSize = true;
            this.sourceLabel.Location = new System.Drawing.Point(12, 65);
            this.sourceLabel.Name = "sourceLabel";
            this.sourceLabel.Size = new System.Drawing.Size(44, 13);
            this.sourceLabel.TabIndex = 2;
            this.sourceLabel.Text = "&Source:";
            // 
            // goButton
            // 
            this.goButton.Location = new System.Drawing.Point(444, 60);
            this.goButton.Name = "goButton";
            this.goButton.Size = new System.Drawing.Size(74, 54);
            this.goButton.TabIndex = 6;
            this.goButton.Text = "Go";
            this.goButton.UseVisualStyleBackColor = true;
            this.goButton.Click += new System.EventHandler(this.goButton_Click);
            // 
            // enigmaRadioButton
            // 
            this.enigmaRadioButton.AutoSize = true;
            this.enigmaRadioButton.Enabled = false;
            this.enigmaRadioButton.Location = new System.Drawing.Point(261, 19);
            this.enigmaRadioButton.Name = "enigmaRadioButton";
            this.enigmaRadioButton.Size = new System.Drawing.Size(60, 17);
            this.enigmaRadioButton.TabIndex = 3;
            this.enigmaRadioButton.TabStop = true;
            this.enigmaRadioButton.Text = "&Enigma";
            // 
            // compressRadioButton
            // 
            this.compressRadioButton.AutoSize = true;
            this.compressRadioButton.Checked = true;
            this.compressRadioButton.Location = new System.Drawing.Point(6, 19);
            this.compressRadioButton.Name = "compressRadioButton";
            this.compressRadioButton.Size = new System.Drawing.Size(71, 17);
            this.compressRadioButton.TabIndex = 0;
            this.compressRadioButton.TabStop = true;
            this.compressRadioButton.Text = "&Compress";
            this.compressRadioButton.UseVisualStyleBackColor = true;
            // 
            // nemesisRadioButton
            // 
            this.nemesisRadioButton.AutoSize = true;
            this.nemesisRadioButton.Enabled = false;
            this.nemesisRadioButton.Location = new System.Drawing.Point(190, 19);
            this.nemesisRadioButton.Name = "nemesisRadioButton";
            this.nemesisRadioButton.Size = new System.Drawing.Size(65, 17);
            this.nemesisRadioButton.TabIndex = 2;
            this.nemesisRadioButton.Text = "&Nemesis";
            // 
            // moduledKosinskiRadioButton
            // 
            this.moduledKosinskiRadioButton.AutoSize = true;
            this.moduledKosinskiRadioButton.Location = new System.Drawing.Point(76, 19);
            this.moduledKosinskiRadioButton.Name = "moduledKosinskiRadioButton";
            this.moduledKosinskiRadioButton.Size = new System.Drawing.Size(108, 17);
            this.moduledKosinskiRadioButton.TabIndex = 1;
            this.moduledKosinskiRadioButton.Text = "&Moduled Kosinski";
            // 
            // kosinskiRadioButton
            // 
            this.kosinskiRadioButton.AutoSize = true;
            this.kosinskiRadioButton.Checked = true;
            this.kosinskiRadioButton.Location = new System.Drawing.Point(6, 19);
            this.kosinskiRadioButton.Name = "kosinskiRadioButton";
            this.kosinskiRadioButton.Size = new System.Drawing.Size(64, 17);
            this.kosinskiRadioButton.TabIndex = 0;
            this.kosinskiRadioButton.TabStop = true;
            this.kosinskiRadioButton.Text = "&Kosinski";
            // 
            // modeGroupBox
            // 
            this.modeGroupBox.Controls.Add(this.compressRadioButton);
            this.modeGroupBox.Controls.Add(this.decompressRadioButton);
            this.modeGroupBox.Location = new System.Drawing.Point(345, 12);
            this.modeGroupBox.Name = "modeGroupBox";
            this.modeGroupBox.Size = new System.Drawing.Size(173, 42);
            this.modeGroupBox.TabIndex = 1;
            this.modeGroupBox.TabStop = false;
            this.modeGroupBox.Text = "Mode";
            // 
            // decompressRadioButton
            // 
            this.decompressRadioButton.AutoSize = true;
            this.decompressRadioButton.Location = new System.Drawing.Point(83, 19);
            this.decompressRadioButton.Name = "decompressRadioButton";
            this.decompressRadioButton.Size = new System.Drawing.Size(84, 17);
            this.decompressRadioButton.TabIndex = 1;
            this.decompressRadioButton.TabStop = true;
            this.decompressRadioButton.Text = "&Decompress";
            // 
            // formatGroupBox
            // 
            this.formatGroupBox.Controls.Add(this.kosinskiRadioButton);
            this.formatGroupBox.Controls.Add(this.moduledKosinskiRadioButton);
            this.formatGroupBox.Controls.Add(this.nemesisRadioButton);
            this.formatGroupBox.Controls.Add(this.enigmaRadioButton);
            this.formatGroupBox.Location = new System.Drawing.Point(12, 12);
            this.formatGroupBox.Name = "formatGroupBox";
            this.formatGroupBox.Size = new System.Drawing.Size(327, 42);
            this.formatGroupBox.TabIndex = 0;
            this.formatGroupBox.TabStop = false;
            this.formatGroupBox.Text = "Format";
            // 
            // errorProvider
            // 
            this.errorProvider.ContainerControl = this;
            // 
            // sourceFileSelector
            // 
            this.sourceFileSelector.DefaultExt = "bin";
            this.sourceFileSelector.FileName = "";
            this.sourceFileSelector.Filter = "All Files (*.*)|*.*";
            this.sourceFileSelector.Location = new System.Drawing.Point(83, 60);
            this.sourceFileSelector.Name = "sourceFileSelector";
            this.sourceFileSelector.Size = new System.Drawing.Size(355, 24);
            this.sourceFileSelector.TabIndex = 3;
            // 
            // destinationFileSelector
            // 
            this.destinationFileSelector.DefaultExt = "bin";
            this.destinationFileSelector.FileName = "";
            this.destinationFileSelector.Filter = "All Files (*.*)|*.*";
            this.destinationFileSelector.Location = new System.Drawing.Point(83, 90);
            this.destinationFileSelector.Mode = SonicRetro.KensSharp.Comp.FileSelectorMode.Save;
            this.destinationFileSelector.Name = "destinationFileSelector";
            this.destinationFileSelector.Size = new System.Drawing.Size(355, 24);
            this.destinationFileSelector.TabIndex = 5;
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(530, 126);
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
            this.Text = "Comp";
            this.modeGroupBox.ResumeLayout(false);
            this.modeGroupBox.PerformLayout();
            this.formatGroupBox.ResumeLayout(false);
            this.formatGroupBox.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.errorProvider)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.sourceFileSelector)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.destinationFileSelector)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label destinationLabel;
        private System.Windows.Forms.Label sourceLabel;
        private System.Windows.Forms.Button goButton;
        private System.Windows.Forms.RadioButton enigmaRadioButton;
        private System.Windows.Forms.RadioButton compressRadioButton;
        private System.Windows.Forms.RadioButton nemesisRadioButton;
        private System.Windows.Forms.RadioButton moduledKosinskiRadioButton;
        private System.Windows.Forms.RadioButton kosinskiRadioButton;
        private System.Windows.Forms.GroupBox modeGroupBox;
        private System.Windows.Forms.RadioButton decompressRadioButton;
        private System.Windows.Forms.GroupBox formatGroupBox;
        private FileSelector sourceFileSelector;
        private FileSelector destinationFileSelector;
        private System.Windows.Forms.ErrorProvider errorProvider;

    }
}

