using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;

namespace DistanceCalculator
{
    public class DistanceCalculator
    {
        public static DistanceCalculator Instance => _instance ?? (_instance = new DistanceCalculator());
        private static DistanceCalculator _instance;

        public PatternResult FindPattern(Bitmap source, Bitmap pattern)
        {
            Image<Bgr, Byte> outImage =
                new Image<Bgr, byte>(source.Width - pattern.Width + 1, source.Height - pattern.Height + 1);

            IOutputArray outAr = outImage.GetOutputArray().GetMat();

            IOutputArray sourceAr = source.ToImage<Bgr, Byte>().GetInputArray().GetMat();
            IOutputArray paternAr = pattern.ToImage<Bgr, Byte>().GetInputArray().GetMat();
            
            CvInvoke.CvtColor(sourceAr, sourceAr, ColorConversion.Bgr2Gray);
            CvInvoke.CvtColor(paternAr, paternAr, ColorConversion.Bgr2Gray);
            
            CvInvoke.EqualizeHist(paternAr, paternAr);
            CvInvoke.EqualizeHist(paternAr, paternAr);

            CvInvoke.MatchTemplate(sourceAr, paternAr, outAr, TemplateMatchingType.CcoeffNormed);
            double min_val = 0, max_val = 0;
            Point min_loc = new Point(), max_loc = new Point();

            CvInvoke.MinMaxLoc(outAr, ref min_val, ref max_val, ref min_loc, ref max_loc);

//           IInputOutputArray result = sourceAr.GetInputArray().GetMat();
//           CvInvoke.Rectangle(result, new Rectangle(max_loc, pattern.Size), new MCvScalar(255), 2);

            return new PatternResult(min_val, max_val,
                min_loc, max_loc, source
                /*result.GetOutputArray().GetMat().ToImage<Bgr, Byte>().ToBitmap()*/);
        }

        [Obsolete("Doesn't work ... idk why")]
        // https://docs.opencv.org/master/dd/d53/tutorial_py_depthmap.html
        public Bitmap CalculateDisparityMap(Bitmap left, Bitmap right)
        {
            StereoBM sbm = new StereoBM(16, 11);
            
            IOutputArray outputArray = new Image<Bgr, byte>(left.Size);
            IOutputArray leftAr = left.ToImage<Bgr, Byte>().GetInputArray().GetMat();
            IOutputArray rightAr = right.ToImage<Bgr, Byte>().GetInputArray().GetMat();
            
            CvInvoke.CvtColor(leftAr, leftAr, ColorConversion.Bgr2Gray);
            CvInvoke.CvtColor(rightAr, rightAr, ColorConversion.Bgr2Gray);

            
            sbm.Compute(leftAr, rightAr, outputArray);
            
//            outputArray.GetInputArray().GetMat().ToBitmap().Save("./newImg.png", ImageFormat.Png);
//            Thread.Sleep(1000000);
            return outputArray.GetInputArray().GetMat().ToBitmap();
        }
    }

    public class PatternResult
    {
        public PatternResult(double minVal, double maxVal, Point minLoc, Point maxLoc, Bitmap outMap)
        {
            MinVal = minVal;
            MaxVal = maxVal;
            MinLoc = minLoc;
            MaxLoc = maxLoc;
            this.OutMap = outMap ?? throw new ArgumentNullException(nameof(outMap));
        }

        public double MinVal { get; }

        public double MaxVal { get; }

        public Point MinLoc { get; }

        public Point MaxLoc { get; }

        public Bitmap OutMap { get; }
    }
}