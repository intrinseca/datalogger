using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Exocortex.DSP;
using System.Collections.ObjectModel;
using NAudio.Wave;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows.Threading;

namespace DataLogger
{
	public class AudioProcessor : INotifyPropertyChanged
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

		//Pre-calculated, 1/SamplingRate
		private double sampleInterval;

		//The sample that was last converted to the spectrum
		private int lastAnalysis = 0;
		
		public event PropertyChangedEventHandler PropertyChanged;

		DispatcherTimer notifyProperties;

		/// <summary>
		/// Create a new audio interface
		/// </summary>
		/// <param name="_samplingRate">The sampling rate assumed, in Hz</param>
		/// <param name="_blockSize">The block size used to calculate the spectrum of the signal, must be a power of two</param>
		public AudioProcessor(int _samplingRate = 8000, int _blockSize = 128)
		{
			Samples = new ObservableCollection<short>();
			Spectrum = new ObservableCollection<float[]>();

			OnPropertyChanged("Samples");

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

			Samples.CollectionChanged += new NotifyCollectionChangedEventHandler(Samples_CollectionChanged);

			player = new DirectSoundOut();
			var format = new WaveFormat(SamplingFrequency, 8, 1);
            provider = new ArrayWaveProvider(Samples, format);
            player.Init(provider);

			player.PlaybackStopped += new EventHandler<StoppedEventArgs>(player_PlaybackStopped);

			notifyProperties = new DispatcherTimer();
			notifyProperties.Interval = new TimeSpan(0, 0, 0, 0, 10);
			notifyProperties.Tick += new EventHandler(notifyProperties_Tick);
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

		public void LoadFile(string path)
		{
			var reader = new WaveFileReader(path);
			var data = new byte[reader.SampleCount];

			reader.Read(data, 0, (int)reader.SampleCount);

			for (int i = 0; i < data.Length; i++)
			{
				Samples.Add((short)(data[i] - 128));
			}
		}

		#region "Fourier Analysis"

		public ObservableCollection<float[]> Spectrum { get; private set; }
		public ObservableCollection<float> SpectrumFrequencies { get; private set; }

		/// <summary>
		/// Check if the parameter is a power of two
		/// </summary>
		bool isPowerOfTwo(int x)
		{
			return (x & (x - 1)) == 0;
		}

		private float hammingWindow(int n, int N)
		{
			float w = (float)(0.5 * (1.0 - Math.Cos((2 * Math.PI * n) / (N - 1))));
			return w;
		}

		/// <summary>
		/// Calculate the spectrum of the signal, in blocks of BlockSize
		/// </summary>
		public int ProcessSpectrum(int blockStart = 0)
		{
			ComplexF[] data = new ComplexF[BlockSize];

			int j = 0;
			float w;

			while (blockStart + BlockSize < Samples.Count)
			{
				float[] result = new float[BlockSize];

				for (int i = 0; i < BlockSize; i++)
				{
					w = hammingWindow(i, BlockSize);

					data[i].Re = w * (float)(Samples[blockStart + i] / (float)short.MaxValue);
					data[i].Im = 0.0f;
				}

				Fourier.FFT(data, FourierDirection.Forward);

				for (int i = 0; i < BlockSize; i++)
				{
					result[i] = data[i].GetModulus();
				}

				Spectrum.Add(result);
				blockStart += BlockSize;
				j++;
			}

			return blockStart;
		}
		#endregion

		#region "Playback Interface"

		DirectSoundOut player;
		ArrayWaveProvider provider;

		public void Play()
		{
			player.Play();
			notifyProperties.Start();
		}

        public void Stop()
        {
            player.Stop();
        }

        public void Pause()
        {
            player.Pause();
        }

        void player_PlaybackStopped(object sender, StoppedEventArgs e)
        {
            notifyProperties.Stop();
        }

        void notifyProperties_Tick(object sender, EventArgs e)
        {
            OnPropertyChanged("ChannelPosition");
        }

		private void OnPropertyChanged(string name)
		{
			if (PropertyChanged != null)
				PropertyChanged(this, new PropertyChangedEventArgs(name));
		}

		public bool IsPlaying
		{
			get { return player.PlaybackState == PlaybackState.Playing; }
		}

		public double ChannelLength
		{
			get
			{
				if (Samples.Count > 0)
					return Samples.Count * (1.0f / SamplingFrequency);
				else
					return 0.0;
			}
		}

		public double ChannelPosition
		{
			get
			{
				return provider.CurrentPosition;
			}
			set
			{
				provider.CurrentPosition = value;
				OnPropertyChanged("ChannelPosition");
			}
		}

		public TimeSpan SelectionBegin { get; set; }

		public TimeSpan SelectionEnd { get; set; }

		#endregion
	}
}
