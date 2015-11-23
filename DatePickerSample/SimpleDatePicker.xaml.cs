using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DatePickerSample.Common;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Basic Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234237

namespace DatePickerSample
{
    public class CalendarData
    {
        public string DayOfWeek { get; set; }
        public string MonthName { get; set; }
        public string DayOfMonth { get; set; }
    }
    /// <summary>
    /// A basic page that provides characteristics common to most applications.
    /// </summary>
    public sealed partial class SimpleDatePicker : DatePickerSample.Common.LayoutAwarePage, INotifyPropertyChanged
    {
        public SimpleDatePicker()
        {
            this.InitializeComponent();
            this.CurrentDateTime = new DateTime(2013, 10, 29);
            this.DataContext = this;
            this.Loaded += async (s, e) =>
            {
                await Task.Delay(3000);

                //________________________________DateCollection___________________________________________________________________________________________
                DateCollection = Enumerable.Range(0, 30).Select(i => new CalendarData() {
                    MonthName = DateTime.Today.AddDays(i).ToString("MMM"),
                    DayOfMonth =DateTime.Today.AddDays(i).ToString("dd"),
                    DayOfWeek = DateTime.Today.AddDays(i).ToString("dddd"),
                }).ToList();
                DateCollection[0] = new CalendarData() { MonthName = "Today" };

                //_________________________________TimeStartCollection Fill____________________________________________________________________________
                TimeStartCollection = Enumerable.Range(0, 96).Select(i => new CalendarData()
                {
                    DayOfMonth = DateTime.Today.AddMinutes(i * 15).ToString("hh:mm"),
                    DayOfWeek = DateTime.Today.AddMinutes(i * 15).ToString("tt")
                }).ToList();
                //________________________________TimeCountCollection Fill_______________________________________________________________________________
                TimeCountCollection = Enumerable.Range(0, 48).Select(i => new CalendarData()
                {
                    DayOfMonth = DateTime.Today.AddMinutes(i * 15).ToString("hh")+" h",
                    DayOfWeek = DateTime.Today.AddMinutes(i * 15).ToString("mm") + " MIN"
                }).ToList();
                for (int i = 1; i < 4; i++)
                {
                    TimeCountCollection[i]=
                    new CalendarData()
                    {
                        DayOfMonth = DateTime.Today.AddMinutes(i * 15).ToString("mm"),
                        DayOfWeek = " MIN"
                    };
                }
                TimeCountCollection.Add(TimeCountCollection[0]);
                TimeCountCollection.RemoveAt(0);
//_______________________________________________________________________________________________________________


                this.PropertyChanged(this, new PropertyChangedEventArgs("DateCollection"));
                this.PropertyChanged(this, new PropertyChangedEventArgs("TimeStartCollection"));
                this.PropertyChanged(this, new PropertyChangedEventArgs("TimeCountCollection"));
            };
        }

        /// <summary>
        /// Populates the page with content passed during navigation.  Any saved state is also
        /// provided when recreating a page from a prior session.
        /// </summary>
        /// <param name="navigationParameter">The parameter value passed to
        /// <see cref="Frame.Navigate(Type, Object)"/> when this page was initially requested.
        /// </param>
        /// <param name="pageState">A dictionary of state preserved by this page during an earlier
        /// session.  This will be null the first time a page is visited.</param>
        protected override void LoadState(Object navigationParameter, Dictionary<String, Object> pageState)
        {
        }

        /// <summary>
        /// Preserves state associated with this page in case the application is suspended or the
        /// page is discarded from the navigation cache.  Values must conform to the serialization
        /// requirements of <see cref="SuspensionManager.SessionState"/>.
        /// </summary>
        /// <param name="pageState">An empty dictionary to be populated with serializable state.</param>
        protected override void SaveState(Dictionary<String, Object> pageState)
        {
        }

        private DateTime currentDateTime;

        public DateTime CurrentDateTime
        {
            get
            {
                return currentDateTime;
            }
            set
            {
                if (value == currentDateTime)
                    return;

                currentDateTime = value;

                if (this.PropertyChanged != null)
                {
                    this.PropertyChanged(this, new PropertyChangedEventArgs("CurrentDateTime"));
                    this.PropertyChanged(this, new PropertyChangedEventArgs("CurrentDateTimeString"));
                }
            }
        }

        public String CurrentDateTimeString
        {
            get
            {
                return currentDateTime.ToString();
            }

        }

        public List<CalendarData> DateCollection { get; set; }

        public List<CalendarData> TimeStartCollection { get; set; }
        public List<CalendarData> TimeCountCollection { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.CurrentDateTime = new DateTime(1976, 10, 23);
        }
    }
}
