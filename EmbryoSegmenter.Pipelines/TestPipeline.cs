using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using itk;

namespace EmbryoSegmenter.Pipelines
{
    public class TestPipeline: Pipeline
    {

        public const PipelineType pipelineType = PipelineType.TEST;
        public itkImageBase imOut;
        public string filenameIn;
        public string filenameOut;
        public List<Filters.Filter> filterList;
        public string pipelineDescription;
        
        public TestPipeline()
        {
            filterList = new List<Filters.Filter>();
            pipelineDescription = "Test pipeline";
            _InitializeFilterList();
        }

        private void _InitializeFilterList()
        {
            filterList = new List<Filters.Filter>();
            Filters.Filter gmf = new Filters.GradientMagnitudeFilter();
            Filters.Filter tf = new Filters.TestFilter();
            filterList.Add(gmf);
            filterList.Add(tf);
        }


        public List<Filters.Filter> GetFilterList()
        {
            return filterList;
        }

        public string GetPipelineDescription()
        { return pipelineDescription; }

        
        public void _InitializeFilterList(string filterString)
        {

        }

        public string Filename_get()
        {
            return this.filenameOut;
        }

        public void Filename_set(string p_filename)
        {
            this.filenameIn = p_filename;
        }

        public Bitmap GetImageOut()
        {
            return Filters.ImageIO.ConvertItkImageToBitmap(imOut);
        }

        public void Initialize()
        {
        }

        public void Start()
        {
            itkImageBase imIn = Filters.ImageIO.ReadImage(filenameIn);
            imOut = itkImage_UC2.New();
            Filters.GradientMagnitudeFilter gmf= new Filters.GradientMagnitudeFilter();
            gmf.Run(imIn, ref imOut);
            filenameOut = "C:\\EmbryoSegmenter_Temp\\" + Guid.NewGuid() + ".bmp";
            //imOut.Write(filenameOut);
            imIn.Dispose(); 
            //imOut.Dispose();
           
        }
    }
}
