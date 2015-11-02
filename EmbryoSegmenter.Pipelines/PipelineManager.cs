using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EmbryoSegmenter.Pipelines
{
    public static class PipelineManager
    {
        public static void InitializePipeline(string typeOfPipeline)
        {

            switch (typeOfPipeline)
            {
                case "Test_Pipeline":

                    TestPipeline test = new TestPipeline();
                    test.Start();
                    break;
            }
        }
    }
}
