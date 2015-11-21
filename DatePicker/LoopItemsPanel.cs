using System;
using DatePicker.Common;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;

namespace DatePicker.Controls
{
    public class LoopItemsPanel : Panel
    {
        public Orientation orientation;
        public event EventHandler<PickerSelectorItem> SelectedItemChanged;

        public ScrollAction ScrollAction { get; set; }

        // hack to animate a value easely
        private Slider internalSlider;

        private readonly TimeSpan animationDuration = TimeSpan.FromMilliseconds(400);
        internal readonly TimeSpan ShortAnimationDuration = TimeSpan.FromMilliseconds(100);

        // item height. must be 1d to fire first arrangeoverride
        private double itemHeight = 1d;
        private double itemWidth = 1d;

        // true when arrange override is ok
        private bool templateApplied;

        private bool isFirstLayoutPassed;

        private PickerSelector parentDatePickerSelector;

        private Storyboard storyboard;
        private DoubleAnimation animationSnap;

        public PickerSelector ParentDatePickerSelector
        {
            get
            {
                return parentDatePickerSelector ??
                       (parentDatePickerSelector = this.GetVisualAncestor<PickerSelector>());
            }
        }

        public Orientation Orientation
        {
            get { return orientation; }
            set
            {
                orientation = value;
                this.ManipulationMode = this.Orientation == Orientation.Vertical
                    ? (ManipulationModes.TranslateY | ManipulationModes.TranslateInertia)
                    : (ManipulationModes.TranslateX | ManipulationModes.TranslateInertia);
                this.internalSlider.Orientation = value;
            }
        }

        /// <summary>
        /// Ctor
        /// </summary>
        public LoopItemsPanel()
        {
            orientation = Orientation.Vertical;
            this.ManipulationMode = this.Orientation == Orientation.Vertical
                ? (ManipulationModes.TranslateY | ManipulationModes.TranslateInertia)
                : (ManipulationModes.TranslateX | ManipulationModes.TranslateInertia);
            this.IsHitTestVisible = true;
            this.ManipulationDelta += OnManipulationDelta;
            this.ManipulationCompleted += OnManipulationCompleted;
            this.Tapped += OnTapped;
            this.Loaded += OnLoaded;

            this.internalSlider = new Slider
            {
                SmallChange = 0.0000000001,
                Minimum = double.MinValue,
                Maximum = double.MaxValue,
                StepFrequency = 0.0000000001,
                Orientation = this.Orientation
            };
            internalSlider.ValueChanged += OnOffsetChanged;

            this.CreateStoryboard();
            this.LayoutUpdated += OnLayoutUpdated;
        }

        private void OnLoaded(object sender, RoutedEventArgs routedEventArgs)
        {
            InvalidateArrange();
        }


        private void OnLayoutUpdated(object sender, object o)
        {
            this.isFirstLayoutPassed = true;

            this.LayoutUpdated -= OnLayoutUpdated;
        }

        /// <summary>
        /// Click or Tap on an item
        /// On Items Panel, the tap action only scroll to selected tap object
        /// </summary>
        private void OnTapped(object sender, TappedRoutedEventArgs args)
        {
            if (this.Children == null || this.Children.Count == 0)
                return;

            // if not focused, dont do anything
            if (this.ParentDatePickerSelector != null)
            {
                if (!this.ParentDatePickerSelector.IsFocused)
                    return;
            }
            var positionY = args.GetPosition(this).Y;
            var positionX = args.GetPosition(this).X;

            foreach (PickerSelectorItem child in this.Children)
            {
                if (this.Orientation == Orientation.Vertical)
                {

                    var childPositionY = child.GetVerticalPosition();
                    var height = child.RectPosition.Height;
                    if (!(positionY >= childPositionY) || !(positionY <= (childPositionY + height)))
                        continue;
                }
                else
                {

                    var childPositionX = child.GetHorizontalPosition();
                    var width = child.RectPosition.Width;
                    if (!(positionX >= childPositionX) || !(positionX <= (childPositionX + width)))
                        continue;
                }

                this.ScrollToSelectedIndex(child, animationDuration);
                break;
            }
        }


        /// <summary>
        /// When manipulation is completed, go to the closest item
        /// </summary>
        private void OnManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
        {
            PickerSelectorItem middleItem = GetMiddleItem();

            // get rect
            if (middleItem == null) return;

            // animate and scroll to
            this.ScrollToSelectedIndex(middleItem, animationDuration);
        }

