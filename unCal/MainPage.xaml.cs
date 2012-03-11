using System;
using System.Globalization;
using System.Linq;
using System.IO.IsolatedStorage;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Scheduler;
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
        AppSettings settings = new AppSettings();

        // Constructor
        public MainPage()
        {
            InitializeComponent();
            genTiles(DateTime.Now.Year);
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            bool updated = settings.updateIfChanged(CultureInfo.CurrentCulture.ToString(), DateTime.Today);
            genTiles(DateTime.Now.Year, updated);
            setTileScroller();
            ApplicationTitle.Text = "w" + wc.getWeekNumber(DateTime.Now) + "\t\t" + DateTime.Now.ToString("D");

            //if (liveTile() == null)
            //    pinButton.IsEnabled = true;
            //else
            //    pinButton.IsEnabled = false;
        }

        private ShellTile liveTile()
        {
            return ShellTile.ActiveTiles.FirstOrDefault(x => x.NavigationUri.ToString().Contains("DefaultTitle=unCal"));
        }

        private void pinButton_Click(object sender, EventArgs e)
        {
            ShellTile TileToFind = liveTile();

            wc.createCalendarImage("Shared\\ShellContent\\unCal.jpg", DateTime.Now, true, true);

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
            int year = DateTime.Now.Year;

            const int MONTHS = 12;
            const int MAX_COLS = 2;
            const int MAX_ROWS = (MONTHS / MAX_COLS);

            StackPanel v_sp = tileScroller;
            StackPanel h_sp = null;

            v_sp.Children.Clear();

            v_sp.Children.Add(new StackPanel { Height = 55 });

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
                        using (IsolatedStorageFileStream isstr = isto.OpenFile(imageName(i * MAX_COLS + j + 1, year), System.IO.FileMode.Open, System.IO.FileAccess.Read))
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

        private void tilePressed(object sender, MouseButtonEventArgs e)
        {
#if DEBUG
            ScheduledActionService.LaunchForTest("TileUpdate",TimeSpan.FromSeconds(10));
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

        public string imageName(int month, int year)
        {
            return FILE_PREFIX + month.ToString("d2") + year.ToString("d4") + ".jpg";
        }

        private void genTiles(int year, bool force=false)
        {
            for (int i = 0; i < 12; i++)
            {
                wc.createCalendarImage(imageName(i + 1, year), new DateTime(year, i + 1, 1), true, force);
            }
        }

        private void refreshButton_Click(object sender, EventArgs e)
        {
            genTiles(DateTime.Now.Year, true);
            setTileScroller();
        }
    }
}