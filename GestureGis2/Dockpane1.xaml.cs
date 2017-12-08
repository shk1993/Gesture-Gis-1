using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using System.Threading;
using System.Windows;
using System.Text;
using System.Windows.Controls;
using System.Windows.Input;
using ArcGIS.Core.Data;
using ArcGIS.Desktop.Core.Geoprocessing;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Framework.Dialogs;
using System.Windows.Shapes;
using ArcGIS.Desktop.Editing.Attributes;
using ArcGIS.Desktop.Mapping;
using ArcGIS.Desktop.Editing;
using System.Collections;

namespace GestureGis2
{
    /// <summary>
    /// Interaction logic for Dockpane1View.xaml
    /// </summary>
    public partial class Dockpane1View : UserControl
    {
        Point currentPoint = new Point();
        //this attributeIndex value is used to make sure that all the ids are unique
        int attributeIndex = 0;
        List<Point> gesture = new List<Point>();
        string projectDirectory = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.FullName;
        string PredictedClassbyWeka;
        public Dockpane1View()
        {
            InitializeComponent();
        }

        private void sketchPad_MouseMove(object sender, MouseEventArgs e)
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

                sketchPad.Children.Add(line);
            }
        }

        private void sketchPad_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                gesture.Add(e.GetPosition(this));
                currentPoint = e.GetPosition(this);
            }

        }

        private void ClearPad(object sender, RoutedEventArgs e)
        {
            sketchPad.Children.Clear();
            gesture = new List<Point>();
        }

        protected async void Button_Click(object sender, RoutedEventArgs e)
        {
            if (gesture.Count == 0 || gesture == null)
            {
                ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show("No gesture drawn");
                return;
            }
            CalculateAllFeatures(gesture, "Residential");
            gesture = new List<Point>();
            sketchPad.Children.Clear();

            try
            {
                BasicFeatureLayer layer = null;
                List<Inspector> inspList = new List<Inspector>();
                Inspector inspector = null;
                await ArcGIS.Desktop.Framework.Threading.Tasks.QueuedTask.Run(() =>
                {
                    //find selected layer
                    if (MapView.Active.GetSelectedLayers().Count == 0)
                    {
                        //MessageBox.Show("Select a feature class from the Content 'Table of Content' first.");
                        return;
                    }
                    layer = MapView.Active.GetSelectedLayers()[0] as BasicFeatureLayer;
                    
                    // get selected features from the map
                    var features = layer.GetSelection();
                    if (features.GetCount() == 0)
                    {
                        return;
                        /*ToDo : add error msg: no feature selected*/
                    }

                    // get ids of all the selected features
                    var featOids = features.GetObjectIDs();
                    if (featOids.Count == 0)
                    {/* ToDo : something is wrong*/}
                    
                    // adding the inspectors to a list so that separate id values can be assigned later
                    for (int i = 0; i < featOids.Count; i++)
                    {
                        var insp = new Inspector();
                        insp.Load(layer, featOids.ElementAt(i));
                        inspList.Add(insp);
                    }
                    inspector = new Inspector();
                    inspector.Load(layer, featOids);

                });
                if (layer == null) { }
                //MessageBox.Show("Unable to find a feature class at the first layer of the active map");
                else
                {
                    //update the attributes of those features

                    // make sure tha attribute exists
                    ArcGIS.Desktop.Editing.Attributes.Attribute att = inspector.FirstOrDefault(a => a.FieldName == "UID");
                    if (att == null)
                    {
                        // if the attribute doesn't exist we create a new field
                        var dataSource = await GetDataSource(layer);
                        //MessageBox.Show($@"{dataSource} was found ... adding a new Field");
                        await
                            ExecuteAddFieldTool(layer, new KeyValuePair<string, string>("UID", "uniqueId"), "Text", 50);
                    }

                    await ArcGIS.Desktop.Framework.Threading.Tasks.QueuedTask.Run(() =>
                    {
                        // we add values of ids to the selected features
                        for(int i = 0; i < inspList.Count; i++)
                        {
                            //if(inspList.ElementAt(i)["UID"]==null )//|| (String)inspList.ElementAt(i)["UID"] == String.Empty)
                            //{
                                // putting a random string now, this should be replaced by the tag user puts in after the recognition part is done.
                                inspList.ElementAt(i)["UID"] = PredictedClassbyWeka + attributeIndex++;
                                var op = new EditOperation();
                                op.Name = "Update";
                                op.SelectModifiedFeatures = true;
                                op.SelectNewFeatures = false;
                                op.Modify(inspList.ElementAt(i));
                                op.ExecuteAsync();
                            
                            //}
                            
                        }
                        
                    });

                }
            }
            catch (Exception ex)
            {
                //MessageBox.Show(ex.ToString());
            }
        }

        

        private async Task<string> GetDataSource(BasicFeatureLayer theLayer)
        {
            try
            {
                return await ArcGIS.Desktop.Framework.Threading.Tasks.QueuedTask.Run(() =>
                {
                    var inTable = theLayer.Name;
                    var table = theLayer.GetTable();
                    var dataStore = table.GetDatastore();
                    var workspaceNameDef = dataStore.GetConnectionString();
                    var workspaceName = workspaceNameDef.Split('=')[1];

                    var fullSpec = System.IO.Path.Combine(workspaceName, inTable);
                    return fullSpec;
                });
            }
            catch (Exception ex)
            {
                //MessageBox.Show(ex.ToString());
                return string.Empty;
            }
        }

        private async Task<bool> ExecuteAddFieldTool(BasicFeatureLayer theLayer, KeyValuePair<string, string> field, string fieldType, int? fieldLength = null, bool isNullable = true)
        {
            try
            {
                return await ArcGIS.Desktop.Framework.Threading.Tasks.QueuedTask.Run(() =>
                {
                    var inTable = theLayer.Name;
                    var table = theLayer.GetTable();
                    var dataStore = table.GetDatastore();
                    var workspaceNameDef = dataStore.GetConnectionString();
                    var workspaceName = workspaceNameDef.Split('=')[1];

                    var fullSpec = System.IO.Path.Combine(workspaceName, inTable);
                    System.Diagnostics.Debug.WriteLine($@"Add {field.Key} from {fullSpec}");

                    var parameters = Geoprocessing.MakeValueArray(fullSpec, field.Key, fieldType.ToUpper(), null, null,
                        fieldLength, field.Value, isNullable ? "NULABLE" : "NON_NULLABLE");
                    var cts = new CancellationTokenSource();
                    var results = Geoprocessing.ExecuteToolAsync("management.AddField", parameters, null, cts.Token,
                        (eventName, o) =>
                        {
                            System.Diagnostics.Debug.WriteLine($@"GP event: {eventName}");
                        });
                    return true;
                });
            }
            catch (Exception ex)
            {
                //MessageBox.Show(ex.ToString());
                return false;
            }
        }

