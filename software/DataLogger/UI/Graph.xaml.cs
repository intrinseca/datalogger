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
using System.ComponentModel;
using System.Collections.Specialized;

namespace DataLogger
{
    /// <summary>
    /// Interaction logic for Graph.xaml
    /// </summary>

    public partial class Graph : UserControl
    {
        /// <summary>
        /// The 'width' of each sample, in pixels
        /// </summary>
        public float Timebase = 1;

        /// <summary>
        /// The number of pixels per unit value vertically
        /// </summary>
        public float Yscale = 1.0f;

        public ObservableCollection<short> Data
        {
            get { return (ObservableCollection<short>)GetValue(DataProperty); }
            set { SetValue(DataProperty, value); }
        }

        public static readonly DependencyProperty DataProperty =
            DependencyProperty.Register(
            "Data",
            typeof(ObservableCollection<short>),
            typeof(Graph),
            new UIPropertyMetadata(null, new PropertyChangedCallback(dataChanged)));

        public Graph()
        {
            InitializeComponent();

            Data = new ObservableCollection<short>();
            Data.Add(0);
        }

        private static void dataChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var g = (Graph)sender;

            g.Data.CollectionChanged += new NotifyCollectionChangedEventHandler(g.Data_CollectionChanged);
        }

        void Data_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                int t = e.NewStartingIndex;

                foreach (short item in e.NewItems)
                {
                    AddPoint(t, item, true);
                    t++;
                }
            }
        }

        public void AddPoint(int t, short y, bool trim = true)
        {
            plGraph.Points.Add(new Point(
                t * Timebase,
                y * Yscale + grdPoints.ActualHeight / 2.0
                ));

            if (trim && plGraph.Points.Count > 1000)
                plGraph.Points.RemoveAt(0);

            scrGraph.ScrollToRightEnd();
        }

        public void ClearPoints()
        {
            plGraph.Points.Clear();
        }
    }
}
