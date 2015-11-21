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
        private LoopItemsPanel _itemsPanel;
        public Rect RectPosition { get; set; }

        public Boolean IsFocused
        {
            get { return (Boolean)GetValue(IsFocusedProperty); }
            set { SetValue(IsFocusedProperty, value); }
        }

        public static readonly DependencyProperty IsFocusedProperty =
            DependencyProperty.Register("IsFocused", typeof(Boolean), typeof(PickerSelector),
                new PropertyMetadata(false, OnIsFocusedChanged));

        private static void OnIsFocusedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue == null || (e.NewValue == e.OldValue))
                return;

            PickerSelector pickerSelector = (PickerSelector)d;
            bool isFocused = (bool)e.NewValue;
            pickerSelector.UpdateIsFocusedItems(isFocused);
        }

        public object SelectedItem
        {
            get { return (object)GetValue(SelectedItemProperty); }
            set { SetValue(SelectedItemProperty, value); }
        }

        public static readonly DependencyProperty SelectedItemProperty =
            DependencyProperty.Register("SelectedItem", typeof(object), typeof(PickerSelector), new PropertyMetadata(null, UpdateSelectedItem));

        private static void UpdateSelectedItem(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var pickerSelector = (PickerSelector)d;
            pickerSelector.UpdateIsSelectedItems(e.NewValue);
        }

        public PickerSelector()
        {
            DefaultStyleKey = typeof(PickerSelector);

        }

        private void UpdateIsFocusedItems(bool isFocused)
        {
            if (Items == null || !Items.Any())
                return;

            if (_itemsPanel == null)
                return;

            foreach (PickerSelectorItem child in _itemsPanel.Children)
                child.IsFocused = isFocused;

        }

        private void UpdateIsSelectedItems(object selectedValue)
        {
            if (Items == null || !Items.Any())
                return;

            if (_itemsPanel == null || selectedValue == null)
                return;

            foreach (PickerSelectorItem pickerSelectorItem in _itemsPanel.Children)
            {
                var currentValue = pickerSelectorItem.DataContext;
                pickerSelectorItem.IsSelected = selectedValue.Equals(currentValue);
            }
        }

        private void RefreshRect()
        {
            RectPosition =
                TransformToVisual(this)
                    .TransformBounds(new Rect(0, 0, DesiredSize.Width, DesiredSize.Height));
        }

        protected override void PrepareContainerForItemOverride(DependencyObject element, object item)
        {
            if (DesignMode.DesignModeEnabled)
                return;

            _itemsPanel = this.GetVisualDescendent<LoopItemsPanel>();

            // get the item
            PickerSelectorItem loopListItem = element as PickerSelectorItem;


            if (loopListItem == null || item == null)
                return;

            if (ItemTemplate == null)
                return;

            // load data templated
            var contentElement = ItemTemplate.LoadContent() as FrameworkElement;

            if (contentElement == null)
                return;
            // attach DataContext and Context to the item
            loopListItem.Style = ItemContainerStyle;
            loopListItem.DataContext = item;
            loopListItem.Content = contentElement;
            loopListItem.IsSelected = item == SelectedItem;
            loopListItem.IsFocused = IsFocused;

        }

        protected override void OnManipulationCompleted(ManipulationCompletedRoutedEventArgs e)
        {
            LoopItemsPanel itemsPanel = e.Container as LoopItemsPanel;

            if (itemsPanel == null)
                return;

            PickerSelectorItem middleItem = itemsPanel.GetMiddleItem();

            if (middleItem == null || middleItem.DataContext == null)
                return;

            SelectedItem = middleItem.DataContext;

            base.OnManipulationCompleted(e);

        }

        protected override bool IsItemItsOwnContainerOverride(object item)
        {
            return item is PickerSelectorItem;

        }

        protected override void OnManipulationStarted(ManipulationStartedRoutedEventArgs e)
        {
            if (!IsFocused)
            {
                IsFocused = true;
            }

            LoopItemsPanel itemsPanel = e.Container as LoopItemsPanel;
            if (itemsPanel == null)
                return;

            RefreshRect();
        }

        protected override DependencyObject GetContainerForItemOverride()
        {
            return new PickerSelectorItem { PickerSelector = this };
        }

        protected override void OnTapped(TappedRoutedEventArgs e)
        {
            if (!IsFocused)
            {
                IsFocused = true;
                return;
            }

            var tappedFrameworkElement = e.OriginalSource as FrameworkElement;

            if (tappedFrameworkElement == null) return;

            var tappedItem = tappedFrameworkElement.DataContext;

            if (SelectedItem != null &&
                tappedItem != null &&
                SelectedItem == tappedItem)
                return;

            SelectedItem = tappedItem;

            RefreshRect();

            if (_itemsPanel != null)
            {
                var selectedItemContainer = (PickerSelectorItem)ContainerFromItem(tappedItem);
                _itemsPanel.ScrollToSelectedIndex(selectedItemContainer, TimeSpan.Zero);
            }
        }

        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
        }

        protected override void OnItemsChanged(object e)
        {
            base.OnItemsChanged(e);
            this.SelectedItem = this.Items != null && this.Items.Any() ? this.Items[0] : null;
        }
    }
}
