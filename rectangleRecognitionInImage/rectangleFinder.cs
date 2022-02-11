using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Runtime.InteropServices;

namespace rectangleRecognitionInImage
{
    public class edgePointCompare
    {
        public double score { get; set; }
        public pixelPosition point { get; set; }
    }


    public class rectangleFinder
    {
        //This tells the isPairValid function how much to search around the search pixel
        private static int searchDistance = 2;
        //This is an error paramter helps with filtering bad pairs of pixel
        private static int maxDiviation = 7;
        //This keeps track of how many none colored pixels do there need to be for a pixel to considered an edge
        private static int edgeDetectionMin = 2; 
        
        public rectangleFinder()
        {

        }

        /// <summary>
        /// Get all pixels with that have some alpha removed from them,
        /// *THIS WORKS WITH THE 32bppArg FORMAT AND IS NOT COMPATIBLE WITH OTHERS*
        /// </summary>
        /// <param name="bmp"></param>
        /// <returns></returns>
        public pixelPosition[] getRectangePositions(Bitmap bmp)
        {
            var width = bmp.Width;
            var pixelCount = bmp.Width * bmp.Height * 4;//we have 4 bytes per pixel
            var pixels = new byte[pixelCount];

            var rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
            IntPtr imageptr = bmp.LockBits(rect, 
                System.Drawing.Imaging.ImageLockMode.ReadOnly, 
                System.Drawing.Imaging.PixelFormat.Format32bppArgb).Scan0;

            Marshal.Copy(imageptr, pixels, 0, pixels.Length);
            var pixelPositions = new List<pixelPosition>();

            for (int i=width; i<pixelCount - width; i+=4)// 4 bytes per pixel
            {
                var alpha = pixels[i + 3];
                if(alpha < 255)
                {
                    var index = i / 4;
                    int x = index % width;

                    if (x == 0)//remove the outer edge of the image
                        continue;

                    pixelPositions.Add(new pixelPosition()
                    {
                        x = x,
                        y = index / width,
                    });
                }
            }
            return pixelPositions.ToArray();
        }

        public double calculatePointDistance(pixelPosition a, pixelPosition b)
        {
            int deltaX = b.x - a.x;
            int deltaY = b.y - a.y;
            return Math.Pow(deltaX, 2) + Math.Pow(deltaY, 2);
        }

        //Calculate center of mass of a cluster of pixelPositions
        public pixelPosition getCenterOfMass(pixelPosition[] rectangle)
        {
            int sumx = 0;
            int sumy = 0;

            for(int i=0; i<rectangle.Length; i++)
            {
                sumx += rectangle[i].x;
                sumy += rectangle[i].y;
            }
            
            return new pixelPosition()
            {
                x = sumx / rectangle.Length,
                y = sumy / rectangle.Length,
            };
        }

        //This returens the corners of the rectangle
        public square getRectangleCorners(square rectangle, int width, int height, bool isLandScape)
        {

            if(rectangle.pixels.Length <= 4) // min required for a detection
            {
                throw new Exception("could not find enough points to generate a rectangle");
            }

            pixelPosition centerOfMass = getCenterOfMass(rectangle.pixels);
            
            var edges = getCorners(rectangle.pixels, centerOfMass,width, height, isLandScape);
            
            if (edges == null)
                return null;
            
            rectangle.centerOfMass = centerOfMass;
            rectangle.edges = edges;
            
            return rectangle;
        }
        /// <summary>
        /// This function returns the 4 corners of a square given a cluster of points "pixels"
        /// that make up the square
        /// </summary>
        /// <param name="pixels"></param>
        /// <param name="centerOfMass"></param>
        /// <param name="imageWidth"></param>
        /// <param name="imageHeight"></param>
        /// <returns>4 corner points</returns>ד
        public pixelPosition[] getCorners(pixelPosition[] pixels, pixelPosition centerOfMass,int imageWidth, int imageHeight, bool isLandScape)
        {
            //Make a matrix that includes just the cluster that we are working on now
            var matrix = getMatrix(pixels, imageWidth, imageHeight);

            //Do an edge detection to get only the edge pixels
            var searchPixels = edgeDetect(matrix);
            
            if(searchPixels.Length == 0)
            {
                return null;
            }
            
            var pixelAndles = getPixelPairs(matrix, searchPixels);
            
            var liniarFunctions = getLinearFunction(pixelAndles);

            if (liniarFunctions == null)
                return null;

            return getLinierFunctionIntercetions(liniarFunctions, centerOfMass,imageWidth, isLandScape);
        }