        /// <summary>
        /// On manipulation delta
        /// </summary>
        private void OnManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            this.ScrollAction = this.Orientation == Orientation.Vertical
                ? (e.Delta.Translation.Y > 0 ? ScrollAction.Down : ScrollAction.Up)
                : (e.Delta.Translation.X > 0 ? ScrollAction.Right : ScrollAction.Left);

            // get translation
            var translation = e.Delta.Translation;

            // update position

            this.internalSlider.Value += this.Orientation == Orientation.Vertical ? translation.Y / 2 :
                translation.X / 2;

        }

        /// <summary>
        /// Scroll to a selected index (with animation or not)
        /// </summary>
        internal void ScrollToSelectedIndex(PickerSelectorItem selectedItem, TimeSpan duration)
        {
            if (!templateApplied)
                return;

            var centerTopOffset = Orientation == Orientation.Vertical
                ? (this.ActualHeight / 2d) - (itemHeight) / 2d
                : (this.ActualWidth / 2d) - (itemWidth) / 2d;

            var deltaOffset = centerTopOffset -
                              (Orientation == Orientation.Vertical
                                  ? selectedItem.GetVerticalPosition()
                                  : selectedItem.GetHorizontalPosition());

            if (Double.IsInfinity(deltaOffset) || Double.IsNaN(deltaOffset))
                return;

            if (duration == TimeSpan.Zero)
                this.internalSlider.Value += deltaOffset;
            else
                this.UpdatePositionsWithAnimationAsync(selectedItem, deltaOffset, duration);

        }

        /// <summary>
        /// Get the centered item
        /// </summary>
        internal PickerSelectorItem GetMiddleItem()
        {
            // Récupère la taille du panel
            double actualHeight = this.ActualHeight;
            double actualWidth = this.ActualWidth;

            // Récupère la position de la moitié de mon viewportheight
            var middleY = actualHeight / 2;
            var middleX = actualWidth / 2;

            // Génération d'un point
            var middlePosition = new Point(middleX, middleY);

            foreach (PickerSelectorItem uiElement in this.Children)
            {
                if (uiElement == null) continue;

                Rect rect = uiElement.TransformToVisual(this).TransformBounds(new Rect());

                if (Orientation == Orientation.Vertical)
                {
                    if (!(middlePosition.Y <= rect.Y + uiElement.RenderSize.Height) || !(middlePosition.Y >= rect.Y))
                        continue;
                }
                else
                {
                    if (!(middlePosition.X <= rect.X + uiElement.RenderSize.Width) || !(middlePosition.X >= rect.X))
                        continue;
                }

                return uiElement;
            }

            return null;
        }

