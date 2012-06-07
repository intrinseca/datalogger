using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace DataLogger
{
    /// <summary>
    /// All the DTMF tones, used as indexes into tone arrays
    /// </summary>
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

    /// <summary>
    /// Stores the constant definitions of the DTMF frequencies
    /// </summary>
    public class DTMFFrequencies
    {
        /// <summary>
        /// The frequencies of the rows, in Hz
        /// </summary>
        public static int[] Rows = new int[] { 687, 770, 852, 941 };

        /// <summary>
        /// The frequencies of the columns, in Hz
        /// </summary>
        public static int[] Columns = new int[] { 1209, 1336, 1477 };

        /// <summary>
        /// A array containing the two frequencies for each button
        /// </summary>
        public static int[,] Tones = new int[Rows.Length * Columns.Length, 2];

        /// <summary>
        /// Constructor, create the <see cref="Tones">Tones</see> array
        /// </summary>
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

    /// <summary>
    /// Stores the data for a single detected tone
    /// </summary>
    public class Tone : INotifyPropertyChanged
    {
        /// <summary>
        /// The block the tone started in
        /// </summary>
        public int StartBlock { get; set; }

        /// <summary>
        /// The key the tone corresponds to
        /// </summary>
        public DTMFTones Key { get; set; }

        /// <summary>
        /// The number of blocks the tone spans
        /// </summary>
        private int m_Duration;
        public int Duration
        {
            get
            {
                return m_Duration;
            }
            set
            {
                m_Duration = value;
                OnPropertyChanged("Duration");
            }
        }

        public DateTime Time { get; set; }

        /// <summary>
        /// String representations of each key
        /// </summary>
        private string[] keyStrings = new string[] { "1", "2", "3", "4", "5", "6", "7", "8", "9", "*", "0", "#" };

        /// <summary>
        /// Implement INotifyPropertyChanged
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Return the string representation of this tone's key
        /// </summary>
        public string KeyString
        {
            get
            {
                return keyStrings[(int)Key];
            }
        }
        /// <summary>
        /// Return the string representation of this tone as "StartBlock-EndBlock: Key"
        /// </summary>
        public override string ToString()
        {
            return string.Format("{0}-{1}: {2}", StartBlock, StartBlock + Duration, Key.ToString());
        }

        /// <summary>
        /// Helper to raise PropertyChanged
        /// </summary>
        /// <param name="name"></param>
        private void OnPropertyChanged(string name)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(name));
        }
    }

    /// <summary>
    /// Analyse a spectrum and determine the DTMF tones present, and hence the keys pressed
    /// </summary>
    public class DTMFAnalysis : INotifyPropertyChanged
    {
        /// <summary>
        /// The ratio of max to min tone intensity to declare a tone present
        /// </summary>
        const float RATIO_THRESHOLD = 4.0f;

        /// <summary>
        /// The minimum max tone strength to declare a tone present, prevents detections in noise
        /// </summary>
        const float ABS_THRESHOLD = 0.01f;

        /// <summary>
        /// The list of detected tones
        /// </summary>
        public ObservableCollection<Tone> Tones { get; set; }

        //Flag for used in Analyse for whether the previous tone has ended
        bool continueTone = false;

        /// <summary>
        /// Implement INotifyPropertyChanged
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Constructor
        /// </summary>
        public DTMFAnalysis()
        {
            Tones = new ObservableCollection<Tone>();
        }

        /// <summary>
        /// Analyse a spectrum and find DTMF tones to put in <see cref="Tones">Tones</see>
        /// </summary>
        /// <param name="spectrum">The input FFT results</param>
        /// <param name="spectrumFrequencies">The frequencies of the FFT samples</param>
        /// <param name="startBlock">Block in <paramref name="spectrum">spectrum</paramref> to resume analysis at</param>
        public void Analyse(IList<float[]> spectrum, IList<float> spectrumFrequencies, int startBlock = 0)
        {
            //If not resuming, delete old tones
            if (startBlock == 0)
                Tones.Clear();

            //Store the indexes into the spectrum of the row and column frequencies
            int[] rowIndexes = new int[DTMFFrequencies.Rows.Length];
            int[] columnIndexes = new int[DTMFFrequencies.Columns.Length];
            rowIndexes = findFrequencies(spectrumFrequencies, DTMFFrequencies.Rows);
            columnIndexes = findFrequencies(spectrumFrequencies, DTMFFrequencies.Columns);

            //Initialise row and column to -1 = no tone found
            int row = -1, column = -1;

            //For each block
            for (int block = startBlock; block < spectrum.Count; block++)
            {
                //Find the dominant row and column tones
                row = findTone(spectrum[block], rowIndexes);
                column = findTone(spectrum[block], columnIndexes);

                //If we found both a row and column
                if (row != -1 && column != -1)
                {
                    //Calculate the corresponding key
                    int key = row * DTMFFrequencies.Columns.Length + column;

                    if (Tones.Count > 0 && (int)Tones[Tones.Count - 1].Key == key && continueTone)
                    {
                        //If the key has not changed and no gap has been found, make the previous tone longer
                        Tones[Tones.Count - 1].Duration++;
                    }
                    else
                    {
                        //Otherwise add a new tone
                        Tones.Add(new Tone() { StartBlock = block, Key = (DTMFTones)key, Duration = 1, Time = DateTime.Now });
                        continueTone = true;
                    }
                }
                else
                {
                    continueTone = false;
                }
            }
        }

        /// <summary>
        /// Find the indexes into the FFT block that correspond to the given frequencies in Hz
        /// </summary>
        /// <param name="spectrumFrequencies">The mapping of FFT block to frequency, in ascending order</param>
        /// <param name="toneFrequencies">The tones to find, in ascending order</param>
        /// <returns>Array of indexes into spectrumFrequencies</returns>
        private int[] findFrequencies(IList<float> spectrumFrequencies, int[] toneFrequencies)
        {
            //Create return value
            int[] indexes = new int[toneFrequencies.Length];

            //Start at lowest frequency
            int index = 0;

            //Loop over mapping
            for (int i = 0; i < spectrumFrequencies.Count && index < indexes.Length; i++)
            {
                if (spectrumFrequencies[i] > toneFrequencies[index])
                {
                    //If we went past the target, store and move on to next tone
                    indexes[index] = i;
                    index++;
                }
            }

            //Return result
            return indexes;
        }

        /// <summary>
        /// Search the spectum for the tones in <paramref name="indexes"/>
        /// </summary>
        /// <param name="spectrum">Spectrum to search</param>
        /// <param name="indexes">The indexes to compare</param>
        /// <returns>The index into <paramref name="indexes"/> that was found, or -1 if none found</returns>
        private int findTone(float[] spectrum, int[] indexes)
        {
            //Initialise return data
            int tone = -1;

            //Find the magnitudes of the tones of interest
            float[] magnitudes = new float[indexes.Length];

            for (int i = 0; i < indexes.Length; i++)
            {
                magnitudes[i] = (spectrum[indexes[i] - 1] + spectrum[indexes[i]] + spectrum[indexes[i] + 1]) / 3.0f;
            }

            //Find the maximum and minimum magnitudes
            float max = magnitudes.Max();
            float min = magnitudes.Min();

            //If the maximum is both indepently strong, and stronger than the minimum tone
            if (max / min > RATIO_THRESHOLD && max > ABS_THRESHOLD)
            {
                //Find the corresponding index and return
                tone = findValue(max, magnitudes);
            }

            return tone;
        }

        /// <summary>
        /// Find <paramref name="value"/> in <paramref name="array"/>
        /// </summary>
        /// <param name="value">Needle</param>
        /// <param name="array">Haystack</param>
        /// <returns>The index of <paramref name="value"/> in <paramref name="array"/></returns>
        private int findValue(float value, float[] array)
        {
            //Loop over array
            for (int i = 0; i < array.Length; i++)
            {
                if (array[i] == value)
                    //Return index if values match
                    return i;
            }

            //Throw exception if value not found
            throw new Exception("Value not found");
        }

        /// <summary>
        /// Helper to raise PropertyChanged
        /// </summary>
        /// <param name="name"></param>
        private void OnPropertyChanged(string name)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(name));
        }
    }
}
