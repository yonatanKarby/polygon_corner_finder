using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace rectangleRecognitionInImage
{
    public class pointMatrix
    {
        public double x { get; set; }
        public double y { get; set; }
        public double z { get; set; }
        public pointMatrix() { }
        public pointMatrix(double _x, double _y, double _z)
        {
            x = _x; y = _y; z = _z;
        }
    }

    public class matrixFunctions
    {
        private static double[][] getXRotaionMatrix(double angle)
        {
            return new double[][]
            {
                new double[]{ 1, 0, 0},
                new double[]{ 0, Math.Cos(angle), -Math.Sin(angle)},
                new double[]{ 0, Math.Sin(angle), Math.Cos(angle)},
            };
        }
        private static double[][] getYRotaionMatrix(double angle)
        {
            return new double[][]
            {
                new double[]{ Math.Cos(angle), 0, Math.Sin(angle)},
                new double[]{ 0, 1, 0},
                new double[]{ -Math.Sin(angle), 0, Math.Cos(angle)},
            };
        }
        private static double[][] getZRotaionMatrix(double angle)
        {
            return new double[][]
            {
                new double[]{ Math.Cos(angle), -Math.Sin(angle), 0},
                new double[]{ Math.Sin(angle), Math.Cos(angle), 0},
                new double[]{ 0, 0, 1},
            };
        }

        public static pointMatrix rotatePoint(pointMatrix p, double angleX, double angleY, double angleZ)
        {
            p = matrixMult(getXRotaionMatrix(angleX), p);
            p = matrixMult(getYRotaionMatrix(angleY), p);
            p = matrixMult(getZRotaionMatrix(angleZ), p);
            return p;
        }

        public static Point[] doPerspective(pointMatrix[] points)
        {
            return points.Select(p =>
            {
                return new Point((int)(p.x / Math.Sqrt(p.z)),
                    (int)(p.y / Math.Sqrt(p.z)));
            }).ToArray();
        }

        private static pointMatrix matrixMult(double[][] matrix, pointMatrix p)
        {
            var temp = new pointMatrix();
            temp.x = p.x * matrix[0][0] + p.y * matrix[0][1] + p.z * matrix[0][2];
            temp.y = p.x * matrix[1][0] + p.y * matrix[1][1] + p.z * matrix[1][2];
            temp.z = p.x * matrix[2][0] + p.y * matrix[2][1] + p.z * matrix[2][2];
            return temp;
        }
    }

    class imageRotater
    {
        private static pointMatrix[] create3DPoints(double imageWidth, double imageHeight, pointMatrix origin)
        {
            double width = imageWidth / 2;
            double height = imageHeight / 2;
            return new pointMatrix[]
            {
                new pointMatrix(- width, - height, 0),
                new pointMatrix(+ width, - height, 0),
                new pointMatrix(+ width, + height, 0),
                new pointMatrix(- width, + height, 0),
            };
        }
        private static double calculatePointDistance(Point a, Point b)
        {
            int deltaX = b.x - a.x;
            int deltaY = b.y - a.y;
            return Math.Sqrt(Math.Pow(deltaX, 2) + Math.Pow(deltaY, 2));
        }

        private static int calculateSegmentation(double k, int x1, int x2)
        {
            return (int)((x1 * k) + (x2 * (1 - k)));
        }

        public static Point[] getImagePoints(Point[] framPoints,rect r, double imageWidth, double imageHeight)
        {

            double frameWidth_1 = calculatePointDistance(framPoints[0], framPoints[1]);
            double frameWidth_2 = calculatePointDistance(framPoints[2], framPoints[3]);
            double frameHeight_1 = calculatePointDistance(framPoints[1], framPoints[2]);
            double frameHeight_2 = calculatePointDistance(framPoints[3], framPoints[0]);

            double pixelsPerUnitW_1 = (frameWidth_1 / r.w);
            double pixelsPerUnitW_2 = (frameWidth_2 / r.w);
            double pixelsPerUnitH_1 = (frameHeight_1 / r.h);
            double pixelsPerUnitH_2 = (frameHeight_2 / r.h);
            
            double KW1 = (frameWidth_1 - imageWidth * pixelsPerUnitW_1) / (frameWidth_1 * 2.0);
            double KW2 = (frameWidth_2 - imageWidth * pixelsPerUnitW_2) / (frameWidth_2 * 2.0);
            double KH1 = (frameHeight_1 - imageHeight * pixelsPerUnitH_1) / (frameHeight_1 * 2.0);
            double KH2 = (frameHeight_2 - imageHeight * pixelsPerUnitH_2) / (frameHeight_2 * 2.0);

            return new Point[]
            {
                new Point(calculateSegmentation(KW1, framPoints[0].x, framPoints[1].x), calculateSegmentation(KH2, framPoints[0].y, framPoints[3].y)),
                new Point(calculateSegmentation(KW1, framPoints[1].x, framPoints[0].x), calculateSegmentation(KH1, framPoints[1].y, framPoints[2].y)),
                new Point(calculateSegmentation(KW2, framPoints[2].x, framPoints[3].x), calculateSegmentation(KH2, framPoints[2].y, framPoints[1].y)),
                new Point(calculateSegmentation(KW2, framPoints[3].x, framPoints[2].x), calculateSegmentation(KH1, framPoints[3].y, framPoints[0].y)),
            };
        }
    }
}
