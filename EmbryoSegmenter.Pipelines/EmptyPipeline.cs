using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

namespace EmbryoSegmenter.Pipelines
{
    class EmptyPipeline : Pipeline
    {
        public const PipelineType pipelineType = PipelineType.TEST;
        public string filenameIn;
        public string filenameOut;
        public List<Filters.Filter> filterList;
        public string pipelineDescription;

        public EmptyPipeline()
        {
            filterList = new List<Filters.Filter>();
            pipelineDescription = "Empty pipeline";
            _InitializeFilterList();
        }

        private void _InitializeFilterList()
        {
            filterList = new List<Filters.Filter>();
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
            return null;
        }

        public void Initialize()
        {
        }

        public void Start()
        {
        }

        public void Activate()
        {
        }

        public void Deactivate()
        {
        }

    }
}
