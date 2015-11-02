using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Shapes;
using System.Windows.Media.Imaging;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace EmbryoSegmenter.Shapes
{
    public class Circle : DrawingVisual
    {

        //public int circleNo;
        public System.Windows.Point centre;
        public double radius;
        //public Annotation annotation;
        public double lineThickness = 0.5;

        //Draws outline from inside line boundry to outside line boundry
        public void DrawCircleOutline(Color color)
        {
            using (DrawingContext dc = this.RenderOpen())
            {
                EllipseGeometry bigEllips = new EllipseGeometry(centre, radius, radius);
                EllipseGeometry smallEllips = new EllipseGeometry(centre, radius - lineThickness, radius - lineThickness);
                CombinedGeometry ellipseOutline = new CombinedGeometry(GeometryCombineMode.Exclude, bigEllips, smallEllips);
                dc.DrawGeometry(new SolidColorBrush(color), new Pen(new SolidColorBrush(color), 1), ellipseOutline);


            }
        }

        //Draws small ellipse inside line boundries
        public void DrawSolidCircle(Color color)
        {
            using (DrawingContext dc = this.RenderOpen())
            {
                dc.DrawEllipse(new SolidColorBrush(Colors.White), new Pen(new SolidColorBrush(color), 1), centre, radius - lineThickness, radius - lineThickness);
            }
        }
    }
}
