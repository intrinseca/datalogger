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
using System.ComponentModel;

namespace DataLogger
{
    /// <summary>
    /// Interaction logic for Spectrum.xaml
    /// </summary>
    public partial class Spectrum : UserControl
    {
        /// The horizontal scale factor, pixels per sample
        /// </summary>
        public float Timebase
        {
            get { return (float)GetValue(TimebaseProperty); }
            set { SetValue(TimebaseProperty, value); }
        }

        public static readonly DependencyProperty TimebaseProperty =
            DependencyProperty.Register("Timebase", typeof(float), typeof(Spectrum), new UIPropertyMetadata(1.0f));

        /// The source AudioProcessor that manages playback and contains the audio data
        public AudioProcessor Audio
        {
            get { return (AudioProcessor)GetValue(AudioProperty); }
            set { SetValue(AudioProperty, value); }
        }

        public static readonly DependencyProperty AudioProperty =
            DependencyProperty.Register("Audio", typeof(AudioProcessor), typeof(Spectrum), new UIPropertyMetadata(null, new PropertyChangedCallback(audioChanged)));

        //Whether audio was paused by a click and should be restarted on mouse up
        bool restartOnMouseUp;

        //Whether the mouse was pressed over the control, and therefore mousemove events should be interpreted as dragging
        bool dragging;

        /// <summary>
        /// Raised when the internal ScrollViewer is scrolled
        /// </summary>
        public event ScrollChangedEventHandler ScrollChanged;

        /// <summary>
        /// Constructor
        /// </summary>
        public Spectrum()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Callback for when the source AudioProcessor is changed
        /// <see cref="Audio"/>
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void audioChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var s = (Spectrum)sender;

            if (s.Audio != null)
            {
                //If Audio has been set to a meaningful value, attach event handlers
                s.Audio.Spectrum.CollectionChanged += new NotifyCollectionChangedEventHandler(s.Spectrum_CollectionChanged);
                s.Audio.PropertyChanged += new PropertyChangedEventHandler(s.Audio_PropertyChanged);

                //Set height of spectrum now so it doesn't move when data comes in
                s.stkSpectrum.Height = s.Audio.BlockSize;
            }

        }

        /// <summary>
        /// Handles PropertyChanged events from the audio interface
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Audio_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "ChannelPosition":
                    //Channel position changed, move cursor
                    var position = Timebase * Audio.ChannelPosition * Audio.SamplingFrequency;
                    bdrCursor.Margin = new Thickness(position, 0, 0, 0);

