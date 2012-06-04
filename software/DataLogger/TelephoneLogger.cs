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
    class TelephoneLogger
    {
        public const int SAMPLING_RATE = 8192;
        public const int BLOCK_SIZE = 256;

        /// <summary>
        /// Connection to the USB device
        /// </summary>
        public IDriver Device = new Driver();

        /// <summary>
        /// Storage and processing of audio data
        /// </summary>
        public AudioProcessor Audio { get; private set; }

        public DTMFAnalysis Analyser { get; private set; }

        public bool Connected { get; private set; }

        private Timer pollTimer;

        /// <summary>
        /// TODO: Move into device class
        /// </summary>
        private DeviceMonitor monitor;

        public TelephoneLogger()
        {
            Audio = new AudioProcessor(SAMPLING_RATE, BLOCK_SIZE);
            monitor = new DeviceMonitor(Device);
            Analyser = new DTMFAnalysis();

            Device.DataReceived += new DataReceivedEventHandler(Device_DataReceived);
            monitor.Connected += new EventHandler(monitor_Connected);

            Audio.Spectrum.CollectionChanged += new System.Collections.Specialized.NotifyCollectionChangedEventHandler(Spectrum_CollectionChanged);

            pollTimer = new Timer(4);
            pollTimer.AutoReset = true;
            pollTimer.Elapsed += new ElapsedEventHandler(pollTimer_Elapsed);
        }

        void monitor_Connected(object sender, EventArgs e)
        {
            Device.DataReceived += new DataReceivedEventHandler(Device_DataReceived);
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

        void pollTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            PollDevice();
        }

        public void BeginPolling()
        {
            Device.SendCommand(COMMANDS.SAMPLING_START, 1);
        }

        public void StopPolling()
        {
            Device.SendCommand(COMMANDS.SAMPLING_STOP, 1);
        }

        public void PollDevice()
        {
            var result = Device.SendCommand(COMMANDS.ADC_READ, 64);

            for (int i = 2; i < result[1]; i++)
            {
                int sample = 128 - result[i];
                Audio.Samples.Add((short)sample);
            }
        }

        public void LoadFile(string filePath)
        {
            Audio.Samples.Clear();
            Audio.LoadFile(filePath);
        }

        public void UpdateAnalysis()
        {
            //Audio.ProcessSpectrum();
            Analyser.Analyse(Audio.Spectrum, Audio.SpectrumFrequencies);
        }

        public void Clear()
        {
            Audio.Samples.Clear();
            Analyser.Tones.Clear();
        }

        public byte GetADC()
        {
            //return Device.SendCommand(COMMANDS.ADC_READ, 2)[1];
            return 0;
        }

        public void Close()
        {
            StopPolling();
            monitor.Stop();
        }
    }
}
