using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Data.Entity;
using System.Net;
using System.Configuration;
using System.Net.Sockets;
using System.IO;

namespace XdsRegistry
{
    public partial class frmRegistry : Form
    {
        Registry Reg = new Registry();
        Registry.LogMessageHandler LogMessageHandler;
        DateTime currentDate = DateTime.Now;

        public frmRegistry()
        {
            InitializeComponent();
            LogMessageHandler = new Registry.LogMessageHandler(logMessageHandler);
            this.Text = DateTime.Now.ToString("dd/MM/yyyy") + " - HSS XDS Registry";
            SetupProperties();
        }

        private void SetupProperties()
        {
            Reg.LogMessageEvent -= LogMessageHandler;
            bool readProperties = Reg.readProperties();
            if (readProperties == false)
            {
                MessageBox.Show("Error in reading properties...", "Error");
            }
            else
            {
                //uncomment to drop DB and populate patient table
                Database.SetInitializer(new ContextInitializer());
                Reg.LogMessageEvent += LogMessageHandler;
                Reg.StartListen();
                logWindow.AppendText((DateTime.Now.ToString("HH:mm:ss.fff") + ": Authority Domain - " + Reg.authDomain + "...\n"));
                logWindow.AppendText((DateTime.Now.ToString("HH:mm:ss.fff") + ": Registry Log - " + Reg.registryLog + "...\n"));
                lblRegUri.Text = Reg.registryURI;

                using (XdsDataBase db = new XdsDataBase())
                {
                    Reg.dataSource = db.Database.Connection.DataSource;
                    Properties.Settings.Default.DataSource = Reg.dataSource;
                    Properties.Settings.Default.Save();

                    try
                    {
                        db.Database.Connection.Open();
                        logWindow.AppendText(DateTime.Now.ToString("HH:mm:ss.fff") + ": Connected to Registry database at " + Reg.dataSource + "...\n");
                        db.Database.Connection.Close();
                    }
                    catch (Exception ex)
                    {
                        logWindow.AppendText(DateTime.Now.ToString("HH:mm:ss.fff") + ": Unable to connect to Registry database at " + Reg.dataSource + "...\n");
                    }
                }
                testConnections();
            }
        }

        private void testConnections()
        {
            logWindow.AppendText("--- --- ---\n");
            //test ATNA connection
            testConnection("ATNA", Reg.atnaHost, Reg.atnaPort);

            //test Notification Recipient connection
            //extract hostname
            int posDoubleSlash = Reg.notificationRecipient.IndexOf("//");
            if (posDoubleSlash > -1)
            {
                //extract port
                int posRecipientPort = Reg.notificationRecipient.IndexOf(":", posDoubleSlash);
                if (posRecipientPort > -1)
                {
                    string hostname = Reg.notificationRecipient.Substring(posDoubleSlash + 2, posRecipientPort - (posDoubleSlash + 2));
                    int recipientPort = int.Parse(Reg.notificationRecipient.Substring(posRecipientPort + 1, 4));
                    testConnection("Notification Recipient", hostname, recipientPort);
                }
            }
        }

        private bool testConnection(string hostname, string host, int port)
        {
            string textToSend = DateTime.Now.ToString();
            TcpClient client = new TcpClient();
            try
            {
                client.Connect(host, port);
                NetworkStream nwStream = client.GetStream();
                byte[] bytesToSend = ASCIIEncoding.ASCII.GetBytes(textToSend);
                nwStream.Write(bytesToSend, 0, bytesToSend.Length);
                logWindow.AppendText(DateTime.Now.ToString("HH:mm:ss.fff") + ": Connection open to " + hostname + " at " + host + ":" + port + "...\n");
                client.Close();
                return true;
            }
            catch (Exception ex)
            {
                logWindow.AppendText(DateTime.Now.ToString("HH:mm:ss.fff") + ": Connection failed to " + hostname + " at " + host + ":" + port + "...\n");
                client.Close();
                return false;
            }
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == (Keys.Control | Keys.Shift | Keys.S))
            {
                //MessageBox.Show("Do Something");
                frmSettings frmSettings = new frmSettings();
                frmSettings.StartPosition = FormStartPosition.Manual;
                frmSettings.Owner = this;
                frmSettings.ShowDialog();
                return true;
            }
            else if (keyData == (Keys.F4))
            {
                Reg.StopListen();
                logWindow.AppendText("--- --- ---\n");
                logWindow.AppendText(DateTime.Now.ToString("HH:mm:ss.fff") + ": Refreshing values...\n");
                SetupProperties();
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        void logMessageHandler(string msg)
        {
            if (InvokeRequired)
            {
                Invoke(LogMessageHandler, new object[] { msg });
            }
            else
            {
                logWindow.AppendText(msg + "\n"); 
            }
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            DialogResult closeDecision = MessageBox.Show("Are you sure you want to close the XDS Registry?", "Close Registry", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button1);
            if (closeDecision == DialogResult.Yes)
            {
                clearLogWindow();
                Reg.StopListen();
                this.Close();
            }
        }

        private void clearLogWindow()
        {
            //create log of all days entries
            using (StreamWriter sw = new StreamWriter(Reg.registryLog + "eventLog_" + DateTime.Now.ToString("ddMMyyyy") + ".txt", true))
            {
                string allevents = logWindow.Text;
                sw.Write("--- --- ---\n");
                sw.Write(allevents);
            }
        }

        private void tmrRepositoryConn_Tick(object sender, EventArgs e)
        {
            if (DateTime.Now.Date > currentDate.Date)
            {
                clearLogWindow();
                this.Text = DateTime.Now.ToString("dd/MM/yyyy") + " - HSS XDS Registry";
                logWindow.Text = "";
                Reg.StopListen();
                logWindow.AppendText("--- --- ---\n");
                SetupProperties();
                currentDate = DateTime.Now;
            }
            testConnections();
        }
    }
}
