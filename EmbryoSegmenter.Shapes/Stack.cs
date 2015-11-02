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
        //public string filename;
        public int nbrOfSlices;
        //public string stackName;
        public int stackNo;
        public STACK_STAGE stage = STACK_STAGE.UNKNOWN;
        public STACK_FRAGMENTATION fragmentation = STACK_FRAGMENTATION.UNKNOWN;

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

    public enum STACK_STAGE
    {
        UNKNOWN = 0,
        ONE_CELL = 1,
        TWO_CELL = 2,
        THREE_CELL = 3,
        FOUR_CELL = 4,
        FIVE_CELL = 5,
        SIX_CELL = 6,
        SEVEN_CELL = 7,
        EIGHT_CELL = 8,
        NINE_CELL = 9,
        TEN_CELL = 10,
        DIVISION = 11,
        FORMING_COMPACTATION = 12,
        COMPACTATION = 13,
        FORMING_BLASTOCOEL = 14,
        BLASTOCOEL = 15
    }

    public enum STACK_FRAGMENTATION
    {
        UNKNOWN = 0,
        GROUP1 = 1, // 0% fragmentation
        GROUP2 = 2, // 1+/-10% fragmentation
        GROUP3 = 3, // 11+/-20% fragmentation
        GROUP4 = 4, // 21+/-50>% fragmentation
        GROUP5 = 5 // >50% fragmentation
    }
}
