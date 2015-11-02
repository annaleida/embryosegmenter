using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EmbryoSegmenter.Shapes
{
    public class Segment
    {
        public SEGMENT_SHAPE shape = SEGMENT_SHAPE.NONE;
        public int segmentNo;
        public int nbrOfPoints;
        public int centerX;
        public int centerY;
        public int segmentArea;
        public bool inEnclosed;
        public Annotation annotation;

        //Properties shape point
        public List<Point> points;   
    
        //properties shape circle
        public Circle circle;

    }

    //TODO need to save these
    public enum SEGMENT_SHAPE
    {
        NONE = 0,
        SPLINE = 1,
        POINTS = 2,
        CIRCLE = 3
    }

    public enum SEGMENT_SOURCE
    {
        EXPERT = 0,
        RAFFERTY
    }
}
