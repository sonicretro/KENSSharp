namespace SonicRetro.KensSharp.Comp
{
    using System;
    using System.Windows.Forms;
    using System.Diagnostics;

    public partial class MainForm : Form
    {
        public MainForm()
        {
            this.InitializeComponent();
        }

        private void goButton_Click(object sender, EventArgs e)
        {
            bool error = false;

            if (string.IsNullOrEmpty(this.sourceFileSelector.FileName))
            {
                this.errorProvider.SetError(this.sourceFileSelector, Properties.Resources.EmptySource);
                error = true;
            }
            else
            {
                this.errorProvider.SetError(this.sourceFileSelector, null);
            }

            if (string.IsNullOrEmpty(this.destinationFileSelector.FileName))
            {
                this.errorProvider.SetError(this.destinationFileSelector, Properties.Resources.EmptyDestination);
                error = true;
            }
            else
            {
                this.errorProvider.SetError(this.destinationFileSelector, null);
            }

            if (!error)
            {
#if DEBUG
                this.Execute();
#else
                try
                {
                    this.Execute();
                }
                catch (Exception ex)
                {
                    if (Debugger.IsAttached)
                    {
                        throw;
                    }

                    MessageBox.Show(this, "An error occurred." + Environment.NewLine + Environment.NewLine + ex.ToString(), this.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
#endif
            }
        }

        private void Execute()
        {
            if (this.compressRadioButton.Checked)
            {
                if (this.kosinskiRadioButton.Checked)
                {
                    Kosinski.Compress(this.sourceFileSelector.FileName, this.destinationFileSelector.FileName, false);
                }
                else if (this.moduledKosinskiRadioButton.Checked)
                {
                    Kosinski.Compress(this.sourceFileSelector.FileName, this.destinationFileSelector.FileName, true);
                }
                else if (this.nemesisRadioButton.Checked)
                {
                    Nemesis.Compress(this.sourceFileSelector.FileName, this.destinationFileSelector.FileName);
                }
                else if (this.enigmaRadioButton.Checked)
                {
                    Enigma.Compress(this.sourceFileSelector.FileName, this.destinationFileSelector.FileName, Endianness.BigEndian);
                }
            }
            else if (this.decompressRadioButton.Checked)
            {
                if (this.kosinskiRadioButton.Checked)
                {
                    Kosinski.Decompress(this.sourceFileSelector.FileName, this.destinationFileSelector.FileName, false);
                }
                else if (this.moduledKosinskiRadioButton.Checked)
                {
                    Kosinski.Decompress(this.sourceFileSelector.FileName, this.destinationFileSelector.FileName, true);
                }
                else if (this.nemesisRadioButton.Checked)
                {
                    Nemesis.Decompress(this.sourceFileSelector.FileName, this.destinationFileSelector.FileName);
                }
                else if (this.enigmaRadioButton.Checked)
                {
                    Enigma.Decompress(this.sourceFileSelector.FileName, this.destinationFileSelector.FileName, Endianness.BigEndian);
                }
            }
        }
    }
}
