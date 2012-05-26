﻿using System;
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

        public static Stream WriteWav(IList<short> samples)
        {
            MemoryStream ms = new MemoryStream();

            BinaryWriter writer = new BinaryWriter(ms);

            writer.Write(new char[] { 'R', 'I', 'F', 'F' }); //chunkID RIFF
            writer.Write(36 + samples.Count); //chunkLength
            writer.Write(new char[] { 'W', 'A', 'V', 'E' }); //chunkFormat WAVE
            writer.Write(new char[] { 'f', 'm', 't', ' ' }); ; //subchunkID fmt
            writer.Write(16); //fmt size
            writer.Write((short)1); //audio format PCM
            writer.Write((short)1); //num channels 
            writer.Write(8192); //sample rate
            writer.Write(8192 * 1); //byte rate
            writer.Write((short)1); //block align
            writer.Write((short)8); //bits per sample

            writer.Write(new char[] { 'd', 'a', 't', 'a' }); //subchunkID DATA
            writer.Write(samples.Count); //subchunk length

            foreach (var sample in samples)
            {
                writer.Write((byte)(sample + 128));
            }

            return ms;
        }
    }
}