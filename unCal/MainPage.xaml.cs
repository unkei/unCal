using System;
using System.Globalization;
using System.Linq;
using System.IO.IsolatedStorage;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using System.Diagnostics;
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

            setTileScroller();

            ApplicationTitle.Text = "w" + getWeekNumber(DateTime.Now) + "\t\t" + DateTime.Now.ToString("D");
        }

        private Color getColor(string colorname)
        {
            MediaColor mc = (MediaColor)System.Windows.Application.Current.Resources[colorname];
            return new Color { A = mc.A, R = mc.R, G = mc.G, B = mc.B };
        }

        enum HAlign {CENTER, LEFT, RIGHT};
        enum StringColor { DIMMED, NORMAL, HIGHLIGHT };
        enum drawArea { MONTH, WKDAY, WKNUM, DAY };
        const int TILE_WIDTH = 200; //173
        const int TILE_HEIGHT = 200; //173

        const int TOP_MARGIN = 6;
        const int LEFT_MARGIN = 2;
        const int WK_FONTSIZE = 12+2;
        const int DAY_FONTSIZE = 16+2;
        const int MONTH_FONTSIZE = 20+2;
        const int WKDAY_FONTSIZE = 14+2;
        
        const int WK_WIDTH = WK_FONTSIZE + LEFT_MARGIN;
        const int DAY_WIDTH = 22+3;

        const int MONTH_HEIGHT = MONTH_FONTSIZE + 6;
        const int WKDAY_HEIGHT = 19+2;
        const int DAY_HEIGHT = 19+3;

        private void drawString(WriteableBitmap wb, int x, int y, string str, StringColor sc, int fontsize = 14, HAlign halign = HAlign.LEFT, string fnt="Segoe WP Light")
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
                tb.TextDecorations = TextDecorations.Underline;
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
            int lastDays = wk_last *  7 - offset;

            if (offset < 0 && dayOfYear + offset < 0) // given date falls into last year
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

            return wk + 1; // +1 to start wk from wk1 (not wk0)
        }

        private int getLastDayOfMonth(DateTime dt)
        {
            DateTime day1 = new DateTime(dt.Year, dt.Month + 1, 1);

            return day1.AddDays(-1).Day;
        }

        public void createCalendarImage(string filename, DateTime dt, bool isHighlightToday=true)
        {
            WriteableBitmap wb = new WriteableBitmap(TILE_WIDTH, TILE_HEIGHT);

            // Draw background in PhoneAccentColor (default tile background color)
            Color ac = getColor("PhoneAccentColor");
            Path rect = new Path();
            rect.Stroke = new SolidColorBrush(Colors.Transparent);
            rect.Fill = new SolidColorBrush(ac);
            rect.StrokeThickness = 0;
            RectangleGeometry rectGeometry = new RectangleGeometry();
            rectGeometry.Rect = new Rect(0, 0, TILE_WIDTH, TILE_HEIGHT);
            rect.Data = rectGeometry;
            wb.Render(rect, null);

            // Draw month and year
            drawString(wb, TILE_WIDTH / 2, TOP_MARGIN, dt.ToString("y").ToUpper().Replace(",", " "), StringColor.NORMAL, DAY_FONTSIZE, HAlign.CENTER, "Segoe WP");

            // Draw weekday
            for (int i = 0; i < 7; i++) // starting from Monday
            {
                string wd = CultureInfo.CurrentCulture.DateTimeFormat.AbbreviatedDayNames[(i+1)%7];
                if (wd.Length > 2) wd = wd.Substring(0,2); // take first 2 letter if long
                int x = DAY_WIDTH * i + DAY_WIDTH - WKDAY_FONTSIZE / 2 + WK_WIDTH + LEFT_MARGIN;
                int y = MONTH_HEIGHT + TOP_MARGIN;
                drawString(wb, x-2, y + 2/*adjust +2*/, wd, StringColor.NORMAL, WKDAY_FONTSIZE, HAlign.CENTER);
            }

            DateTime day1m = new DateTime(dt.Year, dt.Month, 1);
            int day1mWeekday = (int)(day1m.DayOfWeek + 6) % 7;
            DateTime day1w = day1m.AddDays(-day1mWeekday);
            int thisweek = getWeekNumber(DateTime.Now);

            // Draw week numbers and dates
            for (int w = 0; w < 6; w++)
            {
                DateTime monday = day1w.AddDays(w * 7);
                int wk = getWeekNumber(monday);
                int x = WK_WIDTH / 2; // +LEFT_MARGIN;
                int y = DAY_HEIGHT * w + MONTH_HEIGHT + WKDAY_HEIGHT + TOP_MARGIN;
                drawString(wb, x+1/*adj+1*/, y+3/*adj+3*/, Convert.ToString(wk), StringColor.NORMAL, WK_FONTSIZE, HAlign.CENTER);

                for (int wd = 0; wd < 7; wd++)
                {
                    DateTime d = monday.AddDays(wd);

                    StringColor sc = StringColor.NORMAL;

                    if (d.Month != dt.Month)
                        sc = StringColor.DIMMED;
                    if (isHighlightToday == true && d.Month == dt.Month && d == DateTime.Today)
                        sc = StringColor.HIGHLIGHT;
                    if (wd == 7)
                        sc = sc; // todo for Sunday color;

                    x = DAY_WIDTH * wd + DAY_WIDTH + WK_WIDTH; // +LEFT_MARGIN;
                    // same y for wk can be used
                    drawString(wb, x, y, Convert.ToString(d.Day), sc, DAY_FONTSIZE, HAlign.RIGHT);
                }

                if (wk == thisweek)
                {

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

            createCalendarImage("Shared\\ShellContent\\unCal.jpg", DateTime.Now, true);

            StandardTileData tileData = new StandardTileData
            {
                BackgroundImage = new Uri("isostore:/Shared/ShellContent/unCal.jpg", UriKind.Absolute),
            };
            //tileData.Title = (isLine6Used) ? "                 unCal" : "unCal";

            if (TileToFind == null)
                ShellTile.Create(new Uri("/MainPage.xaml?DefaultTitle=unCal", UriKind.Relative), tileData);
            else
                TileToFind.Update(tileData);
        }

        private void setTileScroller()
        {
            genTiles(DateTime.Now.Year);

            const int MONTHS = 12;
            const int MAX_COLS = 2;
            const int MAX_ROWS = (MONTHS / MAX_COLS);

            StackPanel v_sp = tileScroller;
            StackPanel h_sp = null;

            v_sp.Children.Clear();

            for (int i = 0; i < MAX_ROWS; i++)
            {
                h_sp = new StackPanel();
                h_sp.Orientation = System.Windows.Controls.Orientation.Horizontal;

                for (int j = 0; j < MAX_COLS; j++)
                {
                    Image tileImage = new Image();
                    BitmapImage bmp = new BitmapImage();

                    using (IsolatedStorageFile isto = IsolatedStorageFile.GetUserStoreForApplication())
                    {
                        using (IsolatedStorageFileStream isstr = isto.OpenFile("tile" + (i * MAX_COLS + j + 1) + ".jpg", System.IO.FileMode.Open, System.IO.FileAccess.Read))
                        {
                            bmp.SetSource(isstr);
                        }
                    }
                    tileImage.Width = TILE_WIDTH;
                    tileImage.Height = TILE_HEIGHT;
                    tileImage.Source = bmp;
                    tileImage.Margin = new Thickness(13, 0, 0, 13);
                    tileImage.Name = "month" + (i * 2 + j + 1);
                    tileImage.MouseLeftButtonDown += new MouseButtonEventHandler(tilePressed);
                    tileImage.RenderTransform = new CompositeTransform();
                    tileImage.Projection = new PlaneProjection();

                    tileImage.RenderTransformOrigin = new Point(0.5, 0.5);
                    h_sp.Children.Add(tileImage);
                }

                v_sp.Children.Add(h_sp);
            }
        }

        private void tilePressed(object sender, MouseButtonEventArgs e)
        {
            Debug.WriteLine("tilePressed");
            int x = (int)(e.GetPosition((UIElement)sender).X / TILE_WIDTH * 3) - 1;
            int y = (int)(e.GetPosition((UIElement)sender).Y / TILE_HEIGHT * 3) - 1;

            if (x == 0 && y == 0)
            {
                tapAnimc.Stop();
                tapAnimc.Children[0].SetValue(Storyboard.TargetNameProperty, ((Image)sender).Name);
                tapAnimc.Children[1].SetValue(Storyboard.TargetNameProperty, ((Image)sender).Name);
                tapAnimc.Begin();
            }
            else
            {
                tapAnim.Stop();
                tapAnim.Children[0].SetValue(Storyboard.TargetNameProperty, ((Image)sender).Name);
                ((DoubleAnimationUsingKeyFrames)tapAnim.Children[0]).KeyFrames[0].SetValue(EasingDoubleKeyFrame.ValueProperty, 15.0 * y);
                tapAnim.Children[1].SetValue(Storyboard.TargetNameProperty, ((Image)sender).Name);
                ((DoubleAnimationUsingKeyFrames)tapAnim.Children[1]).KeyFrames[0].SetValue(EasingDoubleKeyFrame.ValueProperty, 15.0 * -x);
                tapAnim.Begin();
            }
        }

        private void genTiles(int year)
        {
            for (int i = 0; i < 12; i++)
            {
                createCalendarImage("tile" + (i + 1) + ".jpg", new DateTime(year, i + 1, 1));
            }
        }
    }
}