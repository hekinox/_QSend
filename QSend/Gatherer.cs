using System;
using System.Diagnostics;
using System.IO;

namespace QSend
{
    class Gatherer
    {
        private TransferHeader header;
        private int currentIndex = 0;
        private Int64 chunkSize = 0;

        private FileStream f;

        public Gatherer(TransferHeader header)
        {
            this.header = header;
            f = new FileStream(header.fileName, FileMode.Open);

            chunkSize = header.fileSize / header.nOfStreams;
            
        }

        public byte[] getNextChunk()
        {
            byte[] buffer = new byte[chunkSize];
            f.Read(buffer, 0, (int)chunkSize);

            return buffer;
        }
    }
}
