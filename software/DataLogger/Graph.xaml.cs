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

namespace DataLogger
{
    /// <summary>
    /// Interaction logic for Graph.xaml
    /// </summary>
    /// 

    public class DataPoint
    {
        public double t;
        public double y;

        public DataPoint(float _t, float _y)
        {
            t = _t;
            y = _y;
        }
    }

    public partial class Graph : UserControl
    {
        public float Timebase = 1;
        public float Yscale = 75;

        private ObservableCollection<DataPoint> Data
        {
            get { return (ObservableCollection<DataPoint>)GetValue(DataProperty); }
            set { SetValue(DataProperty, value); }
        }

        public static readonly DependencyProperty DataProperty =
            DependencyProperty.Register("Data", typeof(ObservableCollection<DataPoint>), typeof(Graph), new UIPropertyMetadata(null));

        public Graph()
        {
            InitializeComponent();

            Data = new ObservableCollection<DataPoint>();
            Data.Add(new DataPoint(0, 0));
        }

        public void AddPoint(float t, float y, bool trim = true)
        {
            Yscale = (float) grdPoints.ActualHeight;
            Data.Add(new DataPoint(t, y));

            plGraph.Points.Add(new Point(
                t * Timebase,
                y * Yscale + grdPoints.ActualHeight / 2.0
                ));

            if (trim && plGraph.Points.Count > 500)
                plGraph.Points.RemoveAt(0);

            scrGraph.ScrollToRightEnd();
        }

        public void ClearPoints()
        {
            plGraph.Points.Clear();
            Data.Clear();
        }
    }
}