        /// <summary>
        /// This function takes in the the 4 linear, and returns the corner points in the rectangle
        /// *key note (it specifials the numbering of the corners based on the "isLandScape" parameter)
        /// </summary>
        /// <param name="functions"></param>
        /// <param name="centerOfMass"></param>
        /// <param name="imageWidth"></param>
        /// /// <param name="isLandScape"></param>
        /// <returns>The 4 corners of the square</returns>
        private pixelPosition[] getLinierFunctionIntercetions(LinearFuncion[] functions,pixelPosition centerOfMass, int imageWidth, bool isLandScape)
        {
            var points = new List<pixelPosition>();
            for(int i=0; i<functions.Length; i++)
            {
                for (int m = i + 1; m < functions.Length; m++)
                {
                    var point = functions[i].calculateIntersection(functions[m]);
                    point.getImageIndex(imageWidth);
                    points.Add(point);//calculate all the points
                }
            }
            var possibleEdgePoints = points
                .Distinct(new pixelPositionCompare())
                .ToArray();

            var scoredPoints = possibleEdgePoints.Select(p =>
                {
                    var score = calculatePointDistance(centerOfMass, p);
                    return new edgePointCompare()
                    {
                        point = p,
                        score = score
                    };
                })
                .OrderBy(p => p.score)
                .ToArray();
            
            var rectangleEdges = scoredPoints
                .Take(4)
                .Select(p => p.point)
                .ToArray();

            return orderEdges(functions, rectangleEdges, isLandScape);
        }

        /// <summary>
        /// This function orders the points in the correct numbering based on the
        /// isLandScape parmeter
        /// </summary>
        /// <param name="functions"></param>
        /// <param name="rectangleEdges"></param>
        /// /// <param name="isLandScape"></param>
        /// <returns>The 4 corners ordered correctly</returns>
        private pixelPosition[] orderEdges(LinearFuncion[] functions, pixelPosition[] rectangleEdges, bool isLandScape)
        {
            var diagonal1 = getDiagonals(functions, rectangleEdges);
            var edges = new pixelPosition[4];

            pixelPosition pixel1 = null;
            pixelPosition pixel2 = null;

            
            for(int i=0; i<rectangleEdges.Length; i++)
            {
                if(!(rectangleEdges[i].isEqual(diagonal1.p1) || rectangleEdges[i].isEqual(diagonal1.p2)))
                {
                    if (pixel1 == null)
                        pixel1 = rectangleEdges[i];
                    else
                        pixel2 = rectangleEdges[i];
                }
            }
            var diagonal2 = new pixelPair()
            {
                p1 = pixel1,
                p2 = pixel2,
            };
            
            pixelPair pair1;
            pixelPair pair2;
            var angle = diagonal1.calculateAngle();
            if (angle < 0)
            {
                pair1 = diagonal1;
                pair2 = diagonal2;
            }
            else
            {
                pair1 = diagonal2;
                pair2 = diagonal1;
            } 

            if(isLandScape)
            {
                //landScape logic
                edges[3] = pair1.p1.x >= pair1.p2.x ? pair1.p2 : pair1.p1;
                edges[1] = pair1.p1.x < pair1.p2.x ? pair1.p2 : pair1.p1;

                edges[2] = pair2.p1.x >= pair2.p2.x ? pair2.p1 : pair2.p2;
                edges[0] = pair2.p1.x < pair2.p2.x ? pair2.p1 : pair2.p2;
            }
            else
            {
                //portrait logic
                edges[2] = pair1.p1.x >= pair1.p2.x ? pair1.p2 : pair1.p1;
                edges[0] = edges[2] == pair1.p1 ? pair1.p2 : pair1.p1;

                edges[1] = pair2.p1.x >= pair2.p2.x ? pair2.p1 : pair2.p2;
                edges[3] = edges[1] == pair2.p1 ? pair2.p2 : pair2.p1;
            }

            return edges;
        }

      
        private pixelPair getDiagonals(LinearFuncion[] functions, pixelPosition[] edges)
        {
            for(int i=0; i< edges.Length; i++)
            {
                for(int m=i+1; m<edges.Length; m++)
                {
                    if (isDiagonal(edges[i], edges[m], functions))
                        return new pixelPair()
                        {
                            p1 = edges[i],
                            p2 = edges[m],
                        };
                }
            }
            return null;
        }


