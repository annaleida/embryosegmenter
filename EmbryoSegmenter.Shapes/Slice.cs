using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EmbryoSegmenter.Shapes
{
    public class Slice
    {
        public string fileName;
        public string filepath;
        public int sliceNo;
        public List<Segment> segments;

        public int GetHighestSegmentID()
        {
            int seg_id = -1;
            foreach (Segment seg in segments)
            {
                if (seg.segmentNo > seg_id)
                {
                    seg_id = seg.segmentNo;
                }
            }
            return seg_id;
        }
    }
}
