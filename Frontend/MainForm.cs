namespace SonicRetro.KensSharp.Frontend
{
    using System;
    using System.Windows.Forms;

    public partial class MainForm : Form
    {
#if !DEBUG
        private static readonly string CompressErrorMessageHeader = "An error occurred.";
        private static readonly string DecompressErrorMessageHeader = "An error occurred. Make sure you have selected the correct compression format.";
#endif

        public MainForm()
        {
            this.InitializeComponent();
            this.LoadSettings();
        }

        private void MainForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            this.SaveSettings();
        }

        private void goButton_Click(object sender, EventArgs e)
        {
            this.SaveSettings();

#if DEBUG
            this.Execute();
#else
            try
            {
                this.Execute();
            }
            catch (Exception ex)
            {
                if (System.Diagnostics.Debugger.IsAttached)
                {
                    throw;
                }

                string header = this.decompressRadioButton.Checked && ex is CompressionException ? DecompressErrorMessageHeader : CompressErrorMessageHeader;
                MessageBox.Show(this, header + Environment.NewLine + Environment.NewLine + ex.ToString(), this.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
#endif
        }

        private void formatListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            this.SetSizeParameterEnabled();
            this.SetEndiannessParameterEnabled();
            this.SetGoButtonEnabled();
        }

        private void modeRadioButton_CheckedChanged(object sender, EventArgs e)
        {
            this.SetSizeParameterEnabled();
        }

        private void sizeParameterHexCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            this.sizeParameterNumericUpDown.Hexadecimal = this.sizeParameterHexCheckBox.Checked;
        }

        private void fileSelector_FileNameChanged(object sender, EventArgs e)
        {
            this.SetGoButtonEnabled();
        }

        private void LoadSettings()
        {
            Properties.Settings settings = Properties.Settings.Default;
            this.formatListBox.SelectedIndex = settings.Format;
            this.compressRadioButton.Checked = settings.Mode == 0;
            this.decompressRadioButton.Checked = settings.Mode == 1;
            this.sizeParameterNumericUpDown.Value = settings.SizeParameter;
            this.sizeParameterHexCheckBox.Checked = settings.SizeParameterHex;
            this.endiannessComboBox.SelectedIndex = settings.EndiannessParameter;
            this.sourceFileSelector.FileName = settings.SourceFilePath;
            this.destinationFileSelector.FileName = settings.DestinationFilePath;
        }

        private void SaveSettings()
        {
            Properties.Settings settings = Properties.Settings.Default;
            settings.Format = this.formatListBox.SelectedIndex;
            if (this.compressRadioButton.Checked)
            {
                settings.Mode = 0;
            }
            else if (this.decompressRadioButton.Checked)
            {
                settings.Mode = 1;
            }

            settings.SizeParameter = this.sizeParameterNumericUpDown.Value;
            settings.SizeParameterHex = this.sizeParameterHexCheckBox.Checked;
            settings.EndiannessParameter = this.endiannessComboBox.SelectedIndex;
            settings.SourceFilePath = this.sourceFileSelector.FileName;
            settings.DestinationFilePath = this.destinationFileSelector.FileName;
            settings.Save();
        }

        private void SetSizeParameterEnabled()
        {
            bool enableSizeParameter = this.formatListBox.SelectedIndex == 5 && this.decompressRadioButton.Checked;
            this.sizeParameterLabel.Enabled = enableSizeParameter;
            this.sizeParameterNumericUpDown.Enabled = enableSizeParameter;
            this.sizeParameterHexCheckBox.Enabled = enableSizeParameter;
        }

        private void SetEndiannessParameterEnabled()
        {
            bool enableEndiannessParameter = this.formatListBox.SelectedIndex == 1 || this.formatListBox.SelectedIndex == 2;
            this.endiannessLabel.Enabled = enableEndiannessParameter;
            this.endiannessComboBox.Enabled = enableEndiannessParameter;
        }

        private void SetGoButtonEnabled()
        {
            this.goButton.Enabled =
                this.formatListBox.SelectedIndex != -1 &&
                !string.IsNullOrEmpty(this.sourceFileSelector.FileName) &&
                !string.IsNullOrEmpty(this.destinationFileSelector.FileName);
        }

        private void Execute()
        {
            if (this.compressRadioButton.Checked)
            {
                switch (this.formatListBox.SelectedIndex)
                {
                    case 0: // Kosinski
                        Kosinski.Compress(this.sourceFileSelector.FileName, this.destinationFileSelector.FileName);
                        break;

                    case 1: // Moduled Kosinski
                        ModuledKosinski.Compress(this.sourceFileSelector.FileName, this.destinationFileSelector.FileName, (Endianness)this.endiannessComboBox.SelectedIndex);
                        break;

                    case 2: // Enigma
                        Enigma.Compress(this.sourceFileSelector.FileName, this.destinationFileSelector.FileName, (Endianness)this.endiannessComboBox.SelectedIndex);
                        break;

                    case 3: // Nemesis
                        Nemesis.Compress(this.sourceFileSelector.FileName, this.destinationFileSelector.FileName);
                        break;

                    case 4: // Saxman (with size)
                        Saxman.Compress(this.sourceFileSelector.FileName, this.destinationFileSelector.FileName, true);
                        break;

                    case 5: // Saxman (without size)
                        Saxman.Compress(this.sourceFileSelector.FileName, this.destinationFileSelector.FileName, false);
                        break;
                }
            }
            else if (this.decompressRadioButton.Checked)
            {
                switch (this.formatListBox.SelectedIndex)
                {
                    case 0: // Kosinski
                        Kosinski.Decompress(this.sourceFileSelector.FileName, this.destinationFileSelector.FileName);
                        break;

                    case 1: // Moduled Kosinski
                        ModuledKosinski.Decompress(this.sourceFileSelector.FileName, this.destinationFileSelector.FileName, (Endianness)this.endiannessComboBox.SelectedIndex);
                        break;

                    case 2: // Enigma
                        Enigma.Decompress(this.sourceFileSelector.FileName, this.destinationFileSelector.FileName, (Endianness)this.endiannessComboBox.SelectedIndex);
                        break;

                    case 3: // Nemesis
                        Nemesis.Decompress(this.sourceFileSelector.FileName, this.destinationFileSelector.FileName);
                        break;

                    case 4: // Saxman (with size)
                        Saxman.Decompress(this.sourceFileSelector.FileName, this.destinationFileSelector.FileName);
                        break;

                    case 5: // Saxman (without size)
                        Saxman.Decompress(this.sourceFileSelector.FileName, this.destinationFileSelector.FileName, (long)this.sizeParameterNumericUpDown.Value);
                        break;
                }
            }
        }
    }
}
