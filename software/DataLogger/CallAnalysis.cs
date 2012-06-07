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
        public bool IsValid
        {
            get
            {
                return Number != null && Number.Length == 11 && (Number[0] == '0' && "12378".Contains(Number[1]));
            }
        }
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
                if (i >= 1 && i - callStart > 0 && (tones[i].Time - tones[i - 1].Time).Seconds > 2)
                {
                    addCall(tones, callStart, i - callStart);
                    callStart = i;
                }
                
                if (i == tones.Count - 1)
                {
                    addCall(tones, callStart, i - callStart + 1);
                    callStart = i + 1;
                }
                else if (i - callStart >= 10)
                {
                    addCall(tones, callStart, 11);
                    callStart = i + 1;
                }
            }
        }

        private void addCall(IList<Tone> tones, int callStart, int count)
        {
            var c = new Call();
            c.Time = tones[callStart].Time;

            for (int j = callStart; j < (callStart + count); j++)
            {
                c.Number += tones[j].KeyString;
            }

            Calls.Add(c);
        }
    }
}
