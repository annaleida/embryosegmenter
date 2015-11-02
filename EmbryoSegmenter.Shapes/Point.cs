using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace EmbryoSegmenter.Shapes
{
    public class Point : DrawingVisual
    {
        public double X;
        public double Y;
        public double Z;
        
        public void Draw(Color color)
        {
            using (DrawingContext dc = this.RenderOpen())
            {
                // Fill rects
                System.Windows.Point p = new System.Windows.Point(); p.X = (int)X; p.Y = (int)Y;
                System.Windows.Point p2 = new System.Windows.Point(); p2.X = (int)X + 1; p2.Y = (int)Y + 1;
                dc.DrawLine(new Pen(new SolidColorBrush(color), 1), p, p2);
                
            }
        }
    }
}