        private bool isDiagonal(pixelPosition p1, pixelPosition p2, LinearFuncion[] functions)
        {
            var testPair = new pixelPair()
            {
                p1 = p1,
                p2 = p2
            };

            foreach(var function in functions)
            {
                if (cantainsBoth(testPair, function))
                    return false;
            }
            return true;
        }

        private bool cantainsBoth(pixelPair pair, LinearFuncion func)
        {
            var isP1 = (int)func.getY(pair.p1.xGraph) == pair.p1.y;
            var isP2 = (int)func.getY(pair.p2.xGraph) == pair.p2.y;
            return isP1 && isP2;
        }

        private void printMatrix(int[][] matrix, string filePath)
        {
            Bitmap printImage = new Bitmap(matrix[0].Length, matrix.Length);
            var white = Color.FromArgb(255, 255, 255);
            var black = Color.FromArgb(0, 0, 0);

            for (int i=0; i<matrix.Length; i++)
            {
                for(int m=0; m<matrix[0].Length; m++)
                {
                    if (matrix[i][m] == 0)
                        printImage.SetPixel(m, i, black);
                    else
                        printImage.SetPixel(m, i, white);
                }
            }
            printImage.Save(filePath);
        }

        /// <summary>
        /// This function takes in pairs of pixels on the edge of the square
        /// and returns the linear functions that makeup the edges of the square
        /// </summary>
        /// <param name="pairs"></param>
        /// <returns>linear functions</returns>
        private LinearFuncion[] getLinearFunction(pixelPair[] pairs)
        {
            pairs = pairs
                .OrderByDescending(p => p.distance)
                .ToArray();
            while(pairs.Length > 10)
            {
                for (int i = 0; i < pairs.Length; i++)
                {
                    var removeIndexs = new List<int>();
                    for (int m = i + 1; m < pairs.Length; m++)
                    {
                        int index = contains(pairs[i], pairs[m]);
                        if (index != -1)
                        {
                            switch (index)
                            {
                                case 1:
                                    removeIndexs.Add(i);
                                    break;
                                case 2:
                                    removeIndexs.Add(m);
                                    break;
                            }
                        }
                    }
                    pairs = fastRemove(pairs, removeIndexs.ToArray());
                }
            }
            while(pairs.Length > 4)
            {
                pairs = removeSimularFromPairs(pairs);
            }

            return pairs
                .Select(p => new LinearFuncion(p))
                .ToArray();
        }

        /// <summary>
        /// Remove pairs that are too similar to each other
        /// </summary>
        /// <param name="pairs"></param>
        /// <returns>4 pairs of points that make up the linear functions of the square's edges</returns>
        private pixelPair[] removeSimularFromPairs(pixelPair[] pairs)
        {
            int index = 0;
            var minScore = getPairSimilarityScore(pairs[0], pairs[1]);
            for(int i=0; i<pairs.Length; i++)
            {
                for(int m=i+1; m<pairs.Length; m++)
                {
                    var currentScore = getPairSimilarityScore(pairs[i], pairs[m]);
                    if (currentScore < minScore)
                    {
                        minScore = currentScore;
                        index = getIndex(pairs[i], pairs[m], i, m);
                    }
                }
            }

            var newPairs = new List<pixelPair>();
            for(int i=0; i<pairs.Length; i++)
                if (i != index)
                    newPairs.Add(pairs[i]);

            return newPairs.ToArray();
        }

