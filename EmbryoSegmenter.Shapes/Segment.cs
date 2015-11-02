using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EmbryoSegmenter.Shapes
{
    public class Segment
    {
        public int segmentNo;
        public int nbrOfPoints;
        public List<Point> points;
        public int centerX;
        public int centerY;
        public int segmentArea;
        public bool inEnclosed;
        public Annotation annotation;
    }
}
