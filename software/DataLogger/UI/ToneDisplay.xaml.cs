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
        public int BlockSize
        {
            get { return (int)GetValue(BlockSizeProperty); }
            set { SetValue(BlockSizeProperty, value); }
        }

        // Using a DependencyProperty as the backing store for BlockSize.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty BlockSizeProperty =
            DependencyProperty.Register("BlockSize", typeof(int), typeof(ToneDisplay), new UIPropertyMetadata(0));
              
        public float Timebase
        {
            get { return (float)GetValue(TimebaseProperty); }
            set { SetValue(TimebaseProperty, value); }
        }

        public static readonly DependencyProperty TimebaseProperty =
            DependencyProperty.Register("Timebase", typeof(float), typeof(ToneDisplay), new UIPropertyMetadata(100f, new PropertyChangedCallback(timeBaseChanged)));
              
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

        public event ScrollChangedEventHandler ScrollChanged;

        public ToneDisplay()
        {
            InitializeComponent();

            Tones = new ObservableCollection<Tone>();
        }

        private static void timeBaseChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var t = (ToneDisplay)sender;
            t.refresh();
        }

        private static void tonesChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var t = (ToneDisplay)sender;
            if(t.Tones != null)
                t.Tones.CollectionChanged += new NotifyCollectionChangedEventHandler(t.Tones_CollectionChanged);
        }

        private void Tones_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            refresh();
        }

        private void refresh()
        {
            if (Tones == null)
                return;

            //int width = (int)Timebase;
            double width = Timebase * BlockSize;

            grdTones.Children.Clear();

            foreach (var tone in Tones)
            {
                var b = new Border();

                var t = new TextBlock();
                t.Text = tone.KeyString;

                b.Margin = new Thickness(width * tone.StartBlock, 0, 0, 0);

                var binding = new Binding("Duration");
                binding.Source = tone;
                binding.Converter = new MultiplyConverter();
                binding.ConverterParameter = width;
                b.SetBinding(Border.WidthProperty, binding);

                b.Child = t;
                grdTones.Children.Add(b);
            }
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
