using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace rectangleRecognitionInImage
{
    class LinearFuncion
    {
        public double m, b, angle;
        public double getY(double x)
        {
            return (m * x) + b;
        }
        public double getX(double y)
        {
            return (y - b) / m;
        }

        public pixelPosition calculateIntersection(LinearFuncion func2)
        {
            double x = ((func2.b - b) / (m - func2.m));
            double y = getY(x);
            return new pixelPosition()
            {
                x = (int)x,
                y = (int)y,
                xGraph = x,
                yGraph = y,
            };
        }
        public LinearFuncion(pixelPair pair)
        {
            m = pair.calculateAngle();
            angle = Math.Atan(m);
            b = pair.p1.y - (m * pair.p1.x);
        }
        public pixelPosition pushBackPixel(pixelPosition p, double distance)
        {
            double moveX = Math.Cos(angle) * distance;
            double moveY = Math.Sin(angle) * distance;
            return new pixelPosition((int)(p.x + moveX), (int)(p.y + moveY));
        }
        public bool doseContain(pixelPosition p)
        {
            var yval = (int)getY(p.x);
            return (yval - 5) < p.y && (yval + 5) > p.y;
        }
    }
}