        private int getIndex(pixelPair p1, pixelPair p2, int i, int m)
        {
            return p1.distance > p2.distance ? i : m;
        }

        //remove pairs faster...
        private pixelPair[] fastRemove(pixelPair[] pairs, int[] indexs)
        {
            var hashSet = indexs.ToHashSet();
            var newList = new List<pixelPair>();
            for (int i = 0; i < pairs.Length; i++)
                if (!hashSet.Contains(i))
                    newList.Add(pairs[i]);
            return newList.ToArray();
        }

        private void drawLineImage(pixelPair[] pairs, int width, int height, string filePath)
        {
            Bitmap bmp = new Bitmap(width, height);
            for (int i = 0; i < bmp.Height; i++)
                for (int m = 0; m < bmp.Width; m++)
                    bmp.SetPixel(m, i, Color.Black);
            using(Graphics g = Graphics.FromImage(bmp))
            {
                foreach (var pair in pairs)
                {
                    g.DrawLine(new Pen(Color.White), pair.p1.x, pair.p1.y, pair.p2.x, pair.p2.y);
                    g.DrawEllipse(new Pen(Color.Green), pair.p1.x, pair.p1.y, 3, 3);
                    g.DrawEllipse(new Pen(Color.Red), pair.p2.x, pair.p2.y, 3, 3);
                }
            }
            bmp.Save(filePath);
        }

        private int contains(pixelPair pair1, pixelPair pair2)
        {
            if (isPairContainPoint(pair1, pair2.p1) && isPairContainPoint(pair1, pair2.p2))
                return 2;// pair 2 is contained in pair 1
            else if (isPairContainPoint(pair2, pair1.p1) && isPairContainPoint(pair2, pair1.p2))
                return 1;// pair 1 is contained in pair 2       
            return anyPointEqual(pair1, pair2);
        }

        private int anyPointEqual(pixelPair pair1, pixelPair pair2)
        {
            if (pair1.anyEqual(pair2.p1) || pair1.anyEqual(pair2.p2))
                return biggerDistance(pair1, pair2);
            return -1;
        }

        private int biggerDistance(pixelPair p1, pixelPair p2)
        {
            if (p1.distance > p2.distance)
                return 2;// we need to remove 2
            return 1;// we need to remove 1
        }

        private bool isPairContainPoint(pixelPair pair, pixelPosition p)
        {
            int maxX = Math.Max(pair.p1.x, pair.p2.x);
            int maxY = Math.Max(pair.p1.y, pair.p2.y);

            int minX = Math.Min(pair.p1.x, pair.p2.x);
            int minY = Math.Min(pair.p1.y, pair.p2.y);

            return (p.x > minX - maxDiviation && p.x < maxX + maxDiviation && p.y > minY - maxDiviation && p.y < maxY + maxDiviation);
        }

        private pixelPosition[] edgeDetect(int[][] matrix)
        {
            //Search throw the matrix and remove all the points that are none edge points
            var edgePixels = new List<pixelPosition>();
            var noneEdgePoints = new List<pixelPosition>();
            for (int i = 1; i < matrix.Length-1; i++)
            {
                for (int m = 1; m < matrix[i].Length-1; m++)
                {
                    if (matrix[i][m] == 0)
                        continue;

                    var currentPixel = new pixelPosition(m, i);

                    if (isEdgedPixel(matrix, m, i))
                        edgePixels.Add(currentPixel);
                    else
                        noneEdgePoints.Add(currentPixel);
                }
            }
            var changePoints = noneEdgePoints.ToArray();
            foreach (var point in changePoints)
                matrix[point.y][point.x] = 0;
            return edgePixels.ToArray();
        }
        private bool isEdgedPixel(int[][] matrix, int x, int y)
        {
            int noneClusterImages = 0;
            for(int i=-1; i<=1; i++)
            {
                for(int m=-1; m<=1; m++)
                {
                    int px = x + i;
                    int py = y + m;
                    if (px < 0 || py < 0 || px > matrix[0].Length - 1 || py > matrix.Length - 1)
                        continue;
                    if (matrix[py][px] != 1)
                        noneClusterImages++;
                }
            }
            return noneClusterImages >= edgeDetectionMin;
        }


