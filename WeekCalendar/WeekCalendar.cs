using System;
using System.Globalization;
using System.IO.IsolatedStorage;
using System.Windows.Media.Imaging;


namespace libWkCal
{
    public class WeekCalendar
    {
        //enum drawArea { MONTH, WKDAY, WKNUM, DAY };

        const int TILE_WIDTH = 200; //173
        const int TILE_HEIGHT = 200; //173

        const int TOP_MARGIN = 6;
        const int LEFT_MARGIN = 2;
        const int WK_FONTSIZE = 12 + 2;
        const int DAY_FONTSIZE = 16 + 2;
        const int MONTH_FONTSIZE = 20 + 2;
        const int WKDAY_FONTSIZE = 14 + 2;

        const int WK_WIDTH = WK_FONTSIZE + LEFT_MARGIN;
        const int DAY_WIDTH = 22 + 3;

        const int MONTH_HEIGHT = MONTH_FONTSIZE + 6;
        const int WKDAY_HEIGHT = 19 + 2;
        const int DAY_HEIGHT = 19 + 3;

        public int getWeekNumber(DateTime dt)
        {
            int dayOfYear = dt.DayOfYear - 1;
            int offset = getJan1Offset(dt.Year);
            DateTime dec31_last = new DateTime(dt.Year - 1, 12, 31);
            DateTime dec31_this = new DateTime(dt.Year, 12, 31);
            int wk;
            int wk_last = (dec31_this.DayOfYear - 1 + offset) / 7;
            int lastDays = wk_last * 7 - offset;

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

        private int getLastDayOfMonth(DateTime dt)
        {
            DateTime day1 = new DateTime(dt.Year, dt.Month + 1, 1);

            return day1.AddDays(-1).Day;
        }

        public void createCalendarImage(string filename, DateTime dt, bool isHighlightToday = false, bool isForceRecreate=false)
        {
            using (IsolatedStorageFile isto = IsolatedStorageFile.GetUserStoreForApplication())
            {
                if (isForceRecreate || !isto.FileExists(filename))
                {
                    using (IsolatedStorageFileStream isstr = isto.OpenFile(filename, System.IO.FileMode.Create, System.IO.FileAccess.Write))
                    {
                        WriteableBitmap wb = createCalendarImage(dt, isHighlightToday);
                        wb.SaveJpeg(isstr, wb.PixelWidth, wb.PixelHeight, 0, 100);
                    }
                }
            }
        }

        public WriteableBitmap createCalendarImage(DateTime dt, bool isHighlightToday = false)
        {
            WriteableBitmap wb = new WriteableBitmap(TILE_WIDTH, TILE_HEIGHT);

            // Draw background in PhoneAccentColor (default tile background color)
            GraphicsHelper.drawRectangle(wb, 0, 0, TILE_WIDTH, TILE_HEIGHT, GraphicsHelper.backgroundColor);

            // Draw month and year
            GraphicsHelper.drawString(wb, TILE_WIDTH / 2, TOP_MARGIN, dt.ToString("y").ToUpper().Replace(",", " "), GraphicsHelper.StringColor.NORMAL, DAY_FONTSIZE, GraphicsHelper.HAlign.CENTER, "Segoe WP");

            bool isChinese = CultureInfo.CurrentCulture.ToString().StartsWith("zh");

            // Draw weekday
            for (int i = 0; i < 7; i++) // starting from Monday
            {
                string wd = CultureInfo.CurrentCulture.DateTimeFormat.AbbreviatedDayNames[(i + 1) % 7];
                if (wd.Length > 2) wd = wd.Substring(0, 2); // take first 2 letter if long
                if (isChinese) wd = wd.Substring(1, 1); // takes only 2nd letter if Chinese
                int x = DAY_WIDTH * i + DAY_WIDTH - WKDAY_FONTSIZE / 2 + WK_WIDTH + LEFT_MARGIN;
                int y = MONTH_HEIGHT + TOP_MARGIN;
                GraphicsHelper.drawString(wb, x - 2, y + 2/*adjust +2*/, wd, GraphicsHelper.StringColor.NORMAL, WKDAY_FONTSIZE, GraphicsHelper.HAlign.CENTER);
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
                GraphicsHelper.drawString(wb, x + 1/*adj+1*/, y + 3/*adj+3*/, Convert.ToString(wk), GraphicsHelper.StringColor.NORMAL, WK_FONTSIZE, GraphicsHelper.HAlign.CENTER);

                for (int wd = 0; wd < 7; wd++)
                {
                    DateTime d = monday.AddDays(wd);

                    GraphicsHelper.StringColor sc = GraphicsHelper.StringColor.NORMAL;

                    if (d.Month != dt.Month)
                        sc = GraphicsHelper.StringColor.DIMMED;
                    if (isHighlightToday == true && d.Month == dt.Month && d == DateTime.Today)
                        sc = GraphicsHelper.StringColor.HIGHLIGHT;
                    if (wd == 7)
                        sc = sc; // todo for Sunday color;

                    x = DAY_WIDTH * wd + WK_WIDTH; // +LEFT_MARGIN;
                    // same y for wk can be used
                    GraphicsHelper.drawString(wb, x + DAY_WIDTH, y, Convert.ToString(d.Day), sc, DAY_FONTSIZE, GraphicsHelper.HAlign.RIGHT);

                    if (isHighlightToday == true && d.Month == dt.Month && d == DateTime.Today)
                    {
                        // higlight today
                        GraphicsHelper.drawBox(wb, x + 5, y + 2, DAY_WIDTH, DAY_HEIGHT, GraphicsHelper.foregroundColor);
                    }
                }

                if (isHighlightToday == true && wk == thisweek && DateTime.Today.Month == dt.Month)
                {
                    // higlight this week
                    int width = WK_WIDTH + DAY_WIDTH * 7 - 5 * 3;
                    GraphicsHelper.drawRectangle(wb, WK_WIDTH + 5, y + 2, width, DAY_HEIGHT, GraphicsHelper.highlightColor);
                }
            }

            wb.Invalidate();
            return wb;
        }
    }
}
