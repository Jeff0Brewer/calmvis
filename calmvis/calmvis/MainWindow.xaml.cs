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
        
        Cluster cluster;
        double size = 50;
        double decay = .7;
        double smoothing = .6;

        public MainWindow()
        {
            this.DataContext = this;
            
            cluster = new Cluster(size, decay, smoothing);

            InitializeComponent();
        }

        private void init(object sender, RoutedEventArgs e)
        {
            ImageBrush ib = new ImageBrush();
            ib.ImageSource = new BitmapImage(new Uri(@"txtBg.jpg", UriKind.Relative));
            c.Background = ib;

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
            //cluster.next(PointFromScreen(gaze));
            cluster.next(gaze);
        }

        public class Cluster {
            public PathGeometry path;
            private Dot start;
            private double size;
            private double decay, smoothing;
            private Point prev;
            private double speed;

            public Cluster(double sz, double d, double sm){
                path = new PathGeometry();
                path.FillRule = FillRule.Nonzero;

                start = null;
                size = sz;
                decay = d;
                smoothing = sm;
                
                prev = new Point();
                speed = 0;
            }

            public void next(Point p) {
                p.X = prev.X * smoothing + p.X * (1 - smoothing);
                p.Y = prev.Y * smoothing + p.Y * (1 - smoothing);

                speed = speed * .2 + distance(p, prev) * .8;
                prev = p;

                Dot dot;
                if (!path.FillContains(p) && speed < 5) {
                    dot = new Dot(p, decay, start);
                    if (start != null)
                        start.prev = dot;
                    start = dot;
                }
                
                dot = start;
                path.Clear();
                while (dot != null)
                {
                    double dist = distance(p, dot.center);
                    double rad = dot.update(size - dist/5);
                    if (rad <= 0) {
                        if (dot.prev != null && dot.next != null)
                        {
                            dot.prev.next = dot.next;
                            dot.next.prev = dot.prev;
                        }
                        else if (dot.prev != null)
                        {
                            dot.prev.next = null;
                        }
                        else if (dot.next != null)
                        {
                            dot.next.prev = null;
                            start = dot.next;
                        }
                        else
                        {
                            start = null;
                        }
                    }
                    else
                        path.AddGeometry(dot.shape);
                    dot = dot.next;
                }
            }

            private double distance(Point a, Point b)
            {
                return Math.Sqrt(Math.Pow(a.X - b.X, 2) + Math.Pow(a.Y - b.Y, 2));
            }
        }

        public class Dot {
            public EllipseGeometry shape;
            public Point center;
            public Dot next;
            public Dot prev;
            private double radius;
            private double decay;

            public Dot(Point c, double d) {
                center = c;
                decay = d;
                radius = 0;
                shape = new EllipseGeometry(center, radius, radius);
                next = null;
                prev = null;
            }

            public Dot(Point c, double d, Dot n) {
                center = c;
                decay = d;
                radius = 0;
                shape = new EllipseGeometry(center, radius, radius);
                next = n;
                prev = null;
            }

            public double update(double goal) {
                radius = radius * decay + goal * (1 - decay);
                shape.RadiusX = radius;
                shape.RadiusY = radius;
                return radius;
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

        public PathGeometry clusterPath {
            get {
                return cluster.path;
            }
        }

        private void onClose(object s, EventArgs e)
        {
            host.Dispose();
        }
    }
}
