using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using MediaColor = System.Windows.Media.Color;

namespace libWkCal
{
    public class GraphicsHelper
    {
        public enum HAlign { CENTER, LEFT, RIGHT };
        public enum StringColor { DIMMED, NORMAL, HIGHLIGHT };

        static public Color getColor(string colorname)
        {
            MediaColor mc = (MediaColor)System.Windows.Application.Current.Resources[colorname];
            return new Color { A = mc.A, R = mc.R, G = mc.G, B = mc.B };
        }

        static public void drawString(WriteableBitmap wb, int x, int y, string str, StringColor sc, int fontsize = 14, HAlign halign = HAlign.LEFT, string fnt = "Segoe WP Light")
        {
            TextBlock tb = new TextBlock();
            tb.Text = str;
            tb.FontFamily = new FontFamily(fnt);
            tb.FontSize = fontsize;
            if (sc == StringColor.HIGHLIGHT)
            {
                //tb.FontFamily = new FontFamily("Segoe WP");
                //tb.FontSize = fontsize + 1.0;
                //y--;
                tb.Foreground = new SolidColorBrush(getColor("PhoneForegroundColor"));
                //tb.Foreground = new SolidColorBrush(getColor("PhoneContrastForegroundColor"));
                tb.FontWeight = System.Windows.FontWeights.Bold;
                //tb.TextDecorations = TextDecorations.Underline;
            }
            else
            {
                tb.Foreground = new SolidColorBrush(getColor("PhoneForegroundColor"));
                if (sc == StringColor.DIMMED)
                {
                    tb.Opacity = 0.5;
                }
            }

            switch (halign)
            {
                case HAlign.CENTER:
                    x -= (int)tb.ActualWidth / 2;
                    break;
                case HAlign.RIGHT:
                    x -= (int)tb.ActualWidth;
                    break;
                case HAlign.LEFT:
                    break;
            }
            wb.Render(tb, new TranslateTransform() { X = x, Y = y });

            if (sc == StringColor.HIGHLIGHT)
            {
                // TODO : FIXME, line is not drawn at all ( and even not sure if this is right UI element)
                Line ln = new Line();
                ln.X1 = x;
                ln.Y1 = y + tb.ActualHeight + 2;
                ln.X2 = x + tb.ActualWidth;
                ln.Y2 = y + tb.ActualHeight + 2;
                wb.Render(ln, new TranslateTransform() { X = x, Y = y });
            }
        }

        static public void drawRectangle(WriteableBitmap wb, int x, int y, int w, int h, Color ac)
        {
            Path rect = new Path();
            rect.Stroke = new SolidColorBrush(Colors.Transparent);
            rect.Fill = new SolidColorBrush(ac);
            rect.StrokeThickness = 0;
            RectangleGeometry rectGeometry = new RectangleGeometry();
            rectGeometry.Rect = new Rect(x, y, w, h);
            rect.Data = rectGeometry;
            wb.Render(rect, new MatrixTransform());
        }

        static public void drawBox(WriteableBitmap wb, int x, int y, int w, int h, Color ac)
        {
            Path rect = new Path();
            rect.Stroke = new SolidColorBrush(ac);
            rect.Fill = new SolidColorBrush(Colors.Transparent);
            rect.StrokeThickness = 1;
            RectangleGeometry rectGeometry = new RectangleGeometry();
            rectGeometry.Rect = new Rect(x, y, w, h);
            rect.Data = rectGeometry;
            wb.Render(rect, new MatrixTransform());
        }

    }
}
