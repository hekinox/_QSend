using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;

namespace QSend
{
    class DataClient
    {
        private IPEndPoint ipEndPoint;
        private TcpClient client;
        private TcpListener server;
        private Stream outputStream;

        private bool keepStreamAlive = false;

        // server thread (receive)
        BackgroundWorker worker1 = new BackgroundWorker();

        // client thread (send)
        BackgroundWorker worker2;

        /// <summary>
        /// Creates a new DataClient that acts as a TcpListener & TcpClient
        /// </summary>
        /// <param name="index">index of the current client (for identification)</param>
        /// <param name="port">port used</param>
        /// <param name="outputStream">the stream used to return any data received</param>
        /// <param name="updateTransferStatus">eventhandler for transfer status</param>
        /// <param name="keepStreamAlive">indicates if the stream should be closed or not after receiving data</param>
        public DataClient(int index, int port, Stream outputStream, Action<object, ProgressChangedEventArgs, int> updateTransferStatus, bool keepStreamAlive)
        {
            this.ipEndPoint = new IPEndPoint(IPAddress.Any, port);
            this.server = new TcpListener(ipEndPoint);
            this.outputStream = outputStream;
            this.keepStreamAlive = keepStreamAlive;

            server.Start();

            worker1.WorkerReportsProgress = true;
            worker1.DoWork += acceptClient;
            worker1.ProgressChanged += (sender, e) => updateTransferStatus(sender, e, index); 
            
            worker1.RunWorkerAsync();       
        }

        private void acceptClient(object sender, DoWorkEventArgs e)
        {
     	    while (true)
            {
                worker1.ReportProgress(0, TransferStatus.WFCLIENT);
                
                TcpClient senderClient = server.AcceptTcpClient();
                NetworkStream ns = senderClient.GetStream();
                outputStream.SetLength(0); // clear the last stream
                
                worker1.ReportProgress(0, TransferStatus.WFDATA);

                byte[] buffer = new byte[2048];
                int bytesRead = 0;

                while (ns.DataAvailable)
                {
                    bytesRead = ns.Read(buffer, 0, buffer.Length);
                    outputStream.Write(buffer, 0, bytesRead);

                    worker1.ReportProgress(0, TransferStatus.RECVDATA);
                }

                if (!keepStreamAlive)
                {
                    outputStream.Close();
                }
                // status unknown
                
                worker1.ReportProgress(100, TransferStatus.COMPLETE);
            }
        }

        private void _sendData(object sender, DoWorkEventArgs e)
        {
            List<object> args = (List<object>)e.Argument;

            string ip = (string)args[0];
            int port = (int)args[1];
            byte[] buffer = (byte[])args[2];

            IPEndPoint destEndPoint = new IPEndPoint(IPAddress.Parse(ip), port);

            this.client = new TcpClient();
            client.Connect(destEndPoint);

            NetworkStream ns = client.GetStream();

            int _currentIndex = 0;
            int size = buffer.Length;
            while (size > 0)
            {
                ns.Write(buffer, _currentIndex, 2048 > size ? size : 2048);
                size -= 2048;
                _currentIndex += 2048;
            }
            ns.Flush();
            ns.Close();
            client.Close();

            worker2.CancelAsync();
            
        }

        /// <summary>
        /// Sends data via TCP to the client
        /// </summary>
        /// <param name="ip">ip address</param>
        /// <param name="port">used port</param>
        /// <param name="buffer">data as byte array</param>
        public void sendData(string ip, int port, byte[] buffer)
        {
            List<object> args = new List<object>();
            args.Add(ip);
            args.Add(port);
            args.Add(buffer); // correct value here
            
            worker2 = new BackgroundWorker();
            worker2.WorkerReportsProgress = true;
            worker2.WorkerSupportsCancellation = true;
            worker2.DoWork += _sendData;

            worker2.RunWorkerAsync(args);

            
        }
    }
}
