using System;
using System.Diagnostics;
using System.IO;

namespace QSend
{
    class Gatherer
    {
        private TransferHeader header;
        private Int64 chunkSize = 0;

        private FileStream inputStream;

        private int _streamsRead = 0;

        public Gatherer(TransferHeader header)
        {
            this.header = header;
            inputStream = new FileStream(header.filePath, FileMode.Open);

            chunkSize = 1 + header.fileSize / header.nOfStreams;
            _streamsRead = 0;   
        }

        public byte[] getNextChunk()
        {
            byte[] buffer = new byte[chunkSize];
            inputStream.Read(buffer, 0, (int)chunkSize);
            _streamsRead++;


            if (_streamsRead == header.nOfStreams)
                inputStream.Close();

            return buffer;
        }

        // to do: move in a background worker thread
        public void assembleChunks()
        {
            FileStream outputStream = new FileStream("[RECV]" + header.fileName, FileMode.Create);

            for (int i = 0; i < header.nOfStreams; i++)
            {
                byte[] buffer = File.ReadAllBytes("chunk" + i.ToString() + ".txt");
                outputStream.Write(buffer, 0, buffer.Length);
            }

            outputStream.Close();

        }
    }
}