#region Classification Code
        public void CSV2Arff()
        {

            //load csv
            weka.core.converters.CSVLoader loader = new weka.core.converters.CSVLoader();
            //weka.core.converters.TextDirectoryLoader loader = new weka.core.converters.TextDirectoryLoader();
            // C:/ Users / DELL / source / repos / WekafromCtest / WekafromCtest /
            loader.setSource(new java.io.File(projectDirectory+"//ComparisonFeaturefile.txt"));
            weka.core.Instances data = loader.getDataSet();

            //save arff
            weka.core.converters.ArffSaver saver = new weka.core.converters.ArffSaver();
            saver.setInstances(data);
            //and save as arff file
            saver.setFile(new java.io.File(projectDirectory+"//ComparisonFeaturefile.arff"));
            saver.writeBatch();

        }
        public void classifyTest()
        {
            try
            {

                CSV2Arff();
                java.io.FileReader arrfFile = new java.io.FileReader(projectDirectory+"//ComparisonFeaturefile.arff");
                weka.core.Instances insts = new weka.core.Instances(arrfFile);
                //weka.core.Instances insts2 = new weka.core.Instances(new java.io.FileReader("D:/Gesture-Gis-master/GestureGis2/ComparisonFeaturefile.arff"));
                insts.setClassIndex(insts.numAttributes() - 1);

                //int percentSplit = 66;

                weka.classifiers.Classifier cl = new weka.classifiers.trees.J48();
                //Console.WriteLine("Performing " + percentSplit + "% split evaluation.");

                //randomize the order of the instances in the dataset.
                //weka.filters.Filter myRandom = new weka.filters.unsupervised.instance.Randomize();
                //myRandom.setInputFormat(insts);
                //insts = weka.filters.Filter.useFilter(insts, myRandom);
                int count = insts.numInstances();
                int trainSize = count - 1;
                int testSize = count - trainSize;
                weka.core.Instances train = new weka.core.Instances(insts, 0, trainSize);

                cl.buildClassifier(train);
                //weka.core.Instance current = insts2.instance(0);
                int numCorrect = 0;
                /*for (int i = trainSize; i < insts.numInstances(); i++)
                {
                    weka.core.Instance currentInst = insts.instance(i);
                    double predictedClass = cl.classifyInstance(currentInst);
                    if (predictedClass == insts.instance(i).classValue())
                        numCorrect++;
                }*/
                int index = count - 1;
                weka.core.Instance currentInst = insts.instance(index);
                double predictedClass = cl.classifyInstance(currentInst);
                int pre = (int)predictedClass;
                if (predictedClass == insts.instance(index).classValue())
                    numCorrect++;
                //insts.instance(index).classAttribute();
                //insts.attribute(11);
                string s = insts.toString();
                s = s.Substring(s.IndexOf("{") + 1);
                s = s.Substring(0, s.IndexOf("}"));
                s = s.Substring(0,s.Length);
                string[] ae = s.Split(',');

                /*ArrayList arr = new ArrayList();
                string path_class = @"D:\final_version\Gesture-Gis-master\GestureGis2\Classfile.txt";
                using (StreamReader reader = new StreamReader(path_class))
                {
                    while (!reader.EndOfStream)
                    {
                        arr.Add(reader.ReadLine());
                    }
                    reader.Close();
                }*/
                PredictedClassbyWeka = (string)(ae[pre]);
                arrfFile.close();

                //insts.instance(index).attribute(3);
                /*System.Diagnostics.Debug.WriteLine(numCorrect + " out of " + testSize + " correct (" +
                           (double)((double)numCorrect / (double)testSize * 100.0) + "%)");
                Console.WriteLine(numCorrect + " out of " + testSize + " correct (" +
                           (double)((double)numCorrect / (double)testSize * 100.0) + "%)");*/
            }
            catch (java.lang.Exception ex)
            {
                ex.printStackTrace();
            }
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
            // Double[] arr = {F1,F2,F3,F4,F5,F6,F7,F8,F9,F10,F11};
            //List<Double> featurearray = new List<Double>();
            //featurearray.Add(F1); featurearray.Add(F2); featurearray.Add(F3); featurearray.Add(F4);
            //featurearray.Add(F5); featurearray.Add(F6); featurearray.Add(F7); featurearray.Add(F8);
            //featurearray.Add(F9); featurearray.Add(F10); featurearray.Add(F11);//featurearray.Add(BuildingType);
            string path1 = @projectDirectory+"\\Featurefile.txt";
            string path = @projectDirectory+"\\ComparisonFeaturefile.txt";

            string buildm = "Resdential";
            try
            {
                var myFile = File.Create(path);
                myFile.Close();
                TextWriter tw_new_one = new StreamWriter(path);
                using (StreamReader reader = new StreamReader(path1))
                {


                    while (!reader.EndOfStream)
                    {
                        tw_new_one.WriteLine(reader.ReadLine());

                    }
                    //tw_new_one.WriteLine("   ");
                    //tw_new_one.WriteLine(" ");
                    tw_new_one.Write(F1); tw_new_one.Write(","); tw_new_one.Write(F2); tw_new_one.Write(","); tw_new_one.Write(F3); tw_new_one.Write(","); tw_new_one.Write(F4); tw_new_one.Write(",");
                    tw_new_one.Write(F5); tw_new_one.Write(","); tw_new_one.Write(F6); tw_new_one.Write(","); tw_new_one.Write(F7); tw_new_one.Write(","); tw_new_one.Write(F8); tw_new_one.Write(",");
                    tw_new_one.Write(F9); tw_new_one.Write(","); tw_new_one.Write(F10); tw_new_one.Write(","); tw_new_one.Write(F11); tw_new_one.Write(","); tw_new_one.Write(buildm+Environment.NewLine);
                    //tw_new_one.WriteLine(" ");
                    reader.Close();
                    tw_new_one.Close();
                }

                classifyTest();
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
                //string startupPath = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.FullName;
                //string pathar = @"E:\semester 1\sketch recognition\Project\Final\source\GestureGisFinal\Gesture-Gis-1\GestureGis2\ComparisonFeaturefile.arff";
                string pathar = projectDirectory + "\\ComparisonFeaturefile.arff";
                if (File.Exists(pathar))
                {
                    File.Delete(pathar);
                }

                System.Diagnostics.Debug.WriteLine(F1 + " " + F2 + " " + F3 + " " + F4 + " " + F5 + " " + F6 + " " + F7 + " " + F8 + " " + F9 + " " + F10 + " " + F11);

            }
            catch (java.lang.Exception ex)
            {
                ex.printStackTrace();
            }


        }
#endregion
    }
}