                    //If cursor near egde, scroll along
                    if (position > (scroll.HorizontalOffset + scroll.ActualWidth - 100))
                    {
                        var newOffset = position - scroll.ActualWidth + 100;
                        scroll.ScrollToHorizontalOffset(newOffset);
                    }

                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// Handles changes in the source spectum data
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Spectrum_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            //TODO: Replace with switch
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                //Process any new slices
                addSlices(e.NewItems);
            }
            else if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                //Remove any deleted slices from the displayed spectrogram
                for (int i = 0; i < e.OldItems.Count; i++)
                {
                    stkSpectrum.Children.RemoveAt(e.OldStartingIndex);
                }
            }
            else if (e.Action == NotifyCollectionChangedAction.Reset)
            {
                //Reset: delete all existing slices and re-add
                stkSpectrum.Children.Clear();
                foreach (var slice in Audio.Spectrum)
                {
                    addSlices(slice);
                }
            }
            else
            {
                throw new InvalidOperationException("Operation not supported by spectrogram");
            }
        }

        /// <summary>
        /// Create image slices for FFT blocks, and add them to the spectrogram
        /// </summary>
        /// <param name="items">The FFT blocks to create slices for</param>
        private void addSlices(System.Collections.IList items)
        {
            foreach (float[] slice in items)
            {
                //Make a new image control
                var im = new Image();
                //Generate source image
                im.Source = generateSlice(slice);

                //Set layout
                im.Stretch = Stretch.Fill;
                im.Height = Audio.BlockSize;

                //Bind the width to the Timebase
                var b = new Binding("Timebase");
                b.Source = this;
                b.Converter = new MultiplyConverter();
                b.ConverterParameter = (double)Audio.BlockSize;
                im.SetBinding(Image.WidthProperty, b);
                //Add to spectogram
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

        /// <summary>
        /// Create a one pixel wide image for a given FFT result
        /// </summary>
        /// <param name="spectrum"></param>
        /// <returns></returns>
        public BitmapSource generateSlice(float[] spectrum)
        {
            //Calculate image dimensions. Only show positive (first half) of spectrum
            int imageHeight = spectrum.Length / 2;
            int imageWidth = 1;

            //Stride = width * bytes per pixel
            //bytes per pixel = 3 (RGB)
            int stride = imageWidth * 3;

            //Create array for image data
            byte[] image = new byte[stride * imageHeight];

            //Scaling factor to apply to frequency magnitudes
            float scale = 100.0f;

            //for each frequency component
            for (int j = 0; j < imageHeight; j++)
            {
                //scale and clip the value
                float scaledValue = 2.0f * (float)Math.Log10(scale * spectrum[j]);
                if (scaledValue > 1.0f) scaledValue = 1.0f;
                if (scaledValue < 0.0f) scaledValue = 0.0f;

                //calculate colour and set pixel values
                byte[] color = mapRainbowColor(scaledValue, true);

                image[((imageHeight - 1 - j) * stride)] = color[0];
                image[((imageHeight - 1 - j) * stride) + 1] = color[1];
                image[((imageHeight - 1 - j) * stride) + 2] = color[2];
            }

            //load pixel data into image
            
            var source = BitmapSource.Create(imageWidth, imageHeight, 96, 96, PixelFormats.Rgb24, null, image, stride);
            source.Freeze();
            return source;
        }

        /// <summary>
        /// Helper to raise ScrollChanged
        /// </summary>
        /// <param name="e"></param>
        private void OnScrollChanged(ScrollChangedEventArgs e)
        {
            if (ScrollChanged != null)
                ScrollChanged(this, e);
        }

        /// <summary>
        /// Pass on scroll events from the main scrollviewer
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            OnScrollChanged(e);
        }

        /// <summary>
        /// Scroll to a particular location
        /// </summary>
        /// <param name="offset">The new horizontal offset</param>
        public void ScrollTo(double offset)
        {
            scroll.ScrollToHorizontalOffset(offset);
        }

        /// <summary>
        /// Handle mouse presses on the spectogram
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void grdSpectrum_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            //Move the cursor to the new location
            repositionCursor(e);

            if (Audio.IsPlaying)
            {
                //If audio is playing, pause it
                Audio.Pause();
                restartOnMouseUp = true;
            }

            //Set flag so cursor will click and drag
            dragging = true;
        }

        /// <summary>
        /// Move the cursor to sit under the point described in <paramref name="e"/>
        /// </summary>
        /// <param name="e">MouseEventArgs from the mouse event that triggered the reposition</param>
        private void repositionCursor(MouseEventArgs e)
        {
            //Get the relative position
            var position = e.GetPosition(grdSpectrum);
            //Convert to a time and update source audio processor
            var time = position.X / (Timebase * Audio.SamplingFrequency);
            Audio.ChannelPosition = time;
        }

        /// <summary>
        /// Handle mouse releases on the spectogram
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void grdSpectrum_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            //Restart audio if it was paused
            if (restartOnMouseUp)
            {
                restartOnMouseUp = false;
                Audio.Play();
            }

            //Clear dragging flag
            dragging = false;
        }

        /// <summary>
        /// Handle mouse movement on the spectogram
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void grdSpectrum_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && dragging)
            {
                //If the mouse is pressed, and was initially pressed over the spectogram, drag the cursor
                repositionCursor(e);
            }
        }
    }
}
