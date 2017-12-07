using System;
using System.Collections.Generic;
using System.Windows;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GestureGis2
{
    public class SketchProcessor
    {
        public SketchProcessor()
        {

        }

        private Double resampleSpacing(List<Point> sketch)
        {
            if (sketch == null)
            {
                return 0.0;
            }

            Point point0 = sketch[0];
            Double minX = sketch[0].X;
            Double minY = sketch[0].Y;
            Double maxX = sketch[0].X;
            Double maxY = sketch[0].Y;

            for (int i = 0; i < sketch.Count; i++)
            {
                Point pt = sketch[i];
                if (pt.X > maxX) { maxX = pt.X; }
                if (pt.X < minX) { minX = pt.X; }
                if (pt.Y > maxY) { maxY = pt.Y; }
                if (pt.Y < minY) { minY = pt.Y; }
            }

            Double centerX = minX + (maxX - minX) / 2;
            Double centerY = minY + (maxY - minY) / 2;

            Point topLeft = new Point(minX, maxY);
            Point topRight = new Point(maxX, maxY);
            Point bottomLeft = new Point(minX, minY);
            Point bottomRight = new Point(maxX, minY);

            Double diagonal = calcDistance(topLeft.X, bottomRight.X, topLeft.Y, bottomRight.Y);
            Double spacing = diagonal / 40;

            return spacing;
        }

        private Double calcDistance(Double x1, Double x2, Double y1, Double y2)
        {
            return Math.Sqrt(((x2 - x1) * (x2 - x1)) + ((y2 - y1) * (y2 - y1)));
        }

        public List<Point> getResampledPoints(List<Point> sketch)
        {
            Double S = resampleSpacing(sketch);
            List<Point> newPoints = new List<Point>();
            if (sketch.Count == 0)
            {
                return null;
            }
            Double D = 0.0;
            for (int i = 1; i < sketch.Count; i++)
            {
                Point prev = sketch[i - 1];
                Point curr = sketch[i];
                Double d = calcDistance(prev.X, curr.X, prev.Y, curr.Y);
                if (D + d >= S)
                {
                    Double qx = prev.X + ((S - D) / d) * (curr.X - prev.X);
                    Double qy = prev.Y + ((S - D) / d) * (curr.Y - prev.Y);
                    Point q = new Point(qx, qy);
                    newPoints.Add(q);
                    sketch[i] = q;
                    D = 0.0;
                }
                else
                {
                    D = D + d;
                }
            }
            return newPoints;
        }

    }

}