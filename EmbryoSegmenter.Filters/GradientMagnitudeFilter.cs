using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using itk;

namespace EmbryoSegmenter.Filters
{
    public class GradientMagnitudeFilter:Filter
    {
        string description;
        string name;

        public GradientMagnitudeFilter()
        {
            description = "Gradient Magnitude Filter";
            name = "GradientMagnitudeFilter";
        }

        public override string GetFilterDescription()
        { return description; }

        public string GetFilterName()
        { return name; }

        public override void Run(itkImageBase imIn, ref itkImageBase imOut)
        {
            itkGradientMagnitudeImageFilter_IUC2IUC2 ah = itkGradientMagnitudeImageFilter_IUC2IUC2.New();
            int[] SizeArray = { 500, 500 };
            itkSize LSize = new itkSize(SizeArray);

            int[] IndexArray = { 0, 0 };
            itkIndex LIndex = new itkIndex(IndexArray);

            itkImageRegion region = new itkImageRegion(LSize, LIndex);
            //itkImageBase imOut = itkImage_F2.New();
            imOut.SetRegions(region);
            imOut.Allocate();
            imIn.SetRegions(region);
            imIn.Allocate();
            
            ah.SetInput(imIn);
            ah.Update();
            ah.GetOutput(imOut);
            ah.Dispose();
        }
    }
}
