using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace rectangleRecognitionInImage
{
    public class pixelPair
    {
        public pixelPair() { }

        public pixelPosition p1 { get; set; }
        public pixelPosition p2 { get; set; }
        public double distance { get; set; }

        public bool anyEqual(pixelPosition point)
        {
            return p1.isEqual(point) || p2.isEqual(point);
        }
        public double calculateAngle()
        {
            return (double)(p2.y - p1.y) / (double)(p2.x - p1.x);
        }
    }


}
