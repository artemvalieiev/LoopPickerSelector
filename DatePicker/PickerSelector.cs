using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DatePicker.Common;
using Windows.ApplicationModel;
using Windows.Foundation;
using Windows.UI.Core;
using Windows.UI.Input;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Shapes;
using DatePicker.Controls;

namespace DatePicker
{
    public class PickerSelector : ItemsControl
    {
        internal Boolean InitializationInProgress { get; set; }

        // Type of listbox (Day, Month, Year)
        public DataSourceType DataSourceType { get; set; }
        public YearDataSource YearDataSource { get; set; }
        public MonthDataSource MonthDataSource { get; set; }
        public DayDataSource DayDataSource { get; set; }
        public Rect RectPosition { get; set; }

        // Parent date picker 
        //internal DatePicker DatePicker { get; set; }

        // Items Panel
        private LoopItemsPanel itemsPanel;

        /// <summary>
        /// If Tapped on selected item, prevent animation and visual states
        /// </summary>
        protected override void OnTapped(TappedRoutedEventArgs e)
        {

            if (!this.IsFocused && EnableFirstTapHasFocusOnly)
                return;

            FrameworkElement fxElement = e.OriginalSource as FrameworkElement;

            if (fxElement == null) return;

            var dtw = fxElement.DataContext as DateTimeWrapper;

            if (dtw == null) return;

            if (this.SelectedItem != null && (this.SelectedItem as DateTimeWrapper).DateTime == dtw.DateTime)
                return;

            this.SelectedItem = dtw;

            RefreshRect();

            Point point = e.GetPosition(this);
            FocusPickerSelector(point, FocusSourceType.Tap);


            DateTimeWrapper dateTimeWrapper = this.SelectedItem as DateTimeWrapper;

            this.Value = dateTimeWrapper.DateTime;
            this.CreateOrUpdateItems(((DateTime)dateTimeWrapper.DateTime));
            this.GetItemsPanel().ScrollToSelectedIndex(this.GetSelectedPickerSelectorItem(), TimeSpan.Zero);

        }

        private void RefreshRect()
        {
            this.RectPosition =
                this.TransformToVisual(this)
                    .TransformBounds(new Rect(0, 0, this.DesiredSize.Width, this.DesiredSize.Height));
        }

        /// <summary>
        /// Make a focus or unfocus manually on each selector
        /// </summary>
        private void FocusPickerSelector(Point point, FocusSourceType focusSourceType)
        {
            if (point.X > this.RectPosition.X &&
                point.X < (this.RectPosition.X + this.RectPosition.Width))
            {
                this.IsFocused = true;
            }
        }


        public Boolean IsFocused
        {
            get { return (Boolean)GetValue(IsFocusedProperty); }
            set { SetValue(IsFocusedProperty, value); }
        }

        // Using a DependencyProperty as the backing store for isFocused.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsFocusedProperty =
            DependencyProperty.Register("IsFocused", typeof(Boolean), typeof(PickerSelector),
                new PropertyMetadata(false, OnIsFocusedChanged));

        /// <summary>
        /// When focus is set, just set IsFocused on the itemsPanel
        /// </summary>
        private static void OnIsFocusedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue == null || (e.NewValue == e.OldValue))
                return;

            PickerSelector pickerSelector = (PickerSelector)d;

            bool isFocused = (bool)e.NewValue;

