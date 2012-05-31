using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace DataLogger
{
    /// <summary>
    /// Interaction logic for Spectrum.xaml
    /// </summary>
    public partial class Spectrum : UserControl
    {
        /// <summary>
        /// The length of the FFT block that was used to calculate the FFT
        /// </summary>
        public int BlockSize
        {
            get { return (int)GetValue(BlockSizeProperty); }
            set { SetValue(BlockSizeProperty, value); }
        }

        public static readonly DependencyProperty BlockSizeProperty =
            DependencyProperty.Register("BlockSize", typeof(int), typeof(Spectrum), new UIPropertyMetadata(128));

        /// <summary>
        /// The horizontal scale factor
        /// </summary>
        public float Timebase
        {
            get { return (float)GetValue(TimebaseProperty); }
            set { SetValue(TimebaseProperty, value); }
        }

        public static readonly DependencyProperty TimebaseProperty =
            DependencyProperty.Register("Timebase", typeof(float), typeof(Spectrum), new UIPropertyMetadata(1.0f));        

        /// <summary>
        /// The FFT data
        /// </summary>
        public ObservableCollection<float[]> Data
        {
            get { return (ObservableCollection<float[]>)GetValue(DataProperty); }
            set { SetValue(DataProperty, value); }
        }

        public static readonly DependencyProperty DataProperty =
            DependencyProperty.Register("Data", typeof(ObservableCollection<float[]>), typeof(Spectrum), new UIPropertyMetadata(null, new PropertyChangedCallback(dataChanged)));

        public event ScrollChangedEventHandler ScrollChanged;

        public Spectrum()
        {
            InitializeComponent();
        }

        private static void dataChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var s = (Spectrum)sender;
            s.Data.CollectionChanged += new NotifyCollectionChangedEventHandler(s.Data_CollectionChanged);
        }

        void Data_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                addSlices(e.NewItems);
            }
            else if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                for (int i = 0; i < e.OldItems.Count; i++)
                {
                    stkSpectrum.Children.RemoveAt(e.OldStartingIndex);
                }
            }
            else if (e.Action == NotifyCollectionChangedAction.Reset)
            {
                stkSpectrum.Children.Clear();
                foreach (var slice in Data)
                {
                    addSlices(slice);
                }
            }
            else
            {
                throw new InvalidOperationException("Operation not supported by spectrogram");
            }
        }

        private void addSlices(System.Collections.IList items)
        {
            foreach (float[] slice in items)
            {
                var im = new Image();
                im.Source = generateSlice(slice);
                im.Stretch = Stretch.Fill;
                im.Height = slice.Length;

                var b = new Binding("Timebase");
                b.Source = this;
                im.SetBinding(Image.WidthProperty, b);

                stkSpectrum.Children.Add(im);
            }
        }

        /// <summary>
        /// Convert a 0-1 float into a spectrum colour
        /// </summary>
        /// <param name="value">Input value</param>
        /// <returns>Three-byte array of R G B values</returns>
        private byte[] mapRainbowColor(float value, bool invert = false)
        {
            // Convert into a value between 0 and 1023.
            int scaledValue = (int)(1023 * value);

            //Flip if required
            if (invert)
            {
                scaledValue = 1023 - scaledValue;
            }

            // Map different color bands.
            if (scaledValue < 256)
            {
                // Red to yellow, (255, 0, 0) to (255, 255, 0).
                return new byte[] { 255, (byte)scaledValue, 0 };
            }
            else if (scaledValue < 512)
            {
                // Yellow to green. (255, 255, 0) to (0, 255, 0).
                scaledValue -= 256;
                return new byte[] { (byte)(255 - scaledValue), 255, 0 };
            }
            else if (scaledValue < 768)
            {
                // Green to aqua. (0, 255, 0) to (0, 255, 255).
                scaledValue -= 512;
                return new byte[] { 0, 255, (byte)scaledValue };
            }
            else
            {
                // Aqua to blue. (0, 255, 255) to (0, 0, 255).
                scaledValue -= 768;
                return new byte[] { 0, (byte)(255 - scaledValue), 255 };
            }
        }

        public BitmapSource generateSlice(float[] spectrum)
        {
            int imageHeight = spectrum.Length / 2;
            int imageWidth = 1;
            int stride = imageWidth * 3;

            byte[] image = new byte[stride * imageHeight];

            float scale = 10.0f;
            float valueF;

            //for each frequency component
            for (int j = 0; j < imageHeight; j++)
            {
                //scale and clip the value
                valueF = scale * spectrum[j];
                if (valueF > 1.0f) valueF = 1.0f;
                if (valueF < 0.0f) valueF = 0.0f;

                //calculate colour and set pixel values
                byte[] color = mapRainbowColor(valueF, true);

                image[((imageHeight - 1 - j) * stride)] = color[0];
                image[((imageHeight - 1 - j) * stride) + 1] = color[1];
                image[((imageHeight - 1 - j) * stride) + 2] = color[2];
            }

            //load pixel data into image
            return BitmapSource.Create(imageWidth, imageHeight, 96, 96, PixelFormats.Rgb24, null, image, stride);
        }

        private void onScrollChanged(ScrollChangedEventArgs e)
        {
            if (ScrollChanged != null)
                ScrollChanged(this, e);
        }

        private void ScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            onScrollChanged(e);
        }

        public void ScrollTo(double offset)
        {
            scroll.ScrollToHorizontalOffset(offset);
        }
    }
}
