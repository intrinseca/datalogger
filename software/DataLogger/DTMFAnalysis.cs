using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DataLogger
{
    enum DTMFTones
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

    class DTMFFrequencies
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

    class Tone
    {
        public int BlockNumber;
        public DTMFTones Key;

        public override string ToString()
        {
            return string.Format("{0}: {1}", BlockNumber, Key.ToString());
        }
    }

    class DTMFAnalysis
    {
        const float THRESHOLD = 2.5f;

        public List<Tone> Tones;

        public void Analyse(List<float[]> spectrum, List<float> spectrumFrequencies)
        {
            Tones = new List<Tone>();

            int[] rowIndexes = new int[DTMFFrequencies.Rows.Length];
            float[] rowMagnitudes = new float[rowIndexes.Length];

            int[] columnIndexes = new int[DTMFFrequencies.Columns.Length];
            float[] columnMagnitudes = new float[columnIndexes.Length];
                        
            //calculate the indexes into spectrum for the row and column frequencies
            rowIndexes = findFrequencies(spectrumFrequencies, DTMFFrequencies.Rows);
            columnIndexes = findFrequencies(spectrumFrequencies, DTMFFrequencies.Columns);

            int row = -1, column = -1;
            int previousRow, previousColumn;

            for (int block = 0; block < spectrum.Count; block++)
            {
                previousRow = row;
                previousColumn = column;

                row = findTone(spectrum[block], rowIndexes);
                column = findTone(spectrum[block], columnIndexes);

                if (row != -1 && column != -1 && row != previousRow && column != previousColumn)
                {
                    int x = row * DTMFFrequencies.Columns.Length + column;

                    Tones.Add(new Tone() { BlockNumber = block, Key = (DTMFTones)x });
                }
            }
        }

        private int[] findFrequencies(List<float> spectrumFrequencies, int[] toneFrequencies)
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

            if (max / min > THRESHOLD)
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