        /// <summary>
        /// During an animation of the Scrollbar, update positions
        /// </summary>
        private void OnOffsetChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            this.UpdatePositions(e.NewValue);
        }

        /// <summary>
        /// Get Items Source count items
        /// </summary>
        /// <returns></returns>
        private int GetItemsCount()
        {
            return this.Children != null ? this.Children.Count : 0;
        }


        /// <summary>
        /// Creating the storyboard
        /// </summary>
        private void CreateStoryboard()
        {
            this.storyboard = new Storyboard();
            this.animationSnap = new DoubleAnimation { EnableDependentAnimation = true };

            this.storyboard.Children.Add(animationSnap);

            Storyboard.SetTarget(animationSnap, internalSlider);
            Storyboard.SetTargetProperty(animationSnap, "Value");
        }

        /// <summary>
        /// Updating with an animation (after a tap)
        /// </summary>
        private void UpdatePositionsWithAnimationAsync(PickerSelectorItem selectedItem, Double delta, TimeSpan duration)
        {
            if (Orientation == Orientation.Vertical)
            {
                animationSnap.From = selectedItem.GetTranslateTransform().Y;
                animationSnap.To = selectedItem.GetTranslateTransform().Y + delta;
            }
            else
            {
                animationSnap.From = selectedItem.GetTranslateTransform().X;
                animationSnap.To = selectedItem.GetTranslateTransform().X + delta;
            }

            animationSnap.Duration = duration;
            animationSnap.EasingFunction = new ExponentialEase { EasingMode = EasingMode.EaseInOut };

            internalSlider.ValueChanged -= OnOffsetChanged;
            internalSlider.Value = Orientation == Orientation.Vertical
                ? selectedItem.GetTranslateTransform().Y
                : selectedItem.GetTranslateTransform().X;

            internalSlider.ValueChanged += OnOffsetChanged;

            this.storyboard.Completed += (sender, o) =>
            {
                if (SelectedItemChanged != null)
                {
                    SelectedItemChanged(this, selectedItem);
                }
            };

            this.storyboard.RunAsync();
        }

        /// <summary>
        /// Updating position
        /// </summary>
        private void UpdatePositions(Double offsetDelta)
        {
            var itemLegth = this.Orientation == Orientation.Vertical ? itemHeight : itemWidth;

            Double maxLogicalLenght = this.GetItemsCount() * itemLegth;

            var offset = offsetDelta % maxLogicalLenght;

            // Get the correct number item
            Int32 itemNumberSeparator = (Int32)(Math.Abs(offset) / itemLegth);

            Int32 itemIndexChanging;
            Double offsetAfter;
            Double offsetBefore;

            if (offset > 0)
            {
                itemIndexChanging = this.GetItemsCount() - itemNumberSeparator - 1;
                offsetAfter = offset;

                if (offset % maxLogicalLenght == 0)
                    itemIndexChanging++;

                offsetBefore = offsetAfter - maxLogicalLenght;
            }
            else
            {
                itemIndexChanging = itemNumberSeparator;
                offsetBefore = offset;
                offsetAfter = maxLogicalLenght + offsetBefore;
            }

            // items that must be before
            this.UpdatePosition(itemIndexChanging, this.GetItemsCount(), offsetBefore);

            // items that must be after
            this.UpdatePosition(0, itemIndexChanging, offsetAfter);
        }

        /// <summary>
        /// Translate items to a new offset
        /// </summary>
        private void UpdatePosition(Int32 startIndex, Int32 endIndex, Double offset)
        {
            for (Int32 i = startIndex; i < endIndex; i++)
            {
                PickerSelectorItem loopListItem = this.Children[i] as PickerSelectorItem;
                if (loopListItem == null)
                    continue;

                TranslateTransform translateTransform = loopListItem.GetTranslateTransform();
                if (Orientation == Orientation.Vertical)
                    translateTransform.Y = offset;
                else
                    translateTransform.X = offset;

            }
        }

        /// <summary>
        /// Arrange all items
        /// </summary>
        protected override Size ArrangeOverride(Size finalSize)
        {
            // Clip to ensure items dont override container
            this.Clip = new RectangleGeometry { Rect = new Rect(0, 0, finalSize.Width, finalSize.Height) };

            if (this.Children == null || this.Children.Count == 0)
            {
                var size = base.ArrangeOverride(finalSize);
                var width = double.IsInfinity(size.Width) ? 400 : size.Width;
                var height = double.IsInfinity(size.Height) ? 800 : size.Height;

                return new Size(width, height);
            }

            Double positionTop = 0d;
            Double positionLeft = 0d;
            PickerSelectorItem selectedItem = null;

            // Must Create looping items count
            foreach (PickerSelectorItem item in this.Children)
            {
                if (item == null)
                    continue;

                Size desiredSize = item.DesiredSize;

                if (double.IsNaN(desiredSize.Width) || double.IsNaN(desiredSize.Height)) continue;

                item.RectPosition = new Rect(positionLeft, positionTop, desiredSize.Width, desiredSize.Height);

                item.Arrange(item.RectPosition);

                if (item.IsSelected)
                    selectedItem = item;

                if (Orientation == Orientation.Vertical)
                    positionTop += desiredSize.Height;
                else
                    positionLeft += desiredSize.Width;

            }

            templateApplied = true;

            if (selectedItem != null)
            {
                this.UpdatePositions(internalSlider.Value);
                this.ScrollToSelectedIndex(selectedItem, TimeSpan.Zero);
            }

            return finalSize;
        }

        /// <summary>
        /// Measure items 
        /// </summary>
        protected override Size MeasureOverride(Size availableSize)
        {
            var width = double.IsInfinity(availableSize.Width) ? 400 : availableSize.Width;
            var height = double.IsInfinity(availableSize.Height) ? 800 : availableSize.Height;

            var nSize = new Size(width, height);

            // Measure all items
            foreach (UIElement container in this.Children)
            {
                //container.Measure(availableSize);
                container.Measure(nSize);

                if (Orientation == Orientation.Vertical)
                {
                    if (itemHeight != container.DesiredSize.Height)
                        itemHeight = container.DesiredSize.Height;
                }
                else
                {
                    if (itemWidth != container.DesiredSize.Width)
                        itemWidth = container.DesiredSize.Width;
                }
            }

            return nSize;
        }

    }
}
