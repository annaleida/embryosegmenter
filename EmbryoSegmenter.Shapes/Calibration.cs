using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EmbryoSegmenter.Shapes
{
    public class Calibration
    {
        public double series_timeInterval;
        public string series_timeInterval_unit;
        public double stack_zInterval;
        public string stack_zInterval_unit;
        public double slice_xPixelDistance;
        public double slice_yPixelDistance;
        public string slice_xPixelDistance_unit;
        public string slice_yPixelDistance_unit;
    }
}
