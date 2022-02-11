using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace rectangleRecognitionInImage
{
    public class Point
    {
        public int x;
        public int y;

        public Point(int x, int y)
        {
            this.x = x;
            this.y = y;
        }
    };

    public class rect
    {
        public double w { get; set; }
        public double h { get; set; }
    }

    public partial class Form1 : Form
    {

        public static string mainPath = @"F:\yonatan\coding projects\rectangleRecognitionInImage\rectangleRecognitionInImage\images\testing\";

        public Form1()
        {
            InitializeComponent();
        }
        
        private void Form1_Load(object sender, EventArgs e)
        {
            runSingle(@"F:\yonatan\coding projects\rectangleRecognitionInImage\rectangleRecognitionInImage\images\testing\testing10.png");
        }

        public void runSingle(int num)
        {
            runTest(num);
        }
        public void runSingle(string num)
        {
            runTest(num);
        }


        public void runMulti(int count)
        {
            for (int i = 1; i < count+1; i++)
            {
                Console.WriteLine($"running test on image number:{i}");
                runTest(i);
            }
            Console.WriteLine("done!");
        }

        public void runTest(string path)
        {
            var rectangles = new rectangleFinder().find(path, false, 30);

            Bitmap image = new Bitmap(path);

            var refImage = getRefrenceImage(image);
            string outputPath = $"{mainPath}output-{0}.png";


            var color = Color.FromArgb(0, 255, 0);
            foreach (var currentRect in rectangles)
            {
                for (int i = 0; i < currentRect.edges.Length; i++)
                {
                    var pixel = currentRect.edges[i];
                    drawText(refImage, pixel, i + 1);
                    drawAroundPoint(refImage, pixel, color);
                }

                drawCrosses(refImage, currentRect);
                drawAroundPoint(refImage, currentRect.centerOfMass, color);
            }


            refImage.Save(outputPath);
        }

        public void runTest(int num)
        {
            string path = $"{mainPath}testing{num}.png";
            var rectangles = new rectangleFinder().find(path, true,30);
            
            Bitmap image = new Bitmap(path);

            var refImage = getRefrenceImage(image);
            string outputPath = $"{mainPath}output-{num}.png";

            
            var color = Color.FromArgb(0, 255, 0);
            foreach(var currentRect in rectangles)
            {
                for (int i = 0; i < currentRect.edges.Length; i++)
                {
                    var pixel = currentRect.edges[i];
                    drawText(refImage, pixel, i + 1);
                    drawAroundPoint(refImage, pixel, color);
                }
                
                drawCrosses(refImage, currentRect);
                drawAroundPoint(refImage, currentRect.centerOfMass, color);
            }


            refImage.Save(outputPath);
        }

        private Bitmap getConstBackgroundImage(int width, int height, Color c)
        {
            Bitmap bmp = new Bitmap(width, height);
            for(int i=0; i<height; i++)
            {
                for(int m=0; m<width; m++)
                {
                    bmp.SetPixel(m, i, c);
                }
            }
            return bmp;
        }

        public void drawCrosses(Bitmap image, square s)
        {
            using (Graphics g = Graphics.FromImage(image))
            {
                g.DrawLine(new Pen(Color.Blue), s.crosses[0].p1.x, s.crosses[0].p1.y, s.crosses[0].p2.x, s.crosses[0].p2.y);
                g.DrawLine(new Pen(Color.Blue), s.crosses[1].p1.x, s.crosses[1].p1.y, s.crosses[1].p2.x, s.crosses[1].p2.y);
            }
        }

        public void drawText(Bitmap image, pixelPosition location, int id)
        {
            Graphics g = Graphics.FromImage(image);

            RectangleF rectf = new RectangleF(location.x, location.y, 90, 50);

            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.DrawString(id.ToString(), new Font("Tahoma", 32), Brushes.Red, rectf);

        }

        public Bitmap getRefrenceImage(Bitmap image)
        {
            Bitmap referenceImage = new Bitmap(width: image.Width, 
                height: image.Height);
            var pos = Color.FromArgb(255, 255, 255);
            var neg = Color.FromArgb(0, 0, 0);
            for(int y=0; y<image.Height; y++)
            {
                for(int x=0; x<image.Width; x++)
                {
                    if(image.GetPixel(x,y).A < 1.0)
                    {
                        referenceImage.SetPixel(x, y, pos);
                    }
                    else
                    {
                        referenceImage.SetPixel(x, y, neg);
                    }
                }
            }
            //referenceImage.Save(@"F:\yonatan\coding projects\rectangleRecognitionInImage\rectangleRecognitionInImage\images\testing\ref.png");
            return referenceImage;
        }

        public void drawAroundPoint(Bitmap image, pixelPosition pixel, Color c)
        {
            for(int y=-3; y <= 3; y++)
            {
                for (int x = -3; x <= 3; x++)
                {
                    image.SetPixel(x + pixel.x, y + pixel.y, c);
                }
            }
        }
        public void drawAroundPoint(Bitmap image, Point pixel, Color c)
        {
            for (int y = -3; y <= 3; y++)
            {
                for (int x = -3; x <= 3; x++)
                {
                    image.SetPixel(x + pixel.x, y + pixel.y, c);
                }
            }
        }
        private Bitmap drawPointImage(int width, int height, Point[] points)
        {
            var bmp = new Bitmap(width, height);
            for(int y=0; y<height; y++)
            {
                for(int x=0; x<width; x++)
                {
                    bmp.SetPixel(x, y, Color.FromArgb(0, 0, 0));
                }
            }
            for(int i=0; i<points.Length; i++)
            {
                Color c;
                if (i <= 3)
                    c = Color.FromArgb(255, 255, 0);
                else
                    c = Color.FromArgb(0, 255, 255);
                drawAroundPoint(bmp, points[i], c);
                
            }
            return bmp;
        }

    }
}
