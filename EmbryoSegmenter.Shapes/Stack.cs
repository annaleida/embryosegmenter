using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EmbryoSegmenter.Shapes
{
    public class Stack
    {
        public List<Slice> slices;
        public int segmentCount;
        public string filename;
        public int nbrOfSlices;
        public string stackName;

        public int GetHighestSegmentIDFromAllSlices()
        {
            int highestSegId = -1;
            foreach (Slice slice in slices)
            {
                if (slice.GetHighestSegmentID() > highestSegId)
                {
                    highestSegId = slice.GetHighestSegmentID();
                }
            }
            return highestSegId;
        }
    }
}
