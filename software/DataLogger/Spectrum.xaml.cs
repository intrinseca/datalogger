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

namespace DataLogger
{
    /// <summary>
    /// Interaction logic for Spectrum.xaml
    /// </summary>
    public partial class Spectrum : UserControl
    {
        public int BlockSize
        {
            get { return (int)GetValue(BlockSizeProperty); }
            set { SetValue(BlockSizeProperty, value); }
        }

        public static readonly DependencyProperty BlockSizeProperty =
            DependencyProperty.Register("BlockSize", typeof(int), typeof(Spectrum), new UIPropertyMetadata(128));

        public List<float[]> Data
        {
            get { return (List<float[]>)GetValue(DataProperty); }
            set { SetValue(DataProperty, value); }
        }

        public static readonly DependencyProperty DataProperty =
            DependencyProperty.Register("Data", typeof(List<float[]>), typeof(Spectrum), new UIPropertyMetadata(null));

        public Spectrum()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Convert a 0-1 float into a spectrum colour
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        private byte[] mapRainbowColor(float value)
        {
            // Convert into a value between 0 and 1023.
            int int_value = (int)(1023 * value);

            // Map different color bands.
            if (int_value < 256)
            {
                return new byte[] { 255, (byte)int_value, 0 };
            }
            else if (int_value < 512)
            {
                // Yellow to green. (255, 255, 0) to (0, 255, 0).
                int_value -= 256;
                return new byte[] { (byte)(255 - int_value), 255, 0 };
            }
            else if (int_value < 768)
            {
                // Green to aqua. (0, 255, 0) to (0, 255, 255).
                int_value -= 512;
                return new byte[] { 0, 255, (byte)int_value };
            }
            else
            {
                // Aqua to blue. (0, 255, 255) to (0, 0, 255).
                int_value -= 768;
                return new byte[] { 0, (byte)(255 - int_value), 255 };
            }
        }

        public void Refresh()
        {
            if (Data.Count == 0)
                return;

            int imageHeight = BlockSize / 2;
            int imageWidth = BlockSize * Data.Count;

            int stride = imageWidth * 3;
            float scale = 1.0f;

            byte[] image = new byte[stride * imageHeight];
            float valueF;
            byte value;

            for (int i = 0; i < Data.Count; i++)
            {
                scale = 1.0f / Data[i].Max();

                for (int j = 0; j < imageHeight; j++)
                {
                    valueF = scale * Data[i][j];
                    if (valueF > 1.0f) valueF = 1.0f;
                    if (valueF < 0.0f) valueF = 0.0f;

                    byte[] color = mapRainbowColor(valueF);

                    for (int k = 0; k < BlockSize; k++)
                    {

                        image[(j * stride) + (BlockSize * i) + k * 3] = color[0];
                        image[(j * stride) + (BlockSize * i) + k * 3 + 1] = color[1];
                        image[(j * stride) + (BlockSize * i) + k * 3 + 2] = color[2];
                    }
                }
            }

            BitmapSource specGraph = BitmapSource.Create(imageWidth, imageHeight, 120, 120, PixelFormats.Rgb24, null, image, stride);
            imgSpectrum.Source = specGraph;
        }
    }
}
