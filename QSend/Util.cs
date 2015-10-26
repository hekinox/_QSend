using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Runtime.Serialization.Formatters.Binary;

namespace QSend
{

    class TransferPermission
    {
        public const int ACCEPTED = 1;
        public const int REJECTED = 0;
        public const int UNKNOWN = -1;
    }

    class TransferStatus
    {
        public const int WFCLIENT = 0;
        public const int WFDATA = 1;
        public const int RECVDATA = 2;
        public const int COMPLETE = 3;
    }

    /// <summary>
    /// Contains file & transport information; an object is serialized and sent between clients
    /// </summary>
    [Serializable]
    class TransferHeader
    {
        public string fileName { get; set; }
        public Int64 fileSize { get; set; }
        public string sourceIp { get; set; }
        public int nOfStreams { get; set; }
        public string filePath { get; set; }

        public int transferPermission { get; set; }

        public TransferHeader(string path, int nOfStreams)
        {
            this.filePath = path;
            this.fileName = Path.GetFileName(path);
            this.fileSize = (int)new FileInfo(path).Length;
            this.sourceIp = Util.getExternalIP();
            this.nOfStreams = nOfStreams;

            this.transferPermission = TransferPermission.UNKNOWN;
        }

    }

    /// <summary>
    /// Contains mostly static methods for general purpose.
    /// </summary>
    class Util
    {
        private static BinaryFormatter binaryFormatter = new BinaryFormatter();

        public static string getExternalIP()
        {
            using (WebClient client = new WebClient())
            {
                return client.DownloadString("http://canihazip.com/s");
            }
        }

        /// <summary>
        /// Serializes the object
        /// </summary>
        /// <param name="obj">object to serialize</param>
        /// <returns>object as byte array</returns>
        public static byte[] serialize(object obj)
        {
            MemoryStream serializationStream = new MemoryStream();
            binaryFormatter.Serialize(serializationStream, obj);
            return serializationStream.ToArray();
        }

        /// <summary>
        /// Deserializes object
        /// </summary>
        /// <param name="serializationStream">a stream containing the serialized object</param>
        /// <returns>object deserialized</returns>
        public static object deserialize(Stream serializationStream)
        {
            serializationStream.Position = 0;
            return binaryFormatter.Deserialize(serializationStream);
        }
    }
}