        /// <summary>
        /// This brings all the posible valid pairs of two point, 
        /// we use this to find the linear functions of the edges
        /// </summary>
        /// <param name="matrix"></param>
        /// <param name="pixels"></param>
        /// <returns> all posible pairs of valid pixels on the rectangle</returns>
        private pixelPair[] getPixelPairs(int[][] matrix, pixelPosition[] pixels)
        {
            var pixelAngles = new List<pixelPair>();
            for (int i = 0; i < pixels.Length; i++)
            {
                for (int m = i + 1; m < pixels.Length; m++)
                {
                    if (isValidPair(matrix, pixels[i], pixels[m]))
                    {
                        // this means we found a valid pair of points
                        var currentPixelAngle = new pixelPair()
                        {
                            p1 = pixels[i],
                            p2 = pixels[m]
                        };
                        currentPixelAngle.distance = calculatePointDistance(currentPixelAngle.p1, currentPixelAngle.p2);
                        pixelAngles.Add(currentPixelAngle);
                    }
                }
            }
            return pixelAngles.ToArray();
        }

        private bool isValidPair(int[][] matrix, pixelPosition p1, pixelPosition p2)
        {
            int searchX = (p1.x + p2.x) / 2;
            int searchY = (p1.y + p2.y) / 2;
            
            if (p1.x - p2.x == 0 || p1.y - p2.y == 0 || calculatePointDistance(p1, p2) < 900)
                return false;
            return isPointNear(matrix, searchX, searchY);
        }

        /// <summary>
        /// This function gives a similarity score for a two pairs of points
        /// </summary>
        /// <param name="pair1"></param>
        /// <param name="pair2"></param>
        /// <returns>similarity score</returns>
        private double getPairSimilarityScore(pixelPair pair1, pixelPair pair2)
        {
            double d1 = Math.Min(calculatePointDistance(pair1.p1, pair2.p1), calculatePointDistance(pair1.p1, pair2.p2));
            double d2 = Math.Min(calculatePointDistance(pair1.p2, pair2.p1), calculatePointDistance(pair1.p2, pair2.p2));
            double distance = (d1 + d2) / 2.0;
            double angleScore = Math.Abs(pair1.calculateAngle() - pair2.calculateAngle());

            return (angleScore * 0.5) + (0.5 * distance);
        }

