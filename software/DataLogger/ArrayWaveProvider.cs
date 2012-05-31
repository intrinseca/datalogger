using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.ComponentModel;
using NAudio.Wave;
using NAudio.CoreAudioApi;
using System.Collections.Specialized;
using System.Timers;
using System.Windows.Threading;

namespace DataLogger
{
    class ArrayWaveProvider : IWaveProvider
    {
        private IList<short> samples;
        private int arrayPosition;

        public double CurrentPosition
        {
            get
            {
                return (double)arrayPosition / WaveFormat.AverageBytesPerSecond;
            }
            set
            {
                arrayPosition = (int)(value * WaveFormat.AverageBytesPerSecond);
            }
        }

        public WaveFormat WaveFormat {get; private set;}

        public ArrayWaveProvider(IList<short> samples, WaveFormat format)
        {
            this.samples = samples;
            WaveFormat = format;
        }

        public int Read(byte[] buffer, int offset, int count)
        {
            int i = 0;

            while (i < count && arrayPosition < samples.Count)
            {
                buffer[i + offset] = (byte)(128 + samples[arrayPosition]);
                i++;
                arrayPosition++;
            }

            return i;
        }
    }
}
