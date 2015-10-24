using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Windows.Forms;


namespace QSend
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        DataClient handshakeClient;
        private MemoryStream responseStream = new MemoryStream();

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

            Gatherer gatherer = new Gatherer(header);

            for (int i = 0; i < header.nOfStreams; i++)
            {
                fileClients.Add(new DataClient(i+1, 2700 + i + 1, new FileStream("chunk" + i.ToString() + ".txt", FileMode.Create), handleTransferChunk, false));
                
                //fileClients[i].sendData(header.sourceIp, 2700 + i + 1, Encoding.ASCII.GetBytes("Hello"));
                fileClients[i].sendData(header.sourceIp, 2700 + i + 1, gatherer.getNextChunk());
            }
        }


        private void Form1_Load(object sender, EventArgs e)
        {
            handshakeClient = new DataClient(0, 2700, responseStream, handleTransferHeader, true);
            string filePath = "testfile.zip.txt";
            int nOfStreams = 4;

            TransferHeader transferHeader = new TransferHeader(filePath, nOfStreams);
            
            handshakeClient.sendData("127.0.0.1", 2700, Util.serialize(transferHeader));


        }


    }
}
