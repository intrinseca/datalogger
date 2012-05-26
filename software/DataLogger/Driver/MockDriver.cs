using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DataLogger
{
    class MockDriver : IDriver
    {
        int t;
        float f = 100f;

        public void Open()
        {
        }

        public void Close()
        {
        }

        public bool CheckDevicePresent()
        {
            return true;
        }

        public byte[] SendCommand(COMMANDS command, int responseLength)
        {
            switch (command)
            {
                case COMMANDS.ADC_READ:
                    double audio;
                    byte[] response = new byte[responseLength];
                    response[0] = (byte)command;

                    for (int i = 1; i < responseLength; i++)
                    {
                        audio = 128.0 + 50.0 * Math.Sin(2.0 * Math.PI * t / f);
                        t++;

                        response[i] = (byte)audio;

                        if (t % 200 == 0)
                        {
                            f -= 1f;

                            if (f < 0.5f)
                                f = 100f;
                        }
                    }

                    return response;
                default:
                    throw new NotImplementedException();
            }
        }

        public byte[] SendCommand(byte[] command, int responseLength)
        {
            throw new NotImplementedException();
        }
    }
}
