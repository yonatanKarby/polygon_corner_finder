using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace rectangleRecognitionInImage
{
    public class pixelPositionCompare : IEqualityComparer<pixelPosition>
    {
        public bool Equals(pixelPosition a, pixelPosition b)
        {
            return a.x == b.x && a.y == b.y;
        }

        public int GetHashCode(pixelPosition obj)
        {
            return obj.imageIndex;
        }
    }

    public class pixelPosition
    {
        public int x { get; set; }
        public int y { get; set; }
        public double xGraph { get; set; }
        public double yGraph { get; set; }

        public int imageIndex { get; set; }
        public pixelPosition(int _x, int _y)
        {
            x = _x;
            y = _y;
        }
        public pixelPosition() { }

        public bool isEqual(pixelPosition p)
        {
            return (p.x == x) && (p.y == y);
        }
        public void getImageIndex(int width)
        {
            imageIndex = (y * width) + x;
        }

    }
}
