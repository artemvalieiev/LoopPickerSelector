using System;
using System.Collections;
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

            FrameworkElement tappedFrameworkElement = e.OriginalSource as FrameworkElement;

            if (tappedFrameworkElement == null) return;

            var tappedItem = tappedFrameworkElement.DataContext;

            if (tappedItem == null) return;

            if (this.SelectedItem != null && this.SelectedItem == tappedItem)
                return;

            this.SelectedItem = tappedItem;

            RefreshRect();

            Point point = e.GetPosition(this);
            FocusPickerSelector(point, FocusSourceType.Tap);


            this.SelectedItem = tappedItem;

            var selectedItemContainer = (PickerSelectorItem)ContainerFromItem(tappedItem);
            GetItemsPanel().ScrollToSelectedIndex(selectedItemContainer, TimeSpan.Zero);

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
                var currentValue = pickerSelectorItem.DataContext;
                pickerSelectorItem.IsSelected = selectedValue.Equals(currentValue);
            }
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


            if (loopListItem == null || item == null)
                return;

            if (this.ItemTemplate == null)
                return;

            // load data templated
            var contentElement = this.ItemTemplate.LoadContent() as FrameworkElement;

            if (contentElement == null)
                return;
            // attach DataContext and Context to the item
            loopListItem.Style = ItemContainerStyle;
            loopListItem.DataContext = item;
            loopListItem.Content = contentElement;
            loopListItem.IsSelected = item == this.SelectedItem;
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

            PickerSelectorItem middleItem = itemsPanel.GetMiddleItem();

            if (middleItem == null || middleItem.DataContext == null)
                return;

            this.SelectedItem = middleItem.DataContext;

            base.OnManipulationCompleted(e);


        }

        /// <summary>
        /// Check item type
        /// </summary>
        protected override bool IsItemItsOwnContainerOverride(object item)
        {
            return item is PickerSelectorItem;

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

            var dataSource = Enumerable.Range(1, 50).Select(i => i.ToString()).ToList();
            this.ItemsSource = dataSource;
            this.SelectedItem = dataSource.First();

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
