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
        //public List<Circle> nuclei;

        public int GetHighestSegmentID()
        {
            int seg_id = 0;
            foreach (Segment seg in segments)
            {
                if (seg.segmentNo > seg_id)
                {
                    seg_id = seg.segmentNo;
                }
            }
            return seg_id;
        }

        /*public int GetHighestNucleiID()
        {
            int nuc_id = 0;
            foreach (Circle nuc in nuclei)
            {
                if (nuc.circleNo > nuc_id)
                {
                    nuc_id = nuc.circleNo;
                }
            }
            return nuc_id;
        }*/
    }
}
