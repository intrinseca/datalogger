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
    /// <summary>
    /// Provide samples to the NAudio library from a sample array
    /// </summary>
    class ArrayWaveProvider : IWaveProvider
    {
        //The sample source
        private IList<short> samples;

        //The current index into the array
        private int arrayPosition;

        /// <summary>
        /// The position in the sample array, in seconds
        /// </summary>
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

        /// <summary>
        /// The format of the audio samples
        /// </summary>
        public WaveFormat WaveFormat {get; private set;}

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="samples">The source of the samples</param>
        /// <param name="format">The format of the audio samples</param>
        public ArrayWaveProvider(IList<short> samples, WaveFormat format)
        {
            this.samples = samples;
            WaveFormat = format;
        }

        /// <summary>
        /// Read from the source into a buffer
        /// </summary>
        /// <param name="buffer">The buffer to fill</param>
        /// <param name="offset">Offset into the buffer to place data</param>
        /// <param name="count">Number of bytes of the buffer to fill</param>
        /// <returns>The number of bytes read</returns>
        public int Read(byte[] buffer, int offset, int count)
        {
            //Buffer index
            int i = 0;

            //Until the buffer is full or we run out of data
            while (i < count && arrayPosition < samples.Count)
            {
                //Copy sample to buffer in correct format
                buffer[i + offset] = (byte)(128 + samples[arrayPosition]);

                //Increment counters
                i++;
                arrayPosition++;
            }

            //Return number of bytes copied
            return i;
        }
    }
}
