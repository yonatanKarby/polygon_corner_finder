using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace rectangleRecognitionInImage
{
    public class PointOpacityDTO
    {
        public Point p { get; set; }
        public double opacityPercent { get; set; }
    }

    public class RectangleMaker
    {
        private static int edgeDetectionMin = 2;
        private static int searchDistance = 2;


        private static void printMatrix(byte[][] matrix, string filePath)
        {
            Bitmap printImage = new Bitmap(matrix[0].Length, matrix.Length);
            var white = Color.FromArgb(255, 255, 255);
            var black = Color.FromArgb(0, 0, 0);

            for (int i = 0; i < matrix.Length; i++)
            {
                for (int m = 0; m < matrix[0].Length; m++)
                {
                    if (matrix[i][m] == 0)
                        printImage.SetPixel(m, i, black);
                    else
                        printImage.SetPixel(m, i, white);
                }
            }
            printImage.Save(filePath);
        }



        public static bool IsPointInPolygon(Point point, Point[] polygon)
        {
            int polygonLength = polygon.Length, i = 0;
            bool inside = false;
            // x, y for tested point.
            float pointX = point.x, pointY = point.y;
            // start / end point for the current polygon segment.
            float startX, startY, endX, endY;
            Point endPoint = polygon[polygonLength - 1];
            endX = endPoint.x;
            endY = endPoint.y;
            while (i < polygonLength)
            {
                startX = endX; 
                startY = endY;

                endPoint = polygon[i++];

                endX = endPoint.x; 
                endY = endPoint.y;
                
                inside ^= (endY > pointY ^ startY > pointY) /* ? pointY inside [startY;endY] segment ? */
                          && /* if so, test if it is under the segment */
                          ((pointX - endX) < (pointY - endY) * (startX - endX) / (startY - endY));
            }
            return inside;
        }









        private static void bluerEdges(Bitmap bmp)
        {
            var matrix = getMatrix(bmp);

            var bluerPoints = edgeDetection(matrix);

            bluer(bmp, bluerPoints, 50);
        }

        public static byte[][] getMatrix(Point[] pixels, int width, int height)
        {
            var matrix = new byte[height][];
            for (int i = 0; i < matrix.Length; i++)
            {
                matrix[i] = new byte[width];
                for (int m = 0; m < matrix[i].Length; m++)
                    matrix[i][m] = 0;
            }
            foreach (var pixel in pixels)
            {
                matrix[pixel.y][pixel.x] = 1;
            }
            return matrix;
        }

        private static void bluer(Bitmap bmp, PointOpacityDTO[] points, int newAlpha)
        {
            foreach(var point in points)
            {
                var c = copyColorWithNewAlpha(bmp.GetPixel(point.p.x, point.p.y), (int)(point.opacityPercent * newAlpha));
                bmp.SetPixel(point.p.x, point.p.y, c);
            }
        }

        private static Color copyColorWithNewAlpha(Color c, int newAlpha)
        {
            return Color.FromArgb(newAlpha, c.R, c.G, c.B);
        }

        private static PointOpacityDTO[] edgeDetection(byte[][] matrix)
        {
            var edgePoint = new List<PointOpacityDTO>();
            double farthest = searchDistance * searchDistance * 2;
            for (int i = 0; i < matrix.Length; i++)
            {
                for (int m = 0; m < matrix[0].Length; m++)
                {
                    if (matrix[i][m] == 0)
                        continue;
                    var distance = isEdged(matrix, m, i);
                    if (distance == double.MaxValue)
                    {
                        continue;
                    }

                    edgePoint.Add(new PointOpacityDTO()
                    {
                        p = new Point(m, i),
                        opacityPercent = (distance) / (farthest),
                    });
                }
            }
            return edgePoint.ToArray();
        }

        private static double isEdged(byte[][] matrix, int x, int y)
        {
            double minDistance = double.MaxValue;
            for (int i = -searchDistance; i <= searchDistance; i++)
            {
                for (int m = -searchDistance; m <= searchDistance; m++)
                {
                    int px = x + i;
                    int py = y + m;
                    if (px < 0 || py < 0 || px > matrix[0].Length - 1 || py > matrix.Length - 1)
                        continue;

                    if (matrix[py][px] == 0)
                    {
                        var currentDistance = (i * i) + (m * m);
                        if (currentDistance < minDistance)
                            minDistance = currentDistance;
                    }
                }
            }
            return minDistance;
        }

        private static double calculatePointDistance(Point a, Point b)
        {
            int deltaX = b.x - a.x;
            int deltaY = b.y - a.y;
            return Math.Pow(deltaX, 2) + Math.Pow(deltaY, 2);
        }

        private static byte[][] getMatrix(Bitmap bmp)
        {
            var matrix = new byte[bmp.Height][];
            for (int i = 0; i < matrix.Length; i++)
            {
                matrix[i] = new byte[bmp.Width];
                for (int m = 0; m < matrix[i].Length; m++)
                    matrix[i][m] = 0;
            }

            for (int i=0; i<matrix.Length; i++)
            {
                for(int m=0; m<matrix[0].Length; m++)
                {
                    var alpha = bmp.GetPixel(m, i).A;
                    if (alpha < 255)
                        matrix[i][m] = 1;
                }
            }
            return matrix;
        }

        private static Color getNewPixelRGBA(Point[] square, Point position, Color current)
        {
            if (IsPointInPolygon(position, square))
                return Color.FromArgb(0, 0, 0, 0);//pixel need to be with alpha of 0
            else
                return current;
        }
        private static void drawSquare(Point[] square, Bitmap bmp)
        {
            for (int y = 0; y < bmp.Height; y++)
            {
                for (int x = 0; x < bmp.Width; x++)
                {
                    var color = getNewPixelRGBA(square, new Point(x, y), bmp.GetPixel(x, y));
                    bmp.SetPixel(x, y, color);
                }
            }
        }

        public static void drawSquaresOnImage(Point[][] squares, Bitmap bmp)
        {
            foreach (var square in squares)
                drawSquare(square, bmp);
            
            bluerEdges(bmp);
        }
    }
}
