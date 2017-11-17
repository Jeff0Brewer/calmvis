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
        double smoothing = .9;

        Blob blob;
        int length = 7;
        double size = 50;
        double decay = .6;

        public MainWindow()
        {
            this.DataContext = this;

            blob = new Blob(length, size, decay);

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
            //Point fromScreen = PointFromScreen(gaze);
            Point fromScreen = gaze;
            curr.X = curr.X * smoothing + fromScreen.X * (1 - smoothing);
            curr.Y = curr.Y * smoothing + fromScreen.Y * (1 - smoothing);
            blob.next(curr);
        }

        private class Blob {
            private int length, currlength;
            private double size;
            private double decay;
            private bool frozen;
            private double speed;
            private Point prev;
            private int moveCount;
            private Dot start, end;
            private Canvas c;
            public PathGeometry path;

            public Blob(int l, double s, double d) {
                length = l;
                currlength = 1;
                size = s;
                decay = d;

                start = new Dot(new Point(0, 0), size, decay);
                end = start;

                speed = 0;
                prev = new Point();
                frozen = true;
                moveCount = 0;

                path = new PathGeometry();
                path.FillRule = FillRule.Nonzero;
            }

            private double distance(Point a, Point b)
            {
                return Math.Sqrt(Math.Pow(a.X - b.X, 2) + Math.Pow(a.Y - b.Y, 2));
            }

            private void clear() {
                frozen = true;
                moveCount = 0;
                Dot curr = start.next;
                while (curr != null) {
                    curr.setGoal(0);
                    curr = curr.next;
                }
                start.setGoal(1);
                end = start;
                currlength = 1;
            }

            public void next(Point p){
                speed = speed * .8 + distance(prev, p) * .2;
                prev = p;

                Dot curr;
                if (!path.FillContains(p) && !frozen) //if point is outside of current area
                {
                    moveCount++;

                    //Add new dot
                    curr = new Dot(p, size, decay, start);
                    start.prev = curr;
                    start = curr;
                    currlength++;

                    //Remove last dot if max length exceeded
                    if (currlength > length)
                    {
                        end.setGoal(0);
                        end = end.prev;
                        currlength--;
                    }

                    if (moveCount > length/2)
                        clear();
                }
                else
                    moveCount = 0;

                frozen = frozen && speed > 5;
                
                curr = start;
                path.Clear();
                while (curr != null)
                {
                    double check = curr.update();
                    if (check == -1)
                        curr.prev.next = null;
                    else
                        path.AddGeometry(curr.shape); 
                    curr = curr.next;
                }
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

        public class Dot{
            public EllipseGeometry shape;
            private double size, curr, goal;
            private double decay;
            private Point center;
            public Dot next, prev;

            public Dot(Point c, double s, double d, Dot n) {
                curr = 0;
                size = s;
                goal = s;
                decay = d;
                center = c;
                next = n;
                prev = null;
                shape = new EllipseGeometry(center, curr, curr);
            }

            public Dot(Point center, double s, double d)
            {
                curr = 0;
                goal = s;
                decay = d;
                next = null;
                prev = null;
                shape = new EllipseGeometry(center, curr, curr);
            }

            public double update() {
                curr = curr * decay + goal * (1 - decay);
                shape.RadiusX = curr;
                shape.RadiusY = curr;
                if (Math.Abs(curr - goal) < .1 && goal == 0)
                    return -1;
                return 0;
            }

            public void setPrev(Dot p) {
                prev = p;
            }

            public void setGoal(double g) {
                goal = g;
            }
        }
    }
}
