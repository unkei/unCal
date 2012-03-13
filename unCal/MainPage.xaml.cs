using System;
using System.Globalization;
using System.Linq;
using System.IO.IsolatedStorage;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using System.Diagnostics;
using System.Windows.Media.Imaging;
using libWkCal;

namespace unCal
{
    public partial class MainPage : PhoneApplicationPage
    {
        private WeekCalendar wc = new WeekCalendar();
        const string FILE_PREFIX = "cal";
        const int TILE_WIDTH = 200;
        const int TILE_HEIGHT = 200;
        const string LIVETILE_PATH = "Shared\\ShellContent\\unCal.jpg";
        const string LIVETILE_URI = "isostore:/Shared/ShellContent/unCal.jpg";

        AppSettings settings = new AppSettings();

        // Constructor
        public MainPage()
        {
            InitializeComponent();
            genTiles(DateTime.Today.Year);
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            bool isClutureInfoChanged = settings.updateCultureIfChanged(CultureInfo.CurrentCulture.ToString());
            bool isLastUpdateChanged = settings.updateLastUpdatedIfChanged(DateTime.Today);
            bool isColorChanged = settings.updateColorIfChanged(GraphicsHelper.foregroundColor, GraphicsHelper.backgroundColor);
            genTiles(DateTime.Today.Year, isClutureInfoChanged || isColorChanged);
            updateLiveTile((isClutureInfoChanged || isLastUpdateChanged || isColorChanged));

            setTileScroller();
            ApplicationTitle.Text = "w" + wc.getWeekNumber(DateTime.Today) + "\t\t" + DateTime.Today.ToString("D").ToUpper();
            if (CultureInfo.CurrentCulture.ToString().StartsWith("ja") ||
                CultureInfo.CurrentCulture.ToString().StartsWith("zh"))
                ApplicationTitle.Text += " " + CultureInfo.CurrentCulture.DateTimeFormat.DayNames[int.Parse(DateTime.Today.DayOfWeek.ToString("d"))];

            if (liveTile() == null)
                ((ApplicationBarIconButton)ApplicationBar.Buttons[0]).IsEnabled = true; // enable pinButton
            else
                ((ApplicationBarIconButton)ApplicationBar.Buttons[0]).IsEnabled = false; // disable pinButton
        }

        private void pinButton_Click(object sender, EventArgs e)
        {
            addLiveTile(true);
        }

        private void tilePressed(object sender, MouseButtonEventArgs e)
        {
#if DEBUG
            //ScheduledActionService.LaunchForTest("TileUpdate",TimeSpan.FromSeconds(10));
#endif

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

        private void setTileScroller()
        {
            int year = DateTime.Today.Year;
            int month = DateTime.Today.Month;

            const int MONTHS = 12;
            const int MAX_COLS = 2;
            const int MAX_ROWS = (MONTHS / MAX_COLS);

            StackPanel v_sp = tileScroller;
            StackPanel h_sp = null;

            v_sp.Children.Clear();

            v_sp.Children.Add(new StackPanel { Height = 45 });

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
                        string fileName;
                        if (i * MAX_COLS + j + 1 == month)
                            fileName = LIVETILE_PATH;
                        else
                            fileName = imageName(i * MAX_COLS + j + 1, year);
                        using (IsolatedStorageFileStream isstr = isto.OpenFile(fileName, System.IO.FileMode.Open, System.IO.FileAccess.Read))
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
            v_sp.Children.Add(new StackPanel { Height = 25 });
        }

        public string imageName(int month, int year)
        {
            return FILE_PREFIX + month.ToString("d2") + year.ToString("d4") + ".jpg";
        }

        private ShellTile liveTile()
        {
            return ShellTile.ActiveTiles.FirstOrDefault(x => x.NavigationUri.ToString().Contains("DefaultTitle=unCal"));
        }

        private void genTiles(int year, bool force = false)
        {
            for (int i = 0; i < 12; i++)
            {
                wc.createCalendarImage(imageName(i + 1, year), new DateTime(year, i + 1, 1), false, force);
            }
        }

        private void genLiveTile(string filename, bool force=false)
        {
            wc.createCalendarImage(filename, DateTime.Today, true, force);
        }

        private void refreshButton_Click(object sender, EventArgs e)
        {
            genTiles(DateTime.Today.Year, true);
            setTileScroller();
        }

        private void addLiveTile(bool force=false)
        {
            ShellTile TileToFind = liveTile();

            genLiveTile(LIVETILE_PATH, force);

            StandardTileData tileData = new StandardTileData
            {
                BackgroundImage = new Uri(LIVETILE_URI, UriKind.Absolute),
            };

            if (TileToFind == null)
                ShellTile.Create(new Uri("/MainPage.xaml?DefaultTitle=unCal", UriKind.Relative), tileData);
            else
                TileToFind.Update(tileData);

            return;
        }

        private bool updateLiveTile(bool force=false)
        {
            ShellTile TileToFind = liveTile();
            bool ret = false;

            genLiveTile(LIVETILE_PATH, force);

            if (TileToFind != null)
            {
                StandardTileData tileData = new StandardTileData
                {
                    BackgroundImage = new Uri(LIVETILE_URI, UriKind.Absolute),
                };

                TileToFind.Update(tileData);
                ret = true;
            }
            return ret;
        }

    }
}