using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EmbryoSegmenter.Pipelines
{
    public interface Pipeline
    {
        void Initialize();
        void Start();
        string Filename_get();
        void Filename_set(string p_filename);
        System.Drawing.Bitmap GetImageOut();
        string GetPipelineDescription();
        List<Filters.Filter> GetFilterList();
        void Activate();
        void Deactivate();
    }
}
