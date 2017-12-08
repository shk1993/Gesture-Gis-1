using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;


namespace GestureGis2
{
    /// <summary>
    /// Interaction logic for TrainingDockView.xaml
    /// </summary>
    public partial class TrainingDockView : UserControl
    {
        Point currentPoint = new Point();
        String attrVal = "";
        Dictionary<String, List<List<Point>>> gestureDict = new Dictionary<string, List<List<Point>>>();
        //List<List<List<Point>>> gestureSet = new List<List<List<Point>>>();
        List<List<Point>> gesturePoints = new List<List<Point>>();
        List<Point> gesture = new List<Point>();
        ArrayList classArray = new ArrayList();
        string projectDirectory = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.FullName;
        public TrainingDockView()
        {
            InitializeComponent();
        }

        private void trainPad_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                gesture.Add(e.GetPosition(this));
                currentPoint = e.GetPosition(this);
            }
               
        }

        private void trainPad_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                Line line = new Line();
                gesture.Add(e.GetPosition(this));
                line.Stroke = SystemColors.ControlDarkDarkBrush;
                line.X1 = currentPoint.X;
                line.Y1 = currentPoint.Y;
                line.X2 = e.GetPosition(this).X;
                line.Y2 = e.GetPosition(this).Y;

                currentPoint = e.GetPosition(this);

                trainPad.Children.Add(line);
            }
        }

        private void Button_AddNewExample(object sender, RoutedEventArgs e)
        {
            //ToDo: Add error check if no sketch is drawn as an example
            if (gesture.Count > 0)
                if (gesture.Count > 0 && gestureDict.Count > 0 && attrVal != "")
                {
                    gesturePoints.Add(gesture);
                    CalculateAllFeatures(gesture, attrVal);
                    gesture = new List<Point>();
                    gestureDict[attrVal] = gesturePoints;
                    trainPad.Children.Clear();
                }
                else
                {
                    if (attrVal == "")
                        MessageBox.Show("Please add a new gesture first by providing an attribute value and clicking on the new gesture button");
                    else
                        MessageBox.Show("Please draw the example gesture on the sketch pad first.");
                }

        }

        private void Button_AddNewGesture(object sender, RoutedEventArgs e)
        {
            attrVal= AttributeVal.Text;
            //ToDo: Add error check if no examples have been added to the new gesture
            if (attrVal != "" && !gestureDict.ContainsKey(attrVal))
            {
                gesture = new List<Point>();
                gesturePoints = new List<List<Point>>();
                gestureDict.Add(attrVal, gesturePoints);
                trainPad.Children.Clear();
            }
            else
            {
                if (attrVal == "")
                    MessageBox.Show("Please add an attribute value in the text box that you want to identify your gesture with.");
                else
                    MessageBox.Show("This attribute is already associated with another gesture.");
            }
        }

        private void Button_Clear(object sender, RoutedEventArgs e)
        {
            gesture = new List<Point>();
            trainPad.Children.Clear();
        }

        private void CalculateAllFeatures(List<Point> sketch, String BuildingType)
        {
            SketchProcessor sketchProcessor = new SketchProcessor();
            List<Point> SingleGesturePoints = sketchProcessor.getResampledPoints(sketch);

            //F1, F2 cos  and sin start angle
            // if (!(SingleGesturePoints.Count < 3))
            //{
            Double xdiff = SingleGesturePoints[2].X - SingleGesturePoints[0].X;
            Double ydiff = SingleGesturePoints[2].Y - SingleGesturePoints[0].Y;
            Double hypot = Math.Sqrt(Math.Pow(xdiff, 2) + Math.Pow(ydiff, 2));
            Double F1 = xdiff / hypot;
            Double F2 = ydiff / hypot;
            //}
            //F1, F2 cos  and sin start angle
            System.Diagnostics.Debug.WriteLine("xdiff " + xdiff + " " + "hypot " + hypot);

            //F3 dist first and last
            var lastIndex = SingleGesturePoints.Count - 1;
            Double F3 = Math.Sqrt(Math.Pow((SingleGesturePoints[lastIndex].X - SingleGesturePoints[0].X), 2) + Math.Pow((SingleGesturePoints[lastIndex].Y - SingleGesturePoints[0].Y), 2));
            //F3 dist first and last

            //F4 cos angle first last
            Double F4 = (SingleGesturePoints[SingleGesturePoints.Count - 1].X - SingleGesturePoints[0].X) / F3;
            //F4 cos angle first last

            // F5 sin angle first last
            Double F5 = (SingleGesturePoints[SingleGesturePoints.Count - 1].Y - SingleGesturePoints[0].Y) / F3;
            // F5 sin angle first last

            //F6 stroke length
            Double sum = 0;
            for (var i = 1; i < SingleGesturePoints.Count; i++)
            {
                sum = sum + Math.Sqrt(Math.Pow((SingleGesturePoints[i].X - SingleGesturePoints[i - 1].X), 2) + Math.Pow((SingleGesturePoints[i].Y - SingleGesturePoints[i - 1].Y), 2));
            }
            Double F6 = sum;
            //F6 stroke Length

            //F7, F8, F9 All angles
            Double totalAngle = 0;
            Double squaredAngle = 0;
            Double modAngle = 0;
            Double theta = 0;
            for (var i = 1; i < SingleGesturePoints.Count - 1; i++)
            {
                var numerator = (SingleGesturePoints[i].X * SingleGesturePoints[i - 1].Y) - (SingleGesturePoints[i - 1].X * SingleGesturePoints[i].Y);
                var denominator = (SingleGesturePoints[i].X * SingleGesturePoints[i - 1].Y) + (SingleGesturePoints[i - 1].Y * SingleGesturePoints[i].Y);
                if (denominator != 0)
                {
                    theta = Math.Atan(numerator / denominator);
                }
                else
                {
                    if (numerator < 0)
                        theta = Math.PI * 3 / 2;
                    else
                        theta = Math.PI / 2;
                }
                //not handling the case where numerator = 0
                totalAngle = totalAngle + theta;
                squaredAngle = squaredAngle + Math.Pow(theta, 2);
                modAngle = modAngle + Math.Abs(theta);

            }
            Double F7 = totalAngle;
            Double F8 = squaredAngle;
            Double F9 = modAngle;
            //F7, F8, F9 All angles

            //F10 Length of bounding box
            Double xmax = 0;
            Double ymax = 0;
            Double xmin = 0;
            Double ymin = 0;
            for (var i = 0; i < SingleGesturePoints.Count; i++)
            {
                if (xmax == 0 || xmax < SingleGesturePoints[i].X)
                    xmax = SingleGesturePoints[i].X;
                if (xmin == 0 || xmin > SingleGesturePoints[i].X)
                    xmin = SingleGesturePoints[i].X;
                if (ymax == 0 || ymax < SingleGesturePoints[i].Y)
                    ymax = SingleGesturePoints[i].Y;
                if (ymin == 0 || ymin > SingleGesturePoints[i].Y)
                    ymin = SingleGesturePoints[i].Y;

            }
            Double F10 = Math.Sqrt(Math.Pow(xmax - xmin, 2) + Math.Pow(ymax - ymin, 2));
            //F10 Length of bounding box

            //F11 angle of bounding box
            Double F11 = Math.Atan((ymax - ymin) / (xmax - xmin));
            //F11 angle of bounding box

            //Create/append features and building type to a txt file
            
            string mypath = @projectDirectory+"\\Featurefile.txt";
            string inp = "interpretation";
            //FileStream fs = null;
            if (!File.Exists(mypath))
            {
                var myFile = File.Create(mypath);
                myFile.Close();
                TextWriter tw = new StreamWriter(mypath);
                //tw.WriteLine("  ");
                tw.Write(1.0); tw.Write(","); tw.Write(2.0); tw.Write(","); tw.Write(3.0); tw.Write(","); tw.Write(4.0); tw.Write(","); tw.Write(5.0); tw.Write(","); tw.Write(6.0); tw.Write(","); tw.Write(7.0); tw.Write(","); tw.Write(8.0);
                tw.Write(","); tw.Write(9.0); tw.Write(","); tw.Write(10.0); tw.Write(","); tw.Write(11.0); tw.Write(","); tw.Write(inp+Environment.NewLine);
                //tw.WriteLine(" ");
                tw.Write(F1); tw.Write(","); tw.Write(F2); tw.Write(","); tw.Write(F3); tw.Write(","); tw.Write(F4); tw.Write(",");
                tw.Write(F5); tw.Write(","); tw.Write(F6); tw.Write(","); tw.Write(F7); tw.Write(","); tw.Write(F8); tw.Write(",");
                tw.Write(F9); tw.Write(","); tw.Write(F10); tw.Write(","); tw.Write(F11); tw.Write(","); tw.Write(BuildingType+Environment.NewLine);
                tw.Close();
                // tw.WriteLine(BuildingType);
                //tw.Close();
            }
            else if (File.Exists(mypath))
            {
                using (var tw = new StreamWriter(mypath, true))
                {
                    //tw.WriteLine(" ");
                    tw.Write(F1); tw.Write(","); tw.Write(F2); tw.Write(","); tw.Write(F3); tw.Write(","); tw.Write(F4); tw.Write(",");
                    tw.Write(F5); tw.Write(","); tw.Write(F6); tw.Write(","); tw.Write(F7); tw.Write(","); tw.Write(F8); tw.Write(",");
                    tw.Write(F9); tw.Write(","); tw.Write(F10); tw.Write(","); tw.Write(F11); tw.Write(","); tw.Write(BuildingType+Environment.NewLine);
                    tw.Close();
                }
            }
        }


    }
}
