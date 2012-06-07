using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;

namespace DataLogger
{
    class Call
    {
        public DateTime Time { get; set; }
        public string Number { get; set; }
    }


    class CallAnalysis
    {
        public ObservableCollection<Call> Calls { get; set; }

        public CallAnalysis()
        {
            Calls = new ObservableCollection<Call>();
        }

        public void Analyse(IList<Tone> tones)
        {
            Calls.Clear();

            int callStart = 0;

            for (int i = 0; i < tones.Count; i++)
            {
                if (i >= 1 && (tones[i].Time - tones[i - 1].Time).Seconds > 2)
                {
                    addCall(tones, callStart, i - callStart - 1);
                    callStart = i;
                }

                if (i == tones.Count - 1)
                {
                    addCall(tones, callStart, i - callStart);
                    callStart = i + 1;
                    continue;
                }

                if (i - callStart >= 10)
                {
                    addCall(tones, callStart, 10);
                    callStart = i + 1;
                    continue;
                }
            }
        }

        private void addCall(IList<Tone> tones, int callStart, int count)
        {
            var c = new Call();
            c.Time = tones[callStart].Time;

            for (int j = callStart; j <= (callStart + count); j++)
            {
                c.Number += tones[j].KeyString;
            }

            Calls.Add(c);
        }
    }
}
