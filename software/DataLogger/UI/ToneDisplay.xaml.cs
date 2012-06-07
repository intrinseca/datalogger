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
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace DataLogger
{
    /// <summary>
    /// Interaction logic for ToneDisplay.xaml
    /// </summary>
    public partial class ToneDisplay : UserControl
    {
        /// <summary>
        /// The number of samples represented by a unit tone duration, usually the FFT block size
        /// </summary>
        public int BlockSize
        {
            get { return (int)GetValue(BlockSizeProperty); }
            set { SetValue(BlockSizeProperty, value); }
        }

        public static readonly DependencyProperty BlockSizeProperty =
            DependencyProperty.Register("BlockSize", typeof(int), typeof(ToneDisplay), new UIPropertyMetadata(0));

        /// <summary>
        /// The width of a sample, in pixels/sample
        /// </summary>
        public float Timebase
        {
            get { return (float)GetValue(TimebaseProperty); }
            set { SetValue(TimebaseProperty, value); }
        }

        public static readonly DependencyProperty TimebaseProperty =
            DependencyProperty.Register("Timebase", typeof(float), typeof(ToneDisplay), new UIPropertyMetadata(100f, new PropertyChangedCallback(timeBaseChanged)));

        /// <summary>
        /// The collection of tones that have been detected
        /// </summary>
        public ObservableCollection<Tone> Tones
        {
            get { return (ObservableCollection<Tone>)GetValue(TonesProperty); }
            set { SetValue(TonesProperty, value); }
        }

        public static readonly DependencyProperty TonesProperty =
            DependencyProperty.Register(
            "Tones",
            typeof(ObservableCollection<Tone>),
            typeof(ToneDisplay),
            new UIPropertyMetadata(null, new PropertyChangedCallback(tonesChanged)));

        /// <summary>
        /// The last block that was analysed, whether any tone was detected or not. Used to set width of whole display
        /// </summary>
        public int LastBlock
        {
            get { return (int)GetValue(LastBlockProperty); }
            set { SetValue(LastBlockProperty, value); }
        }

        public static readonly DependencyProperty LastBlockProperty =
            DependencyProperty.Register(
            "LastBlock",
            typeof(int),
            typeof(ToneDisplay),
            new UIPropertyMetadata(0, new PropertyChangedCallback(lastBlockChanged)));

        /// <summary>
        /// Raised when the internal ScrollViewer is scrolled
        /// </summary>
        public event ScrollChangedEventHandler ScrollChanged;

        //Store a reference to the padding border to allow it to be modified
        Border paddingBorder;

        /// <summary>
        /// Constructor
        /// </summary>
        public ToneDisplay()
        {
            InitializeComponent();

            Tones = new ObservableCollection<Tone>();
        }

        /// <summary>
        /// Handle changes to the Timebase, by regenerating the tone display
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void timeBaseChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var t = (ToneDisplay)sender;
            t.refresh();
        }

        /// <summary>
        /// Attaches event handlers if the tone collection is replaced
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void tonesChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var t = (ToneDisplay)sender;
            if (t.Tones != null)
                t.Tones.CollectionChanged += new NotifyCollectionChangedEventHandler(t.Tones_CollectionChanged);
        }

        /// <summary>
        /// Update the display padding if the number of FFT blocks changes
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void lastBlockChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var t = (ToneDisplay)sender;
            if (t.Tones != null)
                t.padDisplay();
        }

        /// <summary>
        /// Handle changes to the tone collection, by regenerating the tone display
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Tones_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            refresh();
        }

        /// <summary>
        /// Regenerate tone display
        /// </summary>
        private void refresh()
        {
            //Stop if we have the tone collection is not set
            if (Tones == null)
                return;

            //Clear the tone container
            grdTones.Children.Clear();

            //Stop here if there are no tones to display
            if (Tones.Count == 0)
                return;

            //Calculate the width of pixels that is one unit tone duration
            double width = getBlockWidth();

            //Add each tone
            foreach (var tone in Tones)
            {
                //Create the tone border
                var b = new Border();

                //Create the textblock to hold the tone string
                var t = new TextBlock();
                t.Text = tone.KeyString;

                //Position the tone
                b.Margin = new Thickness(width * tone.StartBlock, 0, 0, 0);

                //Bind the tone width
                var binding = new Binding("Duration");
                binding.Source = tone;
                binding.Converter = new MultiplyConverter();
                binding.ConverterParameter = width;
                b.SetBinding(Border.WidthProperty, binding);

                //Add the tone to the display
                b.Child = t;
                grdTones.Children.Add(b);
            }

            padDisplay();
        }

        /// <summary>
        /// Get the width of a single FFT block
        /// </summary>
        /// <returns></returns>
        private double getBlockWidth()
        {
            double width = Timebase * BlockSize;
            return width;
        }

        /// <summary>
        /// Add a padding tone to the end of the display to match the spectrogram
        /// </summary>
        /// <param name="width"></param>
        private void padDisplay()
        {
            if (Tones.Count == 0)
                return;

            double width = getBlockWidth();

            var lastTone = Tones[Tones.Count - 1];
            var lastToneEnd = lastTone.StartBlock + lastTone.Duration;
            if (lastToneEnd < LastBlock)
            {
                grdTones.Children.Remove(paddingBorder);

                paddingBorder = new Border();
                paddingBorder.Background = (Brush)FindResource("LightBrush");
                paddingBorder.Width = (LastBlock - lastToneEnd) * width;
                paddingBorder.Margin = new Thickness(width * lastToneEnd, 0, 0, 0);

                grdTones.Children.Add(paddingBorder);
            }
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
    }
}
