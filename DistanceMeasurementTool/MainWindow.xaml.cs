using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net.Mime;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using CameraControl;
using DistanceCalculator;
using FrameCollector;
using Color = System.Drawing.Color;
using Image = System.Windows.Controls.Image;
using Pen = System.Drawing.Pen;
using Point = System.Drawing.Point;
using Size = System.Drawing.Size;
using Timer = System.Timers.Timer;

namespace DistanceMeasurementTool
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        private CameraController cc;
        private int lastFrameReceivedLeft = System.Environment.TickCount;
        private int lastFrameReceivedRight = System.Environment.TickCount;

        private static Bitmap currentLeft;
        private static Bitmap currentRight;
        private static bool isLeftNew = false;
        private static bool isRightNew = false;
        private static bool marked = false;

        private static PatternResult currentPatternResLeft;
        private static PatternResult currentPatternResRight;


//        private static Bitmap currentPatern;
        private static readonly Pattern currentPattern = new Pattern();
        private static double sldPhiValue;

        private DistanceCalculator.DistanceCalculator _distanceCalculator;

        private static BitmapImage bi =
            new BitmapImage(new Uri(@"C:\Users\lukas\Pictures\2000px-FuBK_testcard_vectorized.svg.png"));

//        private Task renderTask;


        public MainWindow()
        {
//            
            InitializeComponent();
            ConsoleManager.Show();
            cc = new CameraController();
            try
            {
                cc.Startup();
                cc.OnRightBitmapReceived += OnRightImageReceived;
                cc.OnLeftBitmapReceived += OnLeftImageReceived;
                
                cc.StartAutomaticRecording();
                ImgDistance.Source = bi;

                _distanceCalculator = DistanceCalculator.DistanceCalculator.Instance;
            }
            catch (Exception e)
            {
                LblMessage.Content = e.Message;
                throw;
            }

            Task.Factory.StartNew(SpecificDistance, CancellationToken.None, TaskCreationOptions.LongRunning,
                TaskScheduler.Default);
//            Thread a = new Thread(SpecificDistance);
//            a.Start();
            LblMessage.Content = "Initialized";
        }

        private void Disparity()
        {
            while (true)
            {
                if (currentLeft != null && currentRight != null &&
                    isLeftNew && isRightNew)
                {
                    try
                    {
                        var disparity = _distanceCalculator.CalculateDisparityMap((Bitmap) currentLeft.Clone(),
                            (Bitmap) currentRight.Clone());
//                        var disparity = _distanceCalculator.CalculateDisparityMap(currentLeft, currentRight);
                        Dispatcher.Invoke(() =>
                        {
                            ImgDistance.Source = disparity.ToImageSource();
                            isLeftNew = isRightNew = false;
                        });
//                        Thread.Sleep(100);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"ex: ${ex}");
                    }
                }
            }
        }


        // continue UI blocking?
        private void SpecificDistance()
        {
            PatternResult resR;
            PatternResult resL;

            while (true)
            {
////                Console.WriteLine("Test");
//                int seconds = DateTime.Now.Second;
//                Dispatcher.Invoke(() => { LblMessage.Content = seconds; });
//                
                if (currentPattern.GetPattern() != null &&
                    isLeftNew && isRightNew)
                {
                    try
                    {
                        resR =
                            _distanceCalculator.FindPattern(currentRight, currentPattern.GetPattern());
                        resL =
                            _distanceCalculator.FindPattern(currentLeft, currentPattern.GetPattern());

                        var r = resR;
                        var l = resL;
                        currentPatternResLeft = resL;
                        currentPatternResRight = resR;
                        double distance = CalcDistance(r.MaxLoc.X, l.MaxLoc.X);
//                        Console.WriteLine(distance);

                        Dispatcher.Invoke(() =>
                        {
                            LblMessage.Content = distance;
                            ImgLeft.Source = resL.OutMap.ToImageSource();
                            ImgRight.Source = resR.OutMap.ToImageSource();
                            isLeftNew = isRightNew = false;
                        });
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"ex: ${ex}");
                    }
                }
            }
        }


        // https://www.researchgate.net/publication/305308988_Object_distance_measurement_by_stereo_vision
        private double CalcDistance(double rX, double lX)
        {
            double b = 9; // distance between the two cameras
            double x = 1024; // width of the picture
            double viewAngle = 78.81928254;

            double phi = 0; // compensation value

            double distance = (b * x) / (2 * (Math.Tan(viewAngle / 2 + sldPhiValue)) * Math.Abs(rX - lX));
            return distance;
        }

        private void OnLeftImageReceived(Bitmap bmp, double fps)
        {
            currentLeft = bmp;
            isLeftNew = true;

            Dispatcher.Invoke(() =>
            {
//                if (!marked)
                    ImgLeft.Source = bmp.ToImageSource();
                
                LbLfpsLeft.Content = $"{fps:f2} FPS";
            });
        }

        private void OnRightImageReceived(Bitmap bmp, double fps)
        {
            currentRight = bmp;
            isRightNew = true;
//            bmp = (Bitmap)bmp.Clone();

            if (currentPatternResRight != null)
            {
                Graphics g = Graphics.FromImage(bmp);
                g.DrawRectangle(new Pen(Color.Blue), new Rectangle(currentPatternResRight.MaxLoc, currentPattern.Size));
            }
            Dispatcher.Invoke(() =>
            {
//                if (!marked)
                    ImgRight.Source = bmp.ToImageSource();
                
                LbLfpsRight.Content = $"{fps:f2} FPS";
            });
        }

        // todo change selection method to self draw
        private void OnCapturePointSelected(object sender, MouseButtonEventArgs e)
        {
//            Task.Factory.StartNew(() =>
//            {
            int squareSideSize = 200;

            System.Windows.Point p = e.GetPosition(ImgLeft);
            double pixelWidth = ImgLeft.Source.Width;
            double pixelHeight = ImgLeft.Source.Height;
            int x = (int) (pixelWidth * p.X / ImgLeft.ActualWidth);
            int y = (int) (pixelHeight * p.Y / ImgLeft.ActualHeight);

//            MessageBox.Show((int)x + "/" + (int)ImgLeft.Source.Width + ";" +
//                            (int)y + "/" + (int)ImgLeft.Source.Height);

            x -= squareSideSize / 2;
            y -= squareSideSize / 2;

            x = x < 0 ? 0 : x;
            y = y < 0 ? 0 : y;

            x = x + squareSideSize > (int) ImgLeft.Source.Width ? (int) ImgLeft.Source.Width - squareSideSize : x;
            y = y + squareSideSize > (int) ImgLeft.Source.Height ? (int) ImgLeft.Source.Height - squareSideSize : y;

            Rectangle source_rect = new Rectangle(x, y, squareSideSize, squareSideSize);
            Rectangle dest_rect = new Rectangle(0, 0, squareSideSize, squareSideSize);

            Bitmap cropped = new Bitmap(squareSideSize, squareSideSize);
            Graphics g = Graphics.FromImage(cropped);
            g.DrawImage(currentLeft, dest_rect, source_rect, GraphicsUnit.Pixel);

//                Dispatcher.Invoke(() =>
//                {
            ImgDistance.Source = cropped.ToImageSource();
//                });

//            currentPatern = cropped;
//
//            RenderDiff(null, null);
//            });
        }

        private void SldPhi_OnValueChanged_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            sldPhiValue = sldPhi.Value;
        }

        //Start marking
        private void ImgLeft_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            currentPattern.BeginSelection(e, ImgLeft);
        }

        //crop and save selection
        private void ImgLeft_OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            try
            {
                currentPattern.EndSelection(e, ImgLeft, currentLeft);
                ImgDistance.Source = currentPattern.GetPattern().ToImageSource();
                marked = true;
            }
            catch
            {
                // ignored
            }
        }
    }

    class Pattern
    {
        private int startX;
        private int startY;

        private int endX;
        private int endY;

        private Bitmap pattern;
        private Size size;

        public Size Size
        {
            get => size;
        }

        public Bitmap GetPattern()
        {
            return pattern;
        }

        public void BeginSelection(MouseButtonEventArgs mouseButtonEventArgs, Image img)
        {
            System.Windows.Point p = mouseButtonEventArgs.GetPosition(img);
            double pixelWidth = img.Source.Width;
            double pixelHeight = img.Source.Height;
            startX = (int) (pixelWidth * p.X / img.ActualWidth);
            startY = (int) (pixelHeight * p.Y / img.ActualHeight);

            pattern = null;
            endX = -1;
            endY = -1;
            size = new Size(-1,-1);
        }

        public void EndSelection(MouseButtonEventArgs mouseButtonEventArgs, Image img, Bitmap currentImg)
        {
            System.Windows.Point p = mouseButtonEventArgs.GetPosition(img);
            double pixelWidth = img.Source.Width;
            double pixelHeight = img.Source.Height;
            endX = (int) (pixelWidth * p.X / img.ActualWidth);
            endY = (int) (pixelHeight * p.Y / img.ActualHeight);

            int topX = startX > endX ? endX : startX;
            int topY = startY > endY ? endY : startY;
            int bottomX = startX < endX ? endX : startX;
            int bottomY = startY < endY ? endY : startY;

            int width = bottomX - topX;
            int height = bottomY - topY;
            
            if(height == 0 || width == 0)
                throw new Exception("Width and Height must be larger than 0");
            
            size = new Size(width,height);

            Rectangle source_rect = new Rectangle(topX, topY, width, height);
            Rectangle dest_rect = new Rectangle(0, 0, width, height);

            pattern = new Bitmap(width, height);
            Graphics g = Graphics.FromImage(pattern);
            g.DrawImage(currentImg, dest_rect, source_rect, GraphicsUnit.Pixel);
        }
    }
}