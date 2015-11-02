using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EmbryoSegmenter.Pipelines
{
    class EmbryoTrackerPipeline
    {

        public const PipelineType pipelineType = PipelineType.EMBRYOTRACKER;
        //public BitmapSource imOut;
        public string filenameIn;
        public string filenameOut;
        public List<Filters.Filter> filterList;
        public string pipelineDescription;
        
    }
}
