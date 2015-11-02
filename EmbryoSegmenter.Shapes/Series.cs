using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EmbryoSegmenter.Shapes
{
    public class Series
    {
        public List<Stack> stacks;
        public int nbrOfStacks;
        public string seriesName;
        public Calibration calibration;
        public string seg_fileName;
        public SERIES_QUALITY quality;
        public int imageWidth; //XXX save those
        public int imageHeight;
    }

    public enum SERIES_QUALITY
    {
        HIGH = 5,
        MEDIUM_HIGH = 4,
        MEDIUM = 3,
        MEDIUM_LOW = 2,
        LOW = 1,
        UNKNOWN = 0
    }
}
