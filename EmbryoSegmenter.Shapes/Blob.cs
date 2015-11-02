using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EmbryoSegmenter.Shapes
{
    public class Blob
    {
        public int blobId;
        public List<Segment> segments = new List<Segment>();

        public Blob(int _blobId)
        {
            blobId = _blobId;
        }
    }
}