        //search around point
        private bool isPointNear(int[][] matrix, int x, int y)
        {
            for(int i=-searchDistance; i<searchDistance; i++)
            {
                for(int m=-searchDistance; m<searchDistance; m++)
                {
                    int px = x + i;
                    int py = y + m;
                    if (px < 0 || py < 0 || px > matrix[0].Length - 1 || py > matrix.Length - 1)
                        continue;
                    
                    if (matrix[py][px] == 1)
                        return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Cluster points that are a part of the same square
        /// </summary>
        /// <param name="pixels"></param>
        /// <param name="imageWidth"></param>
        /// <param name="imageHeight"></param>
        /// <returns>rectangle pixel that contains all the pixels for that square</returns>
        public square[] clusterRectangles(pixelPosition[] pixels, int imageWidth, int imageHeight)
        {
            if (pixels.Length == 0)
                return null;
            //create a matrix of the given pixels
            int[][] matrix = getMatrix(pixels, imageWidth, imageHeight);
            // this adds ids to clusters in the matrix
            floodFill(matrix);

            return getRectangles(matrix);
        }


        private square[] getRectangles(int[][] matrix)
        {
            var rectangles = new List<square>();
            for(int i=0; i<matrix.Length; i++)
            {
                for(int m=0; m<matrix[i].Length; m++)
                {
                    if (matrix[i][m] == 0)//this is a black space
                        continue;
                    
                    var id = matrix[i][m];
                    var newPoint = new pixelPosition(m, i);
                    var match = rectangles
                        .Where(p => p.id == id);

                    if (match.Count() >= 1)
                    {
                        match.ElementAt(0)
                            .addPixel(newPoint);
                    }
                    else
                    {
                        var newRectangle = new square(id);
                        newRectangle.addPixel(newPoint);
                        rectangles.Add(newRectangle);
                    }
                }
            }
            foreach(var rect in rectangles)
                rect.convertList();

            return rectangles.ToArray();
        }

        /// <summary>
        /// Flood fill algorithem that clusters togather a group od points
        /// </summary>
        /// <param name="matrix"></param>
        private void floodFill(int[][] matrix)
        {
            int currentClusterId = 2;
            for(int i=0; i<matrix.Length; i++)
            {
                for(int m=0; m<matrix[i].Length; m++)
                {
                    if (matrix[i][m] == 1)
                        fillPosition(matrix, new pixelPosition(m, i), currentClusterId++);
                }
            }
        }

        private void fillPosition(int[][] matrix, pixelPosition point, int value)
        {
            var nextChecks = new Stack<pixelPosition>();
            var visted = new HashSet<int>();
            pixelPosition current = point;
            handlePoint(matrix, nextChecks, current, value, visted);
            while(nextChecks.Count > 0)
            {
                current = nextChecks.Pop();
                visted.Remove(current.x + (current.y * matrix.Length));
                handlePoint(matrix, nextChecks, current, value, visted);
            }
        }

        private void handlePoint(int[][] matrix, Stack<pixelPosition> nextChecks, pixelPosition p, int value, HashSet<int> visted)
        {
            var newPoints = getValidPixelNighbors(p.x, p.y, matrix, visted);
            foreach (var point in newPoints)
            {
                nextChecks.Push(point);
                visted.Add(point.x + (matrix.Length * point.y));
            }
            matrix[p.y][p.x] = value;

        }

        private pixelPosition[] getValidPixelNighbors(int x, int y, int[][] matrix, HashSet<int> visted)
        {
            var points = new List<pixelPosition>();
            for(int i=-1; i<=1; i++)
            {
                for(int m=-1; m<=1; m++)
                {
                    int px = x + i;
                    int py = y + m;
                    if (px < 0 || py < 0 || px > matrix[0].Length - 1 || py > matrix.Length - 1)
                        continue;
                    if (visted.Contains(px + (py * matrix.Length)))
                        continue;
                    if (matrix[py][px] == 1)
                        points.Add(new pixelPosition(px, py));
                }
            }
            return points.ToArray();
        }
        public int[][] getMatrix(pixelPosition[] pixels, int width, int height)
        {
            var matrix = new int[height][];
            for (int i = 0; i < matrix.Length; i++)
            {
                matrix[i] = new int[width];
                for (int m = 0; m < matrix[i].Length; m++)
                    matrix[i][m] = 0;
            }
            foreach(var pixel in pixels)
            {
                matrix[pixel.y][pixel.x] = 1;
            }
            return matrix;
        }
        
        /// <summary>
        /// This function takes in am image path and returns the all corners of the rectangles
        /// that have alpha removed in the image
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns>Array of rectanglePixels -> all 4 corners of all rectangles</returns>
        public square[] find(string filePath, bool isLandScape, double pushBackDistance = 10)
        {
            var bmp = new Bitmap(filePath);
            var pixels = getRectangePositions(bmp);
            //clustering algorithem
            var rects = clusterRectangles(pixels, bmp.Width, bmp.Height);
            //find corners
            rects = rects.Select(p =>getRectangleCorners(p, bmp.Width, bmp.Height, isLandScape))
                .ToArray();

            foreach (var rectangle in rects)
                rectangle?.pushBackEdges(pushBackDistance, isLandScape);

            return rects;
        }
    }
}
