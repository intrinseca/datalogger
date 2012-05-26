using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Exocortex.DSP;

namespace DataLogger
{
    class AudioProcessor
    {
        /// <summary>
        /// The sampling rate assumed, in Hz
        /// </summary>
        public int SamplingRate { get; private set; }

        /// <summary>
        /// The block size used to calculate the spectrum of the signal
        /// </summary>
        public int BlockSize { get; private set; }

        /// <summary>
        /// Contains all samples
        /// </summary>

        public List<short> Samples { get; private set; }
        public List<float[]> Spectrum { get; private set; }

        //Pre-calculated, 1/SamplingRate
        private double sampleInterval;

        /// <summary>
        /// Create a new audio interface
        /// </summary>
        /// <param name="_samplingRate">The sampling rate assumed, in Hz</param>
        /// <param name="_blockSize">The block size used to calculate the spectrum of the signal, must be a power of two</param>
        public AudioProcessor(int _samplingRate = 8000, int _blockSize = 128)
        {
            Samples = new List<short>();
            Spectrum = new List<float[]>();

            SamplingRate = _samplingRate;

            if (!isPowerOfTwo(_blockSize))
            {
                throw new ArgumentException("BlockSize must be a power of two for the FFT");
            }

            BlockSize = _blockSize;

            sampleInterval = 1.0 / SamplingRate;
        }

        /// <summary>
        /// Check if the parameter is a power of two
        /// </summary>
        bool isPowerOfTwo(int x)
        {
            return (x & (x - 1)) == 0;
        }

        /// <summary>
        /// Calculate the spectrum of the signal, in blocks of BlockSize
        /// </summary>
        public void ProcessSpectrum()
        {
            int blockStart = 0;

            ComplexF[] data = new ComplexF[BlockSize];

            Spectrum.Clear();

            while (blockStart + BlockSize < Samples.Count)
            {
                float[] result = new float[BlockSize];

                for (int i = 0; i < BlockSize; i++)
                {
                    data[i].Re = (float)(Samples[blockStart + i] / (float)short.MaxValue);
                    data[i].Im = 0.0f;
                }

                Fourier.FFT(data, FourierDirection.Forward);

                for (int i = 0; i < BlockSize; i++)
                {
                    result[i] = data[i].GetModulus();
                }

                Spectrum.Add(result);
                blockStart += BlockSize;
            }
        }
    }
}
