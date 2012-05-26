using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Timers;

namespace DataLogger
{
    /// <summary>
    /// Container class for telephone logger
    /// </summary>
    class TelephoneLogger
    {
        private const int SAMPLING_RATE = 8192;
        private const int BLOCK_SIZE = 1024;

        /// <summary>
        /// Connection to the USB device
        /// </summary>
        public IDriver Device = new MockDriver();

        /// <summary>
        /// Storage and processing of audio data
        /// </summary>
        public AudioProcessor Audio = new AudioProcessor(SAMPLING_RATE, BLOCK_SIZE);

        public bool Connected { get; private set; }

        private Timer pollTimer;

        /// <summary>
        /// TODO: Move into device class
        /// </summary>
        private DeviceMonitor monitor;

        public TelephoneLogger()
        {
            monitor = new DeviceMonitor(Device);

            pollTimer = new Timer(4);
            pollTimer.AutoReset = true;
            pollTimer.Elapsed += new ElapsedEventHandler(pollTimer_Elapsed);
        }

        void pollTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            PollDevice();
        }

        public void BeginPolling()
        {
            pollTimer.Start();
        }

        public void StopPolling()
        {
            pollTimer.Stop();
        }

        private void pollTimerCallback(object state)
        {
            PollDevice();
        }

        public void PollDevice()
        {
            var result = Device.SendCommand(COMMANDS.ADC_READ, 128);

            for (int i = 2; i < result[1]; i++)
            {
                int sample = 128 - result[i];
                Audio.Samples.Add((short)sample);
            }
        }
    }
}
