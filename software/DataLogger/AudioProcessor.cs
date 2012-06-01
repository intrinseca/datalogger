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

		/// <summary>
		/// Contains the block-by-block FFT of the data - each entry is the array of the spectrum for that block
		/// </summary>
		public ObservableCollection<float[]> Spectrum { get; private set; }

		/// <summary>
		/// Contains the pre-calculated real frequencies for each FFT sample
		/// </summary>
		public ObservableCollection<float> SpectrumFrequencies { get; private set; }

		//The sample that was last converted to the spectrum
		private int lastAnalysis = 0;
		
		/// <summary>
		/// Implement INotifyPropertyChanged, raised when a property likely to be bound is changed
		/// </summary>
		public event PropertyChangedEventHandler PropertyChanged;

		//Used to regularly raise PropertyChanged for data that does not itself notify, e.g. stream position
		DispatcherTimer notifyProperties;

		//Audio output interface
		DirectSoundOut player;

		//Provider to map sample data to the required format for playback
		ArrayWaveProvider provider;

		/// <summary>
		/// Create a new audio interface
		/// </summary>
		/// <param name="samplingFrequency">The sampling rate assumed, in Hz</param>
		/// <param name="blockSize">The block size used to calculate the spectrum of the signal, must be a power of two</param>
		public AudioProcessor(int samplingFrequency = 8000, int blockSize = 128)
		{
			//Initialise collections and fields
			Samples = new ObservableCollection<short>();
			Spectrum = new ObservableCollection<float[]>();
			SamplingFrequency = samplingFrequency;

			Samples.CollectionChanged += new NotifyCollectionChangedEventHandler(Samples_CollectionChanged);
			OnPropertyChanged("Samples");

			//Check and initialise block size
			if (!isPowerOfTwo(blockSize))
			{
				throw new ArgumentException("BlockSize must be a power of two for the FFT");
			}
			BlockSize = blockSize;

			//Calculate the FFT frequencies
			SpectrumFrequencies = new ObservableCollection<float>();
			for (int i = 0; i < (BlockSize / 2); i++)
			{
				SpectrumFrequencies.Add((float)((i / (float)BlockSize) * SamplingFrequency));
			}

			//Initialise the sound player
			player = new DirectSoundOut();
			var format = new WaveFormat(SamplingFrequency, 8, 1);
			provider = new ArrayWaveProvider(Samples, format);
			player.Init(provider);

			player.PlaybackStopped += new EventHandler<StoppedEventArgs>(player_PlaybackStopped);

			//Initialise the property changed timer
			notifyProperties = new DispatcherTimer();
			notifyProperties.Interval = new TimeSpan(0, 0, 0, 0, 10);
			notifyProperties.Tick += new EventHandler(notifyProperties_Tick);
		}

		/// <summary>
		/// Process modifications to the samples collection
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		void Samples_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			switch (e.Action)
			{
				case NotifyCollectionChangedAction.Add:
					//If we have enough new samples for an FFT block, update the FFT
					if (e.NewStartingIndex + e.NewItems.Count > (lastAnalysis + BlockSize))
					{
						lastAnalysis = ProcessSpectrum(lastAnalysis);
					}
					break;
				case NotifyCollectionChangedAction.Remove:
				case NotifyCollectionChangedAction.Reset:
					//If any items are removed, redo all the calculations
					Spectrum.Clear();
					lastAnalysis = ProcessSpectrum();
					break;
				default:
					throw new NotImplementedException();
			}
		}

		/// <summary>
		/// Fill the samples collection using data from a WAV file
		/// </summary>
		/// <param name="path"></param>
		public void LoadFile(string path)
		{
			//Create file reader
			var reader = new WaveFileReader(path);

			//Create buffer
			var data = new byte[reader.SampleCount];

			//Fill buffer from file
			reader.Read(data, 0, (int)reader.SampleCount);

			//Copy buffer into samples collection
			for (int i = 0; i < data.Length; i++)
			{
				Samples.Add((short)(data[i] - 128));
			}
		}

		#region "Fourier Analysis"

		/// <summary>
		/// Check if the parameter is a power of two
		/// </summary>
		bool isPowerOfTwo(int x)
		{
			return (x & (x - 1)) == 0;
		}

		/// <summary>
		/// Return the hamming window
		/// </summary>
		/// <param name="n">The sample number</param>
		/// <param name="N">The width (in samples) of the window</param>
		/// <returns></returns>
		private float hammingWindow(int n, int N)
		{
			float w = (float)(0.5 * (1.0 - Math.Cos((2 * Math.PI * n) / (N - 1))));
			return w;
		}

		/// <summary>
		/// Calculate the spectrum of the signal, in blocks of BlockSize
		/// </summary>
		/// <param name="blockStart">Resume the analysis from this block, retaining the old data</param>
		/// <returns>The start of the next block to be calculated, if analysis is resumed</returns>
		public int ProcessSpectrum(int blockStart = 0)
		{
			//Temporary array to store the complex FFT input
			ComplexF[] data = new ComplexF[BlockSize];

			//Block index
			int j = 0;

			//Window value
			float w;

			//While we have at least a block of samples to process remaining
			while (blockStart + BlockSize < Samples.Count)
			{

				//Apply the window function and fill the temporary input array
				for (int i = 0; i < BlockSize; i++)
				{
					w = hammingWindow(i, BlockSize);

					data[i].Re = w * (float)(Samples[blockStart + i] / (float)short.MaxValue);
					data[i].Im = 0.0f;
				}

				//Calculate the FFT
				Fourier.FFT(data, FourierDirection.Forward);

				//Store the magnitude of the result
				float[] result = new float[BlockSize];
				for (int i = 0; i < BlockSize; i++)
				{
					result[i] = data[i].GetModulus();
				}

				//Add to the spectrum data list
				Spectrum.Add(result);

				//Increment counters
				blockStart += BlockSize;
				j++;
			}

			//Return the last starting address, to be used if restarting
			return blockStart;
		}
		#endregion

		#region "Playback Interface"

		/// <summary>
		/// True if audio is playing
		/// </summary>
		public bool IsPlaying
		{
			get { return player.PlaybackState == PlaybackState.Playing; }
		}

		/// <summary>
		/// Get or set the current seek position in the stream, in seconds
		/// </summary>
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

		/// <summary>
		/// Start playing audio from the current position
		/// </summary>
		public void Play()
		{
			player.Play();
			//Start the property update timer
			notifyProperties.Start();
		}

		/// <summary>
		/// Stop playing audio
		/// </summary>
		public void Stop()
		{
			player.Stop();
		}

		/// <summary>
		/// Pause audio playback
		/// </summary>
		public void Pause()
		{
			player.Pause();
		}

		/// <summary>
		/// If the playback is stopped, stop the property update timer.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		void player_PlaybackStopped(object sender, StoppedEventArgs e)
		{
			notifyProperties.Stop();
		}

		/// <summary>
		/// Property update timer callback
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		void notifyProperties_Tick(object sender, EventArgs e)
		{
			//ChannelPosition Changed
			OnPropertyChanged("ChannelPosition");
		}

		/// <summary>
		/// Helper for raising PropertyChanged
		/// </summary>
		/// <param name="name"></param>
		private void OnPropertyChanged(string name)
		{
			if (PropertyChanged != null)
				PropertyChanged(this, new PropertyChangedEventArgs(name));
		}
		#endregion
	}
}
