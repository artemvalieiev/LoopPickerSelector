using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DatePicker.Common;
using Windows.Foundation;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;

namespace DatePicker
{

    [TemplateVisualState(Name = "UnFocused", GroupName = "Picker")]
    [TemplateVisualState(Name = "Focused", GroupName = "Picker")]
    [TemplateVisualState(Name = "Selected", GroupName = "Picker")]
    public sealed class PickerSelectorItem : ContentControl
    {
        // Parent picker selector
        internal PickerSelector PickerSelector { get; set; }

        public Rect RectPosition { get; set; }

        public Double GetVerticalPosition()
        {
            return this.RectPosition.Y + this.GetTranslateTransform().Y;
        }

        public Double GetHorizontalPosition()
        {
            return this.RectPosition.X + this.GetTranslateTransform().X;
        }

        public TranslateTransform GetTranslateTransform()
        {
            return (TranslateTransform)this.RenderTransform;
        }

        /// <summary>
        /// ctor
        /// </summary>
        public PickerSelectorItem()
        {
            this.DefaultStyleKey = typeof(PickerSelectorItem);
            this.RenderTransform = new TranslateTransform();
            this.RectPosition = Rect.Empty;

        }

        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            // get grid involved in VisualState transitions
            var grid = this.GetVisualDescendent<Grid>();

            if (grid != null && !this.IsFocused)
                grid.Opacity = this.IsSelected ? 1d : 0d;

            this.ManageVisualStates(false);

        }



        public new Object ContentTemplate
        {
            get { return GetValue(ContentTemplateProperty); }
            set { SetValue(ContentTemplateProperty, value); }
        }

        public static new readonly DependencyProperty ContentTemplateProperty =
            DependencyProperty.Register("ContentTemplate", typeof(Object), typeof(PickerSelectorItem), new PropertyMetadata(null));

        public new Object Content
        {
            get { return GetValue(ContentProperty); }
            set { SetValue(ContentProperty, value); }
        }

        public static new readonly DependencyProperty ContentProperty =
            DependencyProperty.Register("Content", typeof(object), typeof(PickerSelectorItem), new PropertyMetadata(null));


        public Boolean IsSelected
        {
            get { return (Boolean)GetValue(IsSelectedProperty); }
            set { SetValue(IsSelectedProperty, value); }
        }

        public static readonly DependencyProperty IsSelectedProperty =
            DependencyProperty.Register("IsSelected", typeof(Boolean), typeof(PickerSelectorItem), new PropertyMetadata(false, IsSelectedChangedCallback));

        private static void IsSelectedChangedCallback(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs dependencyPropertyChangedEventArgs)
        {
            PickerSelectorItem pickerSelectorItem = (PickerSelectorItem)dependencyObject;
            pickerSelectorItem.ManageVisualStates();
        }


        public bool IsFocused
        {
            get { return (bool)GetValue(IsFocusedProperty); }
            set { SetValue(IsFocusedProperty, value); }
        }

        // Using a DependencyProperty as the backing store for IsFocused.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsFocusedProperty =
            DependencyProperty.Register("IsFocused", typeof(bool), typeof(PickerSelectorItem), new PropertyMetadata(false, FocusChangedCallback));


        private static void FocusChangedCallback(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs dependencyPropertyChangedEventArgs)
        {
            PickerSelectorItem pickerSelectorItem = (PickerSelectorItem)dependencyObject;
            pickerSelectorItem.ManageVisualStates();
        }

        /// <summary>
        /// Set correct Visual States
        /// </summary>
        internal void ManageVisualStates(bool isAnimated = true)
        {

            if (this.IsSelected)
            {
                VisualStateManager.GoToState(this, "Selected", isAnimated);
                return;
            }

            if (this.IsFocused)
            {
                VisualStateManager.GoToState(this, "Focused", isAnimated);
                return;
            }

            VisualStateManager.GoToState(this, "UnFocused", isAnimated);
        }
    }

}
