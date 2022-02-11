using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace rectangleRecognitionInImage
{
    public class square
    {
        public pixelPosition[] pixels = new pixelPosition[0];
        
        //we use this in one part of the code, to save running time
        private List<pixelPosition> listPixels = new List<pixelPosition>();
        public int id { get; set; }
        public pixelPosition centerOfMass { get; set; }
        public pixelPair[] crosses = new pixelPair[2];
        public pixelPosition[] edges { get; set; }

        public square( int _id)
        {
            id = _id;
        }

        public void pushBackEdges(double distance, bool isLandScape)
        {
            updateCrosses();
            if (isLandScape)
            {
                var linear1 = new LinearFuncion(crosses[0]);
                var linear2 = new LinearFuncion(crosses[1]);

                edges[0] = linear1.pushBackPixel(edges[0], -distance);
                edges[2] = linear1.pushBackPixel(edges[2], distance);

                edges[1] = linear2.pushBackPixel(edges[1], distance);
                edges[3] = linear2.pushBackPixel(edges[3], -distance);
            }
            else
            {
                var linear1 = new LinearFuncion(crosses[0]);
                var linear2 = new LinearFuncion(crosses[1]);

                edges[0] = linear1.pushBackPixel(edges[0], distance);
                edges[2] = linear1.pushBackPixel(edges[2], -distance);

                edges[1] = linear2.pushBackPixel(edges[1], distance);
                edges[3] = linear2.pushBackPixel(edges[3], -distance);
            }
        }
        public void updateCrosses()
        {
            crosses[0] = new pixelPair()
            {
                p1 = edges[0],
                p2 = edges[2],
            };
            crosses[1] = new pixelPair()
            {
                p1 = edges[1],
                p2 = edges[3],
            };
        }
        public void convertList()
        {
            pixels = listPixels.ToArray();
        }

        public void addPixel(pixelPosition p)
        {
            listPixels.Add(p);
        }
    }
}
