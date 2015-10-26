using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;


namespace QSend
{
    public partial class MainWnd : Form
    {
        public MainWnd()
        {
            InitializeComponent();
        }

        DataClient handshakeClient;
        Gatherer gatherer;
        private MemoryStream responseStream = new MemoryStream();

        int nOfStreams = 4;

        private void handleTransferHeader(object sender, ProgressChangedEventArgs e, int index)
        {
            if ((int)e.UserState == TransferStatus.COMPLETE)
            {
                TransferHeader header1 = (TransferHeader)(Util.deserialize(responseStream));

                // fie primeste un header pt permisiune fie primeste inapoi headerul cu tot cu permisiune

                if (header1.transferPermission == TransferPermission.REJECTED)
                    return;

                if (header1.transferPermission == TransferPermission.UNKNOWN)
                {
                    // to do: request permission for receiving this
                    header1.transferPermission = TransferPermission.ACCEPTED;
                    
                    handshakeClient.sendData(header1.sourceIp, 2700, Util.serialize(header1));

                    return;
                }

                if (header1.transferPermission == TransferPermission.ACCEPTED)
                {
                    sendChunks(header1);
                }
            }
        }

        int streamsCompleted = 0;
        private void handleTransferChunk(object sender, ProgressChangedEventArgs e, int index)
        {
            if (e.ProgressPercentage == 100)
                streamsCompleted++;

            if (streamsCompleted == nOfStreams)
            {
                gatherer.assembleChunks();
                streamsCompleted = 0;
            }
        }

        List<DataClient> fileClients;
        private void sendChunks(TransferHeader header)
        {
            fileClients = new List<DataClient>();

            gatherer = new Gatherer(header);

            for (int i = 0; i < header.nOfStreams; i++)
            {
                fileClients.Add(new DataClient(i+1, 2700 + i + 1, new FileStream("chunk" + i.ToString() + ".txt", FileMode.Create), handleTransferChunk, false));
                
                fileClients[i].sendData(header.sourceIp, 2700 + i + 1, gatherer.getNextChunk());
            }
        
        }


        private void MainWnd_Load(object sender, EventArgs e)
        {
            // handshakeClient (receiver)
            handshakeClient = new DataClient(0, 2700, responseStream, handleTransferHeader, true);

            ipTextBox.Text = "127.0.0.1";



        }

        private void selectFileButton_Click(object sender, EventArgs e)
        {
            FileDialog fileDialog = new OpenFileDialog();
            DialogResult result = fileDialog.ShowDialog();

            if (result == DialogResult.OK)
            {
                TransferHeader transferHeader = new TransferHeader(fileDialog.FileName, nOfStreams);

                handshakeClient.sendData(ipTextBox.Text, 2700, Util.serialize(transferHeader));
            }
        }



        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.DrawRectangle(Pens.CadetBlue, 0, 0, this.Width-1, this.Height-1);
        }

        private void closeProgram_Click(object sender, EventArgs e)
        {
            this.Close();
        }



        [DllImportAttribute("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int LPAR);
        [DllImportAttribute("user32.dll")]
        public static extern bool ReleaseCapture();

        const int WM_NCLBUTTONDOWN = 0xA1;
        const int HT_CAPTION = 0x2;

        private void moveWindow(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(this.Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
            }
        }

        
    }
}
