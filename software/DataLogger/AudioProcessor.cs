using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Exocortex.DSP;
using System.Collections.ObjectModel;

namespace DataLogger
{
    class AudioProcessor
    {
        /// <summary>
        /// The sampling rate assumed, in Hz
        /// </summary>
        public int SamplingFrequency { get; private set; }

        /// <summary>
        /// The block size used to calculate the spectrum of the signal
        /// </summary>
        public int BlockSize { get; private set; }

        /// <summary>
        /// Contains all samples
        /// </summary>
        public ObservableCollection<short> Samples { get; private set; }

        public ObservableCollection<float[]> Spectrum { get; private set; }
        public ObservableCollection<float> SpectrumFrequencies { get; private set; }

        //Pre-calculated, 1/SamplingRate
        private double sampleInterval;

        //The sample that was last converted to the spectrum
        private int lastAnalysis = 0;

        /// <summary>
        /// Create a new audio interface
        /// </summary>
        /// <param name="_samplingRate">The sampling rate assumed, in Hz</param>
        /// <param name="_blockSize">The block size used to calculate the spectrum of the signal, must be a power of two</param>
        public AudioProcessor(int _samplingRate = 8000, int _blockSize = 128)
        {
            Samples = new ObservableCollection<short>();
            Spectrum = new ObservableCollection<float[]>();

            SamplingFrequency = _samplingRate;

            if (!isPowerOfTwo(_blockSize))
            {
                throw new ArgumentException("BlockSize must be a power of two for the FFT");
            }

            BlockSize = _blockSize;

            SpectrumFrequencies = new ObservableCollection<float>();
            for (int i = 0; i < (BlockSize / 2); i++)
            {
                SpectrumFrequencies.Add((float)((i / (float)BlockSize) * SamplingFrequency));
            }

            sampleInterval = 1.0 / SamplingFrequency;

            Samples.CollectionChanged += new System.Collections.Specialized.NotifyCollectionChangedEventHandler(Samples_CollectionChanged);
        }

        void Samples_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case System.Collections.Specialized.NotifyCollectionChangedAction.Add:
                    if (e.NewStartingIndex + e.NewItems.Count > (lastAnalysis + BlockSize))
                    {
                        lastAnalysis = ProcessSpectrum(lastAnalysis);
                    }
                    break;
                case System.Collections.Specialized.NotifyCollectionChangedAction.Remove:
                case System.Collections.Specialized.NotifyCollectionChangedAction.Reset:
                    Spectrum.Clear();
                    lastAnalysis = ProcessSpectrum();
                    break;
                default:
                    throw new NotImplementedException();
            }
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
        public int ProcessSpectrum(int blockStart = 0)
        {
            ComplexF[] data = new ComplexF[BlockSize];

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

            return blockStart;
        }
    }
}