            pickerSelector.UpdateIsFocusedItems(isFocused);

        }


        /// <summary>
        /// Ctor. Register for first LayoutUpdate to set initial DateTime
        /// </summary>
        public PickerSelector()
        {
            if (DesignMode.DesignModeEnabled)
                return;

            // Set style
            this.DefaultStyleKey = typeof(PickerSelector);
            this.InitializationInProgress = true;

        }

        private object selectedItem;

        /// <summary>
        /// Current DateTime selected
        /// </summary>
        public object SelectedItem
        {
            get { return selectedItem; }
            set
            {
                selectedItem = value;
                // update the IsSelected items state
                this.UpdateIsSelectedItems(value);
            }
        }


        /// <summary>
        /// Update is focused or not on items state
        /// </summary>
        private void UpdateIsFocusedItems(bool isFocused)
        {
            if (this.Items == null || this.Items.Count <= 0)
                return;

            if (this.itemsPanel == null)
                return;

            foreach (PickerSelectorItem child in this.itemsPanel.Children)
                child.IsFocused = isFocused;

        }

        /// <summary>
        /// Update selected or not selected items state
        /// </summary>
        private void UpdateIsSelectedItems(object selectedValue)
        {
            if (this.Items == null || this.Items.Count <= 0)
                return;

            if (this.itemsPanel == null)
                return;

            if (selectedValue == null)
                return;

            foreach (PickerSelectorItem pickerSelectorItem in this.itemsPanel.Children)
            {
                DateTimeWrapper currentValue = (DateTimeWrapper)pickerSelectorItem.DataContext;
                pickerSelectorItem.IsSelected = ((DateTimeWrapper)selectedValue).DateTime ==
                                                ((DateTimeWrapper)currentValue).DateTime;
                if (pickerSelectorItem.IsSelected)
                    selectedPickerSelectorItem = pickerSelectorItem;
            }
        }

        private PickerSelectorItem selectedPickerSelectorItem;

        internal PickerSelectorItem GetSelectedPickerSelectorItem()
        {
            return selectedPickerSelectorItem;
        }

        /// <summary>
        /// Overridden. Creates or identifies the element that is used to display the given item.
        /// </summary>
        protected override DependencyObject GetContainerForItemOverride()
        {
            return new PickerSelectorItem { PickerSelector = this };
        }


        /// <summary>
        /// Return ItemsPanel
        /// </summary>
        internal LoopItemsPanel GetItemsPanel()
        {
            return this.itemsPanel;
        }

        /// <summary>
        /// Prepares the specified element to display the specified item.
        /// </summary>
        protected override void PrepareContainerForItemOverride(DependencyObject element, object item)
        {
            if (DesignMode.DesignModeEnabled)
                return;

            itemsPanel = this.GetVisualDescendent<LoopItemsPanel>();

            // get the item
            PickerSelectorItem loopListItem = element as PickerSelectorItem;
            DateTimeWrapper dateTimeWrapper = item as DateTimeWrapper;

            if (loopListItem == null || dateTimeWrapper == null)
                return;

            if (this.ItemTemplate == null) return;

            // load data templated
            var contentElement = this.ItemTemplate.LoadContent() as FrameworkElement;

            if (contentElement == null)
                return;
            // attach DataContext and Context to the item
            loopListItem.Style = ItemContainerStyle;
            loopListItem.DataContext = item;
            loopListItem.Content = contentElement;
            loopListItem.IsSelected = dateTimeWrapper == this.SelectedItem;
            loopListItem.IsFocused = this.IsFocused;

        }

        /// <summary>
        /// Maybe this method is Obsolet : TODO : Test obsoletence
        /// </summary>
        /// <param name="e"></param>
        protected override void OnManipulationCompleted(ManipulationCompletedRoutedEventArgs e)
        {
            LoopItemsPanel itemsPanel = e.Container as LoopItemsPanel;

            if (itemsPanel == null)
                return;

            PickerSelectorItem selectorItem = itemsPanel.GetMiddleItem();

            if (selectorItem == null)
                return;

            DateTimeWrapper dateTimeWrapper = selectorItem.DataContext as DateTimeWrapper;

            if (dateTimeWrapper == null)
                return;

            this.Value = dateTimeWrapper.DateTime;

            PickerSelectorItem middleItem = this.itemsPanel.GetMiddleItem();

            if (middleItem == null) return;

            this.SelectedItem = middleItem.DataContext as DateTimeWrapper;

            base.OnManipulationCompleted(e);


        }

        /// <summary>
        /// Check item type
        /// </summary>
        protected override bool IsItemItsOwnContainerOverride(object item)
        {
            return item is PickerSelectorItem;

        }


        /// <summary>
        /// Get the first available date and the max (28, 29 30 or 31 for Day per example)
        /// </summary>
        private DateTime GetFirstAvailable(DateTime dateTime, out int newMax)
        {
            DateTime firstAvailableDate = dateTime;
            newMax = 0;
            switch (this.DataSourceType)
            {
                case DataSourceType.Year:
                    firstAvailableDate = this.YearDataSource.GetFirstAvailableDate(dateTime);
                    newMax = this.YearDataSource.GetNumberOfItems(dateTime);
                    break;
                case DataSourceType.Month:
                    firstAvailableDate = this.MonthDataSource.GetFirstAvailableDate(dateTime);
                    newMax = this.MonthDataSource.GetNumberOfItems(dateTime);
                    break;
                case DataSourceType.Day:
                    firstAvailableDate = this.DayDataSource.GetFirstAvailableDate(dateTime);
                    newMax = this.DayDataSource.GetNumberOfItems(dateTime);
                    break;
            }

            return firstAvailableDate;
        }

        /// <summary>
        /// Update Items. Used for days
        /// </summary>
        internal void CreateOrUpdateItems(DateTime dateTime)
        {
            if (this.Items == null)
                return;

            DateTimeWrapper selectedDateTimeWrapper = null;
            int newMax;
            DateTime firstAvailableDate = GetFirstAvailable(dateTime, out newMax);

            // Make a copy without any minutes / seconds ...
            DateTimeWrapper newData =
                new DateTimeWrapper(new DateTime(firstAvailableDate.Year,
                    firstAvailableDate.Month,
                    firstAvailableDate.Day));

            // One item is deleted but was selected..
            // Don't forget to reactivate a selected item
            Boolean oneItemMustBeDeletedAndIsSelected = false;

            // If new month have less day than last month
            if (newMax < this.Items.Count)
            {
                int numberOfLastDaysToDelete = this.Items.Count - newMax;
                for (int cpt = 0; cpt < numberOfLastDaysToDelete; cpt++)
                {
                    PickerSelectorItem item =
                        this.ItemContainerGenerator.ContainerFromItem(this.Items[this.Items.Count - 1]) as
                            PickerSelectorItem;

                    if (item == null)
                        continue;

                    if (item.IsSelected)
                        oneItemMustBeDeletedAndIsSelected = true;

                    this.Items.RemoveAt(this.Items.Count - 1);

                }
            }


            for (int i = 0; i < newMax; i++)
            {

                // -----------------------------------------------------------------------------
                // Add or Update Items
                // -----------------------------------------------------------------------------
                if (this.Items.Count <= i)
                {
                    this.Items.Add(newData);
                }
                else
                {
                    // Verify the item already exists
                    var itemDate = ((DateTimeWrapper)this.Items[i]).DateTime;

                    if (itemDate != newData.DateTime)
                        this.Items[i] = newData;
                }

                // -----------------------------------------------------------------------------
                // Get the good selected itm
                // -----------------------------------------------------------------------------
                if (newData.DateTime == dateTime)
                    selectedDateTimeWrapper = newData;

                // -----------------------------------------------------------------------------
                // Get the next data, relative to original wrapper, then relative to firstWrapper
                // -----------------------------------------------------------------------------
                DateTime? nextData = null;

                // Get nex date
                switch (this.DataSourceType)
                {
                    case DataSourceType.Year:
                        nextData = this.YearDataSource.GetNext(dateTime, firstAvailableDate, i + 1);
                        break;
                    case DataSourceType.Month:
                        nextData = this.MonthDataSource.GetNext(dateTime, firstAvailableDate, i + 1);
                        break;
                    case DataSourceType.Day:
                        nextData = this.DayDataSource.GetNext(dateTime, firstAvailableDate, i + 1);
                        break;
                }
                if (nextData == null)
                    break;

                newData = nextData.Value.ToDateTimeWrapper();
            }

            // Set the correct Selected Item
            if (selectedDateTimeWrapper != null)
                this.SelectedItem = selectedDateTimeWrapper;
            else if (oneItemMustBeDeletedAndIsSelected)
                // When 31 was selected and we are on a Month < 31 days (February, April ...)
                this.SelectedItem = (DateTimeWrapper)this.Items[this.Items.Count - 1];
            else
                this.SelectedItem = (DateTimeWrapper)this.Items[0];
        }


        protected override void OnManipulationStarted(ManipulationStartedRoutedEventArgs e)
        {
            LoopItemsPanel itemsPanel = e.Container as LoopItemsPanel;
            if (itemsPanel == null)
                return;

            RefreshRect();
            // fake Position to get the correct PickerSelector
            Point position = new Point(itemsPanel.ParentDatePickerSelector.RectPosition.X + 1,
                itemsPanel.ParentDatePickerSelector.RectPosition.Y + 1);

            FocusPickerSelector(position, FocusSourceType.Manipulation);
        }


        public DateTime Value
        {
            get
            {
                var t = GetValue(ValueProperty);
                if (t == null)
                    SetValue(ValueProperty, DateTime.Today);

                return (DateTime)GetValue(ValueProperty);
            }
            set
            {
                DateTime dt = new DateTime(value.Year, value.Month, value.Day);
                SetValue(ValueProperty, dt);
            }
        }

        // Using a DependencyProperty as the backing store for Value.  This enables animation, styling, binding, etc...
        internal static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register("Value", typeof(DateTime), typeof(PickerSelector),
                new PropertyMetadata(DateTime.Today, OnValueChanged));


        private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var selector = (PickerSelector)d;

            if (selector.InitializationInProgress)
                return;

            if (e.OldValue == null)
                return;

            if (e.NewValue == e.OldValue)
                return;


            if (selector != null)
            {
                selector.SelectedItem = new DateTimeWrapper((DateTime)e.NewValue);
            }
        }


        public Boolean EnableFirstTapHasFocusOnly
        {
            get { return (Boolean)GetValue(EnableFirstTapHasFocusOnlyProperty); }
            set { SetValue(EnableFirstTapHasFocusOnlyProperty, value); }
        }

        // Using a DependencyProperty as the backing store for EnableFirstTapHasFocusOnly.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty EnableFirstTapHasFocusOnlyProperty =
            DependencyProperty.Register("EnableFirstTapHasFocusOnly", typeof(Boolean), typeof(PickerSelector),
                new PropertyMetadata(true));


        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            this.InitializationInProgress = true;



            // Create wrapper on current value
            var wrapper = new DateTimeWrapper(Value);

            // Init Selectors


            this.YearDataSource = new YearDataSource();
            this.DataSourceType = DataSourceType.Year;
            this.CreateOrUpdateItems(wrapper.DateTime);


            this.Visibility = Visibility.Visible;

            this.InitializationInProgress = false;
        }
    }

    public enum ScrollAction
    {
        Down,
        Up
    }
    public enum FocusSourceType
    {
        Tap,
        Pointer,
        Keryboard,
        Manipulation,
        UnFocus
    }
    public enum DataSourceType
    {
        Year,
        Month,
        Day,
        Hour,
        Minute,
        Second
    }

}
