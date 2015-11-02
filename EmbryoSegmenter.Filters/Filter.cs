using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using itk;

namespace EmbryoSegmenter.Filters
{
    public abstract class Filter
    {
        bool active;
        string description;

        public Filter()
        {
        }

        public virtual void Run(itkImageBase imIn, ref itkImageBase imOut)
        {
        }

        public virtual string GetFilterDescription()
        {
            return description;
        }
    }
}
