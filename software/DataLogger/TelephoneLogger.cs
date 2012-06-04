using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Timers;
using System.Collections.Specialized;
using System.Diagnostics;

namespace DataLogger
{
    /// <summary>
    /// Container class for telephone logger
    /// </summary>
    class TelephoneLogger : IDisposable
    {
        public const int SAMPLING_RATE = 8192;
        public const int BLOCK_SIZE = 256;

        /// <summary>
        /// Connection to the USB device
        /// </summary>
        public IDriver Device { get; private set; }

        /// <summary>
        /// Storage and processing of audio data
        /// </summary>
        public AudioProcessor Audio { get; private set; }

        public DTMFAnalysis Analyser { get; private set; }

        private bool _capturing;
        public bool Capturing 
        { 
            get
            {
                return _capturing;
            }
            set
            {
                if (Device.IsOpen)
                {
                    if (!value && _capturing)
                    {
                        Device.SendCommand(COMMANDS.CAPTURE_STOP, 1);
                    }
                    else if(value && !_capturing)
                    {
                        Device.SendCommand(COMMANDS.CAPTURE_START, 1);
                    }
                }
                _capturing = value;
            }
        }

        public TelephoneLogger()
        {
            Audio = new AudioProcessor(SAMPLING_RATE, BLOCK_SIZE);
            Analyser = new DTMFAnalysis();

            Device = new Driver(true);
            Device.Disconnected += new EventHandler(Device_Disconnected);
            Device.DataReceived += new DataReceivedEventHandler(Device_DataReceived);

            Audio.Spectrum.CollectionChanged += new NotifyCollectionChangedEventHandler(Spectrum_CollectionChanged);
        }

        void Device_Disconnected(object sender, EventArgs e)
        {
            Capturing = false;
        }

        void Device_DataReceived(object sender, DataReceivedEventArgs e)
        {
            App.Current.Dispatcher.Invoke(new Action<byte[]>(addSample), e.Buffer);
        }

        private void addSample(byte[] buffer)
        {
            for (int i = 1; i < buffer.Length; i++)
            {
                int sample = 128 - buffer[i];
                Audio.Samples.Add((short)sample);
            }
        }

        void Spectrum_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                Analyser.Analyse(Audio.Spectrum, Audio.SpectrumFrequencies, e.NewStartingIndex);
            }
        }

        public void LoadFile(string filePath)
        {
            Audio.Samples.Clear();
            Audio.LoadFile(filePath);
        }

        public void Clear()
        {
            Audio.Samples.Clear();
            Analyser.Tones.Clear();
        }

        public byte GetADC()
        {
            return Device.SendCommand(COMMANDS.ADC_READ, 2)[1];
        }

        public void Dispose()
        {
            Capturing = false;
            Device.Dispose();
        }
    }
}
