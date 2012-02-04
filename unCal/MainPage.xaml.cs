using System;
using System.Globalization;
using System.Linq;
using System.IO.IsolatedStorage;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using MediaColor = System.Windows.Media.Color;

namespace unCal
{
    public partial class MainPage : PhoneApplicationPage
    {
        // Constructor
        public MainPage()
        {
            InitializeComponent();
        }

        private Color getColor(string colorname)
        {
            MediaColor mc = (MediaColor)System.Windows.Application.Current.Resources[colorname];
            return new Color { A = mc.A, R = mc.R, G = mc.G, B = mc.B };
        }

        enum HAlign {CENTER, LEFT, RIGHT};
        const int TILE_WIDTH = 173;
        const int TILE_HEIGHT = 173;
        const int TILE_MARGIN = 3;
        const int TILE_TEXT_ROW = 8;
        const int TILE_TEXT_COL = 8;

        // cx = 0..7, cy = 0..7
        private void drawString(WriteableBitmap wb, int cx, int cy, string str, Color color, int fontsize = 14, HAlign halign = HAlign.LEFT, int xadj=0, int yadj=0)
        {
            TextBlock tb = new TextBlock();
            tb.Text = str;
            tb.FontSize = fontsize;
            tb.Foreground = new SolidColorBrush(color);

            int x = (TILE_WIDTH  - TILE_MARGIN * 2) / TILE_TEXT_COL * cx + TILE_MARGIN;
            int y = (TILE_HEIGHT - TILE_MARGIN * 2) / TILE_TEXT_ROW * cy + TILE_MARGIN;
            switch (halign)
            {
                case HAlign.CENTER:
                    x = TILE_WIDTH / 2 - (int)tb.ActualWidth / 2;
                    break;
                case HAlign.RIGHT:
                    x -= (int)tb.ActualWidth;
                    break;
                case HAlign.LEFT:
                    break;
            }
            x += xadj;
            y += yadj;
            wb.Render(tb, new TranslateTransform() { X = x, Y = y });
        }

        private static string GetLocalizedWeekdayName(DayOfWeek weekday)
        {
            return CultureInfo.CurrentCulture.DateTimeFormat.DayNames[(int)weekday];
        }

        public void createCalendarImage(string filename) //(DateTime d)
        {
            WriteableBitmap wb = new WriteableBitmap(173,173);

            // Draw background in PhoneAccentColor (default tile background color)
            Color ac = getColor("PhoneAccentColor");
            Path rect = new Path();
            rect.Stroke = new SolidColorBrush(Colors.Transparent);
            rect.Fill = new SolidColorBrush(ac);
            rect.StrokeThickness = 0;
            RectangleGeometry rectGeometry = new RectangleGeometry();
            rectGeometry.Rect = new Rect(0, 0, 173, 173);
            rect.Data = rectGeometry;
            wb.Render(rect, null);

            // 
            Color fc = getColor("PhoneForegroundColor");
            drawString(wb, 0, 0, DateTime.Now.ToString("y"), fc, 16, HAlign.CENTER);

            for (int i = 0; i < 7; i++) // starting from Sunday (0 is Sunday)
            {
                drawString(wb, i+2, 1, CultureInfo.CurrentCulture.DateTimeFormat.AbbreviatedDayNames[i], fc, 14, HAlign.RIGHT);
            }

            for (int y = 0; y < 5; y++)
            {
                drawString(wb, 0, y+2, Convert.ToString(y+35), fc, 14, HAlign.LEFT, 0, 2);
                for (int x = 0; x < 7; x++)
                {
                    drawString(wb, x+2, y+2, Convert.ToString(y*7+x+1), fc, 17, HAlign.RIGHT, -1);
                }
            }


            wb.Invalidate();

            using (IsolatedStorageFile isto = IsolatedStorageFile.GetUserStoreForApplication())
            {
                using (IsolatedStorageFileStream isstr = isto.OpenFile(filename, System.IO.FileMode.Create, System.IO.FileAccess.Write))
                {
                    wb.SaveJpeg(isstr, wb.PixelWidth, wb.PixelHeight, 0, 100);
                }
            }
        }

        private void pinButton_Click(object sender, EventArgs e)
        {
            ShellTile TileToFind = ShellTile.ActiveTiles.FirstOrDefault(x => x.NavigationUri.ToString().Contains("DefaultTitle=unCal"));

            createCalendarImage("Shared\\ShellContent\\unCal.jpg");

            if (TileToFind == null)
            {
                StandardTileData tileData = new StandardTileData
                {
                    BackgroundImage = new Uri("isostore:/Shared/ShellContent/unCal.jpg", UriKind.Absolute),
                    Title = "unCal - wk37",
                };

                ShellTile.Create(new Uri("/MainPage.xaml?DefaultTitle=unCal", UriKind.Relative), tileData);
            }

        }
    }
}