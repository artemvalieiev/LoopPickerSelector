﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DatePicker.Common;
using DatePicker.Controls;
using Windows.ApplicationModel;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using System.Diagnostics;
using Windows.UI.Core;

// The Templated Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234235

namespace DatePicker
{
    /// <summary>
    /// Represents a page used by the DatePicker control that allows the user to choose a date (day/month/year).
    /// </summary>
    [TemplatePart(Name = PrimarySelectorName, Type = typeof(PickerSelector))]
    public sealed class DatePicker : Control
    {
        private const string PrimarySelectorName = "PART_PrimarySelector";

        private PickerSelector primarySelector;


        internal Boolean InitializationInProgress { get; set; }

        internal static readonly DependencyProperty SmallFontSizeProperty =
            DependencyProperty.Register("SmallFontSize", typeof(Double), typeof(DatePicker), new PropertyMetadata(default(Double)));

        public Double SmallFontSize
        {
            get { return (Double)GetValue(SmallFontSizeProperty); }
            set { SetValue(SmallFontSizeProperty, value); }
        }

        public Boolean EnableFirstTapHasFocusOnly
        {
            get { return (Boolean)GetValue(EnableFirstTapHasFocusOnlyProperty); }
            set { SetValue(EnableFirstTapHasFocusOnlyProperty, value); }
        }

        // Using a DependencyProperty as the backing store for EnableFirstTapHasFocusOnly.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty EnableFirstTapHasFocusOnlyProperty =
            DependencyProperty.Register("EnableFirstTapHasFocusOnly", typeof(Boolean), typeof(DatePicker), new PropertyMetadata(true));



        /// <summary>
        /// Selected value binded
        /// </summary>
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
            DependencyProperty.Register("Value", typeof(DateTime), typeof(DatePicker),
            new PropertyMetadata(DateTime.Today, OnValueChanged));


        static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            DatePicker ctrl = (DatePicker)d;

            if (ctrl.InitializationInProgress)
                return;

            if (e.OldValue == null)
                return;

            if (e.NewValue == e.OldValue)
                return;


            if (ctrl.primarySelector != null)
            {
                ctrl.primarySelector.CreateOrUpdateItems(((DateTime)e.NewValue));
                ctrl.primarySelector.GetItemsPanel().ScrollToSelectedIndex(ctrl.primarySelector.GetSelectedPickerSelectorItem(), TimeSpan.Zero);
            }
        }


        public DatePicker()
        {
            this.DefaultStyleKey = typeof(DatePicker);
            this.InitializationInProgress = true;
        }

        /// <summary>
        /// Get boundaries on each selector
        /// </summary>


        private void RefreshRect()
        {
            if (primarySelector != null)
                primarySelector.RectPosition = primarySelector.TransformToVisual(this).TransformBounds(new Rect(0, 0, primarySelector.DesiredSize.Width, primarySelector.DesiredSize.Height));

        }
        /// <summary>
        /// On Tapped, make focus on the good PickerSelector
        /// </summary>
        protected override void OnTapped(TappedRoutedEventArgs e)
        {
            RefreshRect();

            Point point = e.GetPosition(this);
            FocusPickerSelector(point, FocusSourceType.Tap);

            PickerSelector selector = null;
            if (primarySelector != null && point.X > primarySelector.RectPosition.X &&
                           point.X < (primarySelector.RectPosition.X + primarySelector.RectPosition.Width))
            {
                selector = primarySelector;
            }

            if (selector != null)
            {
                DateTimeWrapper dateTimeWrapper = selector.SelectedItem as DateTimeWrapper;

                if (dateTimeWrapper == null)
                    return;

                this.Value = dateTimeWrapper.DateTime;
            }
        }

        /// <summary>
        /// On Manipulation, make focus on the good PickerSelector
        /// </summary>
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

        }
        /// <summary>
        /// Make a focus or unfocus manually on each selector
        /// </summary>
        private void FocusPickerSelector(Point point, FocusSourceType focusSourceType)
        {
            if (primarySelector != null && point.X > primarySelector.RectPosition.X &&
                point.X < (primarySelector.RectPosition.X + primarySelector.RectPosition.Width))
            {

                if (primarySelector != null)
                    primarySelector.IsFocused = true;
                return;
            }

        }



        /// <summary>
        /// Override of OnApplyTemplate
        /// </summary>
        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            this.InitializationInProgress = true;

            primarySelector = GetTemplateChild(PrimarySelectorName) as PickerSelector;

            // Create wrapper on current value
            var wrapper = new DateTimeWrapper(Value);

            // Init Selectors
            // Set Datasource 
            if (primarySelector != null)
            {
                primarySelector.DatePicker = this;
                primarySelector.YearDataSource = new YearDataSource();
                primarySelector.DataSourceType = DataSourceType.Year;
                primarySelector.CreateOrUpdateItems(wrapper.DateTime);
            }

            this.primarySelector.Visibility = Visibility.Visible;

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
