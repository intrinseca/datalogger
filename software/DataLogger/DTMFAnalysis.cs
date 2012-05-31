using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Collections.ObjectModel;

namespace DataLogger
{
    public enum DTMFTones
    {
        Num1,
        Num2,
        Num3,
        Num4,
        Num5,
        Num6,
        Num7,
        Num8,
        Num9,
        Star,
        Num0,
        Hash
    }

    public class DTMFFrequencies
    {
        public static int[] Rows = new int[] { 687, 770, 852, 941 };
        public static int[] Columns = new int[] { 1209, 1336, 1477 };

        public static int[,] Tones = new int[Rows.Length * Columns.Length, 2];

        static DTMFFrequencies()
        {
            for (int row = 0; row < Rows.Length; row++)
            {
                for (int column = 0; column < Columns.Length; column++)
                {
                    Tones[row * Columns.Length + column, 0] = Rows[row];
                    Tones[row * Columns.Length + column, 0] = Columns[column];
                }
            }
        }
    }

    public class Tone
    {
        public int StartBlock;
        public DTMFTones Key;
        public int Duration;

        private string[] keyStrings = new string[] { "1", "2", "3", "4", "5", "6", "7", "8", "9", "*", "0", "#" };

        public string KeyString
        {
            get
            {
                return keyStrings[(int)Key];
            }
        }

        public override string ToString()
        {
            return string.Format("{0}-{1}: {2}", StartBlock, StartBlock + Duration, Key.ToString());
        }
    }

    public class DTMFAnalysis
    {
        const float RATIO_THRESHOLD = 4.0f;
        const float ABS_THRESHOLD = 0.01f;

        public ObservableCollection<Tone> Tones { get; set; }

        public DTMFAnalysis()
        {
            Tones = new ObservableCollection<Tone>();
        }

        public void Analyse(IList<float[]> spectrum, IList<float> spectrumFrequencies, int startBlock = 0)
        {
            if(startBlock == 0)
                Tones.Clear();

            int[] rowIndexes = new int[DTMFFrequencies.Rows.Length];
            float[] rowMagnitudes = new float[rowIndexes.Length];

            int[] columnIndexes = new int[DTMFFrequencies.Columns.Length];
            float[] columnMagnitudes = new float[columnIndexes.Length];
                        
            //calculate the indexes into spectrum for the row and column frequencies
            rowIndexes = findFrequencies(spectrumFrequencies, DTMFFrequencies.Rows);
            columnIndexes = findFrequencies(spectrumFrequencies, DTMFFrequencies.Columns);

            int row = -1, column = -1;
            int previousRow, previousColumn;

            for (int block = startBlock; block < spectrum.Count; block++)
            {
                previousRow = row;
                previousColumn = column;

                row = findTone(spectrum[block], rowIndexes);
                column = findTone(spectrum[block], columnIndexes);

                if (row != -1 && column != -1)
                {
                    //found a tone
                    if ((row != previousRow || column != previousColumn))
                    {
                        //tone has changed
                        int x = row * DTMFFrequencies.Columns.Length + column;
                        Tones.Add(new Tone() { StartBlock = block, Key = (DTMFTones)x, Duration = 1 });
                    }
                    else
                    {
                        //same tone
                        Tones[Tones.Count - 1].Duration++;
                    }
                }
            }
        }

        private int[] findFrequencies(IList<float> spectrumFrequencies, int[] toneFrequencies)
        {

            int[] indexes = new int[toneFrequencies.Length];

            int index = 0;

            for (int i = 0; i < spectrumFrequencies.Count && index < indexes.Length; i++)
            {
                if (spectrumFrequencies[i] > toneFrequencies[index])
                {
                    indexes[index] = i;
                    index++;
                }
            }

            return indexes;
        }

        private int findTone(float[] spectrum, int[] indexes)
        {
            float[] magnitudes = new float[indexes.Length];

            int tone = -1;

            for (int i = 0; i < indexes.Length; i++)
            {
                magnitudes[i] = (spectrum[indexes[i] - 1] + spectrum[indexes[i]] + spectrum[indexes[i] + 1]) / 3.0f;
            }

            float max = magnitudes.Max();
            float min = magnitudes.Min();

            if (max / min > RATIO_THRESHOLD && max > ABS_THRESHOLD)
                tone = findValue(max, magnitudes);

            return tone;
        }

        private int findValue(float value, float[] array)
        {
            for (int i = 0; i < array.Length; i++)
            {
                if (array[i] == value)
                    return i;
            }

            throw new Exception("Value not found");
        }
    }
}
