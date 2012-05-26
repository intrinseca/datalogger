﻿using System;
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
        /// <param name="value">Input value</param>
        /// <returns>Three-byte array of R G B values</returns>
        private byte[] mapRainbowColor(float value, bool invert = false)
        {
            // Convert into a value between 0 and 1023.
            int scaledValue = (int)(1023 * value);

            //Flip if required
            if(invert)
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

        /// <summary>
        /// Generate the spectogram
        /// </summary>
        public void Refresh()
        {
            if (Data.Count == 0)
                return;

            //Set horizontal scale
            int blockWidth = (int)(BlockSize * Timebase);

            //Calculate dimensions
            int imageHeight = BlockSize / 2;
            int imageWidth = Data.Count * blockWidth;

            int stride = imageWidth * 3;

            //Default magnitude scaling
            float scale = 1.0f;

            byte[] image = new byte[stride * imageHeight];
            float valueF;

            //for each FFT result
            for (int i = 0; i < Data.Count; i++)
            {
                //Scale block to its maximum magnitude
                scale = 1.0f / Data[i].Max();

                //for each frequency component
                for (int j = 0; j < imageHeight; j++)
                {
                    //scale and clip the value
                    valueF = scale * Data[i][j];
                    if (valueF > 1.0f) valueF = 1.0f;
                    if (valueF < 0.0f) valueF = 0.0f;

                    //calculate colour and set pixel values
                    byte[] color = mapRainbowColor(valueF, true);

                    for (int k = 0; k < blockWidth; k++)
                    {

                        image[(j * stride) + (blockWidth * i * 3) + k * 3] = color[0];
                        image[(j * stride) + (blockWidth * i * 3) + k * 3 + 1] = color[1];
                        image[(j * stride) + (blockWidth * i * 3) + k * 3 + 2] = color[2];
                    }
                }
            }

            //load pixel data into image
            BitmapSource specGraph = BitmapSource.Create(imageWidth, imageHeight, 120, 120, PixelFormats.Rgb24, null, image, stride);
            imgSpectrum.Source = specGraph;
        }
    }
}