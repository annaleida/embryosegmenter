using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using itk;


namespace EmbryoSegmenter.Filters
{
    public class TestFilter:Filter
    {
        string description;
        string name;
             
        public TestFilter()
        {
            description = "Test Filter";
            name = "TestFilter";
        }

        public override void Run(itkImageBase imIn, ref itkImageBase imOut)
        {
        }

        public override string GetFilterDescription()
        { return description; }

        public string GetFilterName()
        { return name; }
    }
}
