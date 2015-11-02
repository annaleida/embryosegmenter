using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using itk;

namespace EmbryoSegmenter.Pipelines
{
    public class TestPipeline: Pipeline
    {

        public TestPipeline()
        {
            
        }

        public void Initialize()
        {
        }

        public void Start()
        {
            itkGradientMagnitudeImageFilter_IF2IF2 ah = itkGradientMagnitudeImageFilter_IF2IF2.New();
            int[] SizeArray = { 512, 512 };
            itkSize LSize = new itkSize(SizeArray);

            int[] IndexArray = { 0, 0 };
            itkIndex LIndex = new itkIndex(IndexArray);

            itkImageRegion region = new itkImageRegion(LSize, LIndex);
            itkImageBase imOut = itkImage_F2.New();
            itkImageBase imIn = itkImage_F2.New();
            imOut.SetRegions(region);
            imOut.Allocate();
            imIn.SetRegions(region);
            imIn.Allocate();
            imIn.Read("C:\\bitmap.bmp");
            ah.SetInput(imIn);
            ah.Update();
            ah.GetOutput(imOut);
            ah.Dispose(); imIn.Dispose(); imOut.Dispose();

        }
    }
}
