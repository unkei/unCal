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

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            genTiles();

            BitmapImage bmp = new BitmapImage();

            using (IsolatedStorageFile isto = IsolatedStorageFile.GetUserStoreForApplication())
            {
                using (IsolatedStorageFileStream isstr = isto.OpenFile("tile.jpg", System.IO.FileMode.Open, System.IO.FileAccess.Read))
                {
                    bmp.SetSource(isstr);
                }
            }
            tilePreview.Source = bmp;
            Grid.SetRow(tilePreview, 0);
            Grid.SetColumn(tilePreview, 0);

            //Button BackButton = new Button();
            //BackButton.Content = "Back";
            //BackButton.Height = 100;
            //BackButton.Width = 200;

            //ContentPanel.Children.Add(BackButton);
            //Grid.SetRow(BackButton, 0);
            //Grid.SetColumn(BackButton, 1);

            Image tilePreview2 = new Image();
            BitmapImage bmp2 = new BitmapImage();

            using (IsolatedStorageFile isto = IsolatedStorageFile.GetUserStoreForApplication())
            {
                using (IsolatedStorageFileStream isstr = isto.OpenFile("tile2.jpg", System.IO.FileMode.Open, System.IO.FileAccess.Read))
                {
                    bmp2.SetSource(isstr);
                }
            }
            tilePreview2.Source = bmp2;
            tilePreview2.Margin = new Thickness(13);

            ContentPanel.Children.Add(tilePreview2);
            Grid.SetRow(tilePreview2, 0);
            Grid.SetColumn(tilePreview2, 1);

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
        const int TEXT_COL_WIDTH  = (TILE_WIDTH  - TILE_MARGIN * 2) / TILE_TEXT_COL;
        const int TEXT_ROW_HEIGHT = (TILE_HEIGHT - TILE_MARGIN * 2) / TILE_TEXT_ROW;

        // cx = 0..7, cy = 0..7
        private void drawString(WriteableBitmap wb, int cx, int cy, string str, Color color, int fontsize = 14, HAlign halign = HAlign.LEFT, int xadj=0, int yadj=0)
        {
            TextBlock tb = new TextBlock();
            tb.Text = str;
            tb.FontSize = fontsize;
            tb.Foreground = new SolidColorBrush(color);

            int x = TEXT_COL_WIDTH  * cx + TILE_MARGIN;
            int y = TEXT_ROW_HEIGHT * cy + TILE_MARGIN;
            switch (halign)
            {
                case HAlign.CENTER:
                    x += (TEXT_COL_WIDTH - (int)tb.ActualWidth) / 2;
                    break;
                case HAlign.RIGHT:
                    x += TEXT_COL_WIDTH - (int)tb.ActualWidth;
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

        private bool isJan1wk1(int year)
        {
            return (getJan1Offset(year) >= 0) ? true : false;
        }

        private bool isDec31wk1(int year)
        {
            return isJan1wk1(year + 1);
        }

        private int getJan1Offset(int year)
        {
            DateTime jan1 = new DateTime(year, 1, 1);
            int jan1Weekday = (int)(jan1.DayOfWeek + 6) % 7;

            // Fr:-3, Sa:-2, Su:-1, Mo:0, Tu:1, We:2, Th:3 depending of weekday of Jan1
            return (jan1Weekday < 4) ? jan1Weekday : jan1Weekday - 7;
        }

        private int getWeekNumber(DateTime dt)
        {
            int dayOfYear = dt.DayOfYear - 1;
            int offset = getJan1Offset(dt.Year);
            DateTime dec31_last = new DateTime(dt.Year - 1, 12, 31);
            DateTime dec31_this = new DateTime(dt.Year,     12, 31);
            int wk;
            int wk_last = (dec31_this.DayOfYear - 1 + offset) / 7;
            int lastDays = (wk_last - 1) *  7 - offset;

            if (dayOfYear < 3 && offset < 0) // Jan is not wk1
            {
                int dayOfYear_dec31 = dec31_this.DayOfYear - 1;
                int offset_lastyear = getJan1Offset(dt.Year - 1);
                wk = (dayOfYear_dec31 + offset_lastyear) / 7;
            }
            else if (dayOfYear >= lastDays && isDec31wk1(dt.Year))
            {
                wk = 0;
            }
            else
            {
                wk = (dayOfYear + offset) / 7;
            }

            return wk;
        }

        private int getLastDayOfMonth(DateTime dt)
        {
            DateTime day1 = new DateTime(dt.Year, dt.Month + 1, 1);

            return day1.AddDays(-1).Day;
        }

        public bool createCalendarImage(string filename, DateTime dt)
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

            // Draw month and year
            Color fc = getColor("PhoneForegroundColor");
            drawString(wb, TILE_TEXT_COL / 2 - 1, 0, dt.ToString("y"), fc, 16, HAlign.CENTER, TEXT_COL_WIDTH/2);

            // Draw weekday
            for (int i = 0; i < 7; i++) // starting from Monday
            {
                string wd = CultureInfo.CurrentCulture.DateTimeFormat.AbbreviatedDayNames[(i+1)%7];
                if (wd.Length > 2) wd = wd.Substring(0,2); // take first 2 letter if long
                drawString(wb, i+1, 1, wd, fc, 14, HAlign.CENTER, TEXT_COL_WIDTH/10);
            }

            DateTime day1 = new DateTime(dt.Year, dt.Month, 1);
            int day1Weekday = (int)(day1.DayOfWeek + 6) % 7;

            int day1OfYear = day1.DayOfYear;
            DateTime lastDate = day1.AddMonths(1).AddDays(-1);
            int lastday = lastDate.Day;

            int wk = getWeekNumber(day1) + 1; // 0 is wk1
            int y;

            // Draw week numbers and dates
            for (y = 0; y < 6; y++)
            {
                int d = 0;
                drawString(wb, 0, y + 2, Convert.ToString(wk), fc, 14, HAlign.CENTER, 0, TEXT_ROW_HEIGHT/10);
                if (day1OfYear < 4 && wk > 50) wk = 1;
                else wk++;

                for (int x = 0; x < 7; x++)
                {
                    d = y * 7 + x - day1Weekday;
                    if (0 <= d && d < lastday)
                    {
                        drawString(wb, x + 1, y + 2, Convert.ToString(d + 1), fc, 17, HAlign.RIGHT, -1);
                    }
                }
                if (d + 1 >= lastday) break;
            }

            bool isLine6Used = (y >= 5)? true: false;

            wb.Invalidate();

            using (IsolatedStorageFile isto = IsolatedStorageFile.GetUserStoreForApplication())
            {
                using (IsolatedStorageFileStream isstr = isto.OpenFile(filename, System.IO.FileMode.Create, System.IO.FileAccess.Write))
                {
                    wb.SaveJpeg(isstr, wb.PixelWidth, wb.PixelHeight, 0, 100);
                }
            }

            return isLine6Used;
        }
        
        private void pinButton_Click(object sender, EventArgs e)
        {
            ShellTile TileToFind = ShellTile.ActiveTiles.FirstOrDefault(x => x.NavigationUri.ToString().Contains("DefaultTitle=unCal"));

            bool isLine6Used = createCalendarImage("Shared\\ShellContent\\unCal.jpg", DateTime.Now);

            if (TileToFind == null)
            {
                StandardTileData tileData = new StandardTileData
                {
                    BackgroundImage = new Uri("isostore:/Shared/ShellContent/unCal.jpg", UriKind.Absolute),
                };
                tileData.Title = (isLine6Used) ? "                 unCal" : "unCal";

                ShellTile.Create(new Uri("/MainPage.xaml?DefaultTitle=unCal", UriKind.Relative), tileData);
            }
        }

        private void genTiles()
        {
            createCalendarImage("tile.jpg", DateTime.Now);
            createCalendarImage("tile2.jpg", new DateTime(DateTime.Now.Year, DateTime.Now.AddMonths(1).Month, DateTime.Now.Day));

            //for (int i = 0; i < 12; i++)
            //{
            //    bool isLine6Used = createCalendarImage("Shared\\ShellContent\\unCal" + i + ".jpg", new DateTime(DateTime.Now.Year, i + 1, 1));
            //}
        }
    }
}