using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace DataLogger
{
    class WaveFile
    {
        public static byte[] GetSamples(string file)
        {
            byte[] byteArray;

            BinaryReader reader = new BinaryReader(new FileStream(file, FileMode.Open, FileAccess.Read));
            
            int chunkID = reader.ReadInt32();
            int fileSize = reader.ReadInt32();
            int riffType = reader.ReadInt32();
            int fmtID = reader.ReadInt32();
            int fmtSize = reader.ReadInt32();
            int fmtCode = reader.ReadInt16();
            int channels = reader.ReadInt16();
            int sampleRate = reader.ReadInt32();
            int fmtAvgBPS = reader.ReadInt32();
            int fmtBlockAlign = reader.ReadInt16();
            int bitDepth = reader.ReadInt16();

            if (fmtSize == 18)
            {
                // Read any extra values
                int fmtExtraSize = reader.ReadInt16();
                reader.ReadBytes(fmtExtraSize);
            }

            int dataID = reader.ReadInt32();
            int dataSize = reader.ReadInt32();

            byteArray = reader.ReadBytes(dataSize);

            return byteArray;
        }
    }
}
