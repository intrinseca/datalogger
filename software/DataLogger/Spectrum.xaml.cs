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

        public void Refresh()
        {
            int imageHeight = BlockSize;
            int imageWidth = BlockSize * Data.Count;

            int stride = imageWidth;
            float scale = 1.0f;

            byte[] image = new byte[stride * imageHeight];
            float valueF;
            byte value;

            for (int i = 0; i < Data.Count; i++)
            {
                for (int j = 0; j < BlockSize; j++)
                {
                    valueF = scale * Data[i][j];
                    if (valueF > 1.0f) valueF = 1.0f;
                    if (valueF < 0.0f) valueF = 0.0f;

                    value = (byte)(valueF * 255.0f);

                    for (int k = 0; k < BlockSize; k++)
                    {
                        image[(j * stride) + (BlockSize * i) + k] = value;
                    }
                }
            }

            BitmapSource specGraph = BitmapSource.Create(imageWidth, imageHeight, 120,120,PixelFormats.Gray8, null, image, stride);
            imgSpectrum.Source = specGraph;
        }
    }
}
