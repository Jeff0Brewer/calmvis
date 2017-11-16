using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Media;
using System.Windows.Threading;
using Tobii.EyeX.Framework;
using EyeXFramework;

namespace calmvis
{
    public partial class MainWindow : Window
    {
        EyeXHost host;
        Point gaze = new Point();
        Point curr = new Point();
        double smoothing = .95;

        Blob blob;
        int length = 10;
        double size = 50;
        double decay = .9;

        public MainWindow()
        {
            this.DataContext = this;

            blob = new Blob(length, size, decay);

            InitializeComponent();
        }

        private void init(object sender, RoutedEventArgs e)
        {
            host = new EyeXHost();
            host.Start();
            var gazeData = host.CreateGazePointDataStream(GazePointDataMode.LightlyFiltered);
            //gazeData.Next += newPoint;
            c.PreviewMouseMove += mouseTesting;

            DispatcherTimer timer = new DispatcherTimer();
            timer.Interval = new TimeSpan(0, 0, 0, 0, 10);
            timer.Tick += tick;
            timer.Start();
        }

        private void tick(object sender, EventArgs e) {
            //Point fromScreen = PointFromScreen(gaze);
            Point fromScreen = gaze;
            curr.X = curr.X * smoothing + fromScreen.X * (1 - smoothing);
            curr.Y = curr.Y * smoothing + fromScreen.Y * (1 - smoothing);
            blob.next(curr);
        }

        private class Blob {
            private int length;
            private double size;
            private double decay;
            private EllipseGeometry[] shapes;
            private double[] goal;
            private int lastShape;
            public PathGeometry path;

            public Blob(int l, double s, double d) {
                length = l;
                size = s;
                decay = d;

                shapes = new EllipseGeometry[length];
                goal = new double[length];
                for (int i = 0; i < length; i++) {
                    shapes[i] = new EllipseGeometry(new Point(i*size*2,0),size,size);
                    goal[i] = -1;
                }
                lastShape = 0;
                path = new PathGeometry();
                path.FillRule = FillRule.Nonzero;
            }

            public void next(Point p){
                for (int i = 0; i < length; i++){
                    if (goal[i] != -1) {
                        double rad = shapes[i].RadiusX * decay + goal[i] * (1 - decay);
                        if (Math.Abs(goal[i] - rad) < .05) {
                            rad = goal[i];
                            goal[i] = -1;
                        }
                        shapes[i].RadiusX = rad;
                        shapes[i].RadiusY = rad;
                    }
                }
                if (!path.FillContains(p)){
                    shapes[lastShape] = new EllipseGeometry(p, 0, 0);
                    goal[lastShape] = size;
                    lastShape = (lastShape + 1) % length;
                    goal[lastShape] = 0;
                }
                path.Clear();
                for (int i = 0; i < length; i++)
                    path.AddGeometry(shapes[i]);
            }
        }

        private void newPoint(object s, EyeXFramework.GazePointEventArgs e)
        {
            gaze.X = e.X;
            gaze.Y = e.Y;
        }

        private void mouseTesting(object s, MouseEventArgs args)
        {
            Point e = args.GetPosition(c);
            gaze.X = e.X;
            gaze.Y = e.Y;
        }

        private void onClose(object s, EventArgs e)
        {
            host.Dispose();
        }

        public PathGeometry Geometry {
            get {
                return blob.path;
            }
        }
    }
}
