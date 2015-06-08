using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace XdsRegistry
{
    public partial class frmSettings : Form
    {
        int currentHashValue;
        public frmSettings()
        {
            InitializeComponent();
        }

        private void cmdClose_Click(object sender, EventArgs e)
        {
            if (cmdSaveSettings.Enabled == true)
            {
                MessageBox.Show("Save settings before exiting!");
            }
            else
            {
                this.Close();
            }
        }

        private int CalcuateHash()
        {
            string[] txtStrings = new string[] { txtDomain.Text, txtDataSource.Text, txtRegistryURI.Text, txtRegistryLog.Text, txtRepositoryId.Text, txtRepositoryPath.Text, txtAtnaHost.Text, txtAtnaPort.Text, txtBroker.Text };
            string hashString = String.Concat(txtStrings);
            int hash = hashString.GetHashCode();
            return hash;
        }

        private void frmSettings_Load(object sender, EventArgs e)
        {
            txtDomain.Text = Properties.Settings.Default.AuthDomain;
            txtDataSource.Text = Properties.Settings.Default.DataSource;
            txtRegistryURI.Text = Properties.Settings.Default.RegistryURI;
            txtRepositoryId.Text = Properties.Settings.Default.RepositoryId;
            txtRepositoryPath.Text = Properties.Settings.Default.RepositoryPath;
            txtRegistryLog.Text = Properties.Settings.Default.RegistryLog;
            txtAtnaHost.Text = Properties.Settings.Default.ATNAHost;
            txtAtnaPort.Text = Properties.Settings.Default.ATNAPort.ToString();
            txtBroker.Text = Properties.Settings.Default.BrokerURI;
            Properties.Settings.Default.HashCode = CalcuateHash();
            Properties.Settings.Default.Save();
            this.Location = this.Owner.Location;
            this.Left += this.Owner.ClientSize.Width / 2 - this.Width / 2;
            this.Top += this.Owner.ClientSize.Height / 2 - this.Height / 2;
            cmdSaveSettings.Enabled = false;
            this.txtDomain.SelectionStart = 0;
            this.txtDomain.SelectionLength = 0;
            toolTip1.SetToolTip(txtDataSource, "To change, edit the config files in the Registry folder, and reload the Registry...");
        }

        private void txtAtnaPort_TextChanged(object sender, EventArgs e)
        {
            string portString = txtAtnaPort.Text;
            if (portString.Length > 4)
            {
                txtAtnaPort.Text = portString.Substring(0, 4);
                MessageBox.Show("Port number cannot be more than 4 digits");
            }

            for (int i = 0; i < portString.Length; i++)
            {
                if (!char.IsNumber(portString[i]))
                {
                    MessageBox.Show("Please insert a valid number");
                }
            }
        }

        private void cmdSaveSettings_Click(object sender, EventArgs e)
        {
            Properties.Settings.Default.AuthDomain = txtDomain.Text;
            Properties.Settings.Default.DataSource = txtDataSource.Text;
            Properties.Settings.Default.RegistryURI = txtRegistryURI.Text;
            Properties.Settings.Default.RepositoryId = txtRepositoryId.Text;
            Properties.Settings.Default.RepositoryPath = txtRepositoryPath.Text;
            Properties.Settings.Default.RegistryLog = txtRegistryLog.Text;
            Properties.Settings.Default.ATNAHost = txtAtnaHost.Text;
            Properties.Settings.Default.ATNAPort = int.Parse(txtAtnaPort.Text);
            Properties.Settings.Default.BrokerURI = txtBroker.Text;
            Properties.Settings.Default.HashCode = CalcuateHash();
            Properties.Settings.Default.Save();
            cmdSaveSettings.Enabled = false;
        }

        private void HashChanged()
        {
            int currentHash = CalcuateHash();
            if (currentHash != Properties.Settings.Default.HashCode)
            {
                currentHashValue = CalcuateHash();
                cmdSaveSettings.Enabled = true;
            }
        }

        private void txtRegistryURI_Leave(object sender, EventArgs e)
        {
            HashChanged();
        }

        private void txtRegistryLog_Leave(object sender, EventArgs e)
        {
            HashChanged();
        }

        private void txtRepositoryId_Leave(object sender, EventArgs e)
        {
            HashChanged();
        }

        private void txtRepositoryPath_Leave(object sender, EventArgs e)
        {
            HashChanged();
        }

        private void txtAtnaHost_Leave(object sender, EventArgs e)
        {
            HashChanged();
        }

        private void txtAtnaPort_Leave(object sender, EventArgs e)
        {
            HashChanged();
        }

        private void txtBroker_Leave(object sender, EventArgs e)
        {
            HashChanged();
        }

        private void txtDomain_Leave(object sender, EventArgs e)
        {
            HashChanged();
        }
    }
}
