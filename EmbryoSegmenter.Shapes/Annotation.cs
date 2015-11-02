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
    public class Annotation : DrawingVisual
    {
        public string text;

        public Annotation(string note)
        {
            text = note;
        }

        public void Draw(Color color, int pos_x, int pos_y)
        {
            using (DrawingContext dc = this.RenderOpen())
            {
                FormattedText formated_text = new FormattedText(text, System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight, Fonts.SystemTypefaces.First(), 10.0, new SolidColorBrush(color));
                System.Windows.Point p = new System.Windows.Point(); p.X = pos_x; p.Y = pos_y;
                dc.DrawText(formated_text, p);
            }
        }
    }
}
