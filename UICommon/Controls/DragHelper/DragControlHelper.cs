using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Shapes;
using System.Windows.Controls;

namespace UICommon.Controls
{
    public class DragControlHelper : DragHelperBase
    {
        public bool IsAttached { get; set; } = false;

        #region Cotr & Events
        public DragControlHelper()
        {
            this.Loaded += OnLoaded;
            this.Unloaded += OnUnloaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            AttachParentEvents();
            this.Loaded -= OnLoaded;
        }
        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            DetachParentEvents();
            this.Unloaded -= OnUnloaded;
        }
        #endregion

        #region DependencyProperty

        #region IsAutoResetES
        public static bool GetIsAutoResetES(DependencyObject obj)
        {
            return (bool)obj.GetValue(IsAutoResetESProperty);
        }

        public static void SetIsAutoResetES(DependencyObject obj, bool value)
        {
            obj.SetValue(IsAutoResetESProperty, value);
        }

        // Using a DependencyProperty as the backing store for IsAutoResetESProperty.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsAutoResetESProperty =
            DependencyProperty.RegisterAttached("IsAutoResetES", typeof(bool), typeof(DragHelperBase), new PropertyMetadata(false));
        #endregion

        #region IsEditable
        public static bool GetIsEditable(DependencyObject obj)
        {
            return (bool)obj.GetValue(IsEditableProperty);
        }

        public static void SetIsEditable(DependencyObject obj, bool value)
        {
            obj.SetValue(IsEditableProperty, value);
        }

        // Using a DependencyProperty as the backing store for IsEditable.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsEditableProperty =
            DependencyProperty.RegisterAttached("IsEditable", typeof(bool), typeof(DragHelperBase), new PropertyMetadata(false));
        #endregion

        #region IsSelectable
        public static bool GetIsSelectable(DependencyObject obj)
        {
            return (bool)obj.GetValue(IsSelectableProperty);
        }

        public static void SetIsSelectable(DependencyObject obj, bool value)
        {
            obj.SetValue(IsSelectableProperty, value);
        }

        // Using a DependencyProperty as the backing store for IsSelectable.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsSelectableProperty =
            DependencyProperty.RegisterAttached("IsSelectable", typeof(bool), typeof(DragHelperBase), new PropertyMetadata(false));
        #endregion

        #region TargetElement
        public FrameworkElement TargetElement
        {
            get { return (FrameworkElement)GetValue(TargetElementProperty); }
            set { SetValue(TargetElementProperty, value); }
        }

        // Using a DependencyProperty as the backing store for TargetElement.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty TargetElementProperty =
            DependencyProperty.Register("TargetElement", typeof(FrameworkElement), typeof(DragControlHelper),
                                        new FrameworkPropertyMetadata(default(FrameworkElement),
                                            new PropertyChangedCallback(TargetElementChanged)));
        #endregion

        #region TargetElementChanged
        private static void TargetElementChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            DragControlHelper Helper = d as DragControlHelper;

            if (Helper == null)
            {
                return;
            }

            FrameworkElement oldTarget = e.OldValue as FrameworkElement;
            Helper.DetachTatgetEvents(oldTarget);
            bool isAutoReset = oldTarget != null ? GetIsAutoResetES(oldTarget) : false;

            FrameworkElement newTarget = e.NewValue as FrameworkElement;

            if (newTarget == null
                || newTarget is DragControlHelper
                || newTarget.RenderSize.IsEmpty
                || double.IsNaN(newTarget.RenderSize.Width)
                || double.IsNaN(newTarget.RenderSize.Height)
                || !GetIsSelectable(newTarget))
            {
                Helper.Visibility = Visibility.Collapsed;

                if (isAutoReset)
                {
                    //重置可编辑属性
                    SetIsEditable(oldTarget, false);
                    SetIsSelectable(oldTarget, false);
                }

                return;
            }

            Helper.AttachTatgetEvents(newTarget);

            int Zindex = Panel.GetZIndex(newTarget);

            if (Zindex >= Panel.GetZIndex(Helper))
            {
                Panel.SetZIndex(Helper, Zindex + 1);
            }

            double Y = CorrectDoubleValue(Canvas.GetTop(newTarget));
            double X = CorrectDoubleValue(Canvas.GetLeft(newTarget));

            Canvas.SetTop(Helper, Y);
            Canvas.SetLeft(Helper, X);
            Helper.Width = newTarget.ActualWidth;
            Helper.Height = newTarget.ActualHeight;
        }
        #endregion

        #endregion

        #region Parent Event Handler

        #region AttachParentEvents
        public void AttachParentEvents(Canvas canvasParent = null)
        {
            Canvas CanvasParent = canvasParent != null ? canvasParent : Parent as Canvas;

            if (CanvasParent == null)
            {
                throw new Exception("DragControlHelper Must place into Canvas!");
            }

            CanvasParent.MouseLeftButtonDown += OnParentMouseLeftButtonDown;

            this.IsAttached = true;
        }
        #endregion

        #region DetachParentEvents
        public void DetachParentEvents(Canvas canvasParent = null)
        {
            Canvas CanvasParent = canvasParent != null ? canvasParent : Parent as Canvas;

            if (CanvasParent != null)
            {
                CanvasParent.MouseLeftButtonDown -= OnParentMouseLeftButtonDown;
            }

            this.IsAttached = false;
        }
        #endregion

        #region OnParentMouseLeftButtonDown
        private void OnParentMouseLeftButtonDown(object Sender, MouseButtonEventArgs e)
        {
            FrameworkElement SelectedElement = e.OriginalSource as FrameworkElement;

            if (CheckTargetIsSelectable(SelectedElement))
            {
                TargetElement = SelectedElement;
            }
            else
            {
                TargetElement = null;
            }

            SelectedElement.Focus();
        }
        #endregion

        #region CheckTargetIsSelectable
        private bool CheckTargetIsSelectable(FrameworkElement Target)
        {
            return (Target != null) && !Target.Equals(Parent) && !Target.Equals(this) && GetIsSelectable(Target);
        }
        #endregion

        #endregion

        #region Target Event Handler

        #region AttachTatgetEvents
        private void AttachTatgetEvents(FrameworkElement Target)
        {
            if (Target == null)
            {
                throw new ArgumentNullException("Target");
            }

            Target.Focusable = true;
            Target.GotFocus += TargetElement_GotFocus;
            Target.LostFocus += TargetElement_LostFocus;

            double Thickness = 1.0;

            if (Target is Shape)
            {
                Thickness = (Target as Shape).StrokeThickness;
            }

            bool CanEdit = GetIsEditable(Target);

            SetupVisualPropertes(Thickness, CanEdit);
        }
        #endregion

        #region DetachTatgetEvents
        private void DetachTatgetEvents(FrameworkElement Target)
        {
            if (Target != null)
            {
                Target.GotFocus -= TargetElement_GotFocus;
                Target.LostFocus -= TargetElement_LostFocus;
            }
        }
        #endregion

        #region TargetElement_GotFocus
        private void TargetElement_GotFocus(object sender, RoutedEventArgs e)
        {
            Visibility = Visibility.Visible;
        }
        #endregion

        #region TargetElement_LostFocus
        private void TargetElement_LostFocus(object sender, RoutedEventArgs e)
        {
            TargetElement = null;
            Visibility = Visibility.Collapsed;
        }
        #endregion

        #endregion

        #region DragHelperBase Member

        #region GetTargetActualBound
        protected override Rect GetTargetActualBound()
        {
            if (TargetElement == null)
            {
                return Rect.Empty;
            }

            double Top = CorrectDoubleValue(Canvas.GetTop(TargetElement));
            double Left = CorrectDoubleValue(Canvas.GetLeft(TargetElement));

            return new Rect
            {
                X = Left,
                Y = Top,
                Width = TargetElement.ActualWidth,
                Height = TargetElement.ActualHeight
            };
        }
        #endregion

        #region SetTargetActualBound
        protected override void SetTargetActualBound(Rect NewBound)
        {
            if (TargetElement != null)
            {
                TargetElement.Width = NewBound.Width;
                TargetElement.Height = NewBound.Height;
                Canvas.SetTop(TargetElement, NewBound.Y);
                Canvas.SetLeft(TargetElement, NewBound.X);
            }
        }
        #endregion

        #region RaisenDragCompletedEvent
        protected override void RaisenDragStartedEvent(Rect NewBound)
        {
            RaiseEvent(new DragChangedEventArgs(DragCompletedEvent, NewBound, TargetElement));
        }

        protected override void RaisenDragCompletedEvent(Rect NewBound)
        {
            RaiseEvent(new DragChangedEventArgs(DragCompletedEvent, NewBound, TargetElement));
        }

        protected override void RaisenDragChangingEvent(Rect NewBound)
        {
            RaiseEvent(new DragChangedEventArgs(DragChangingEvent, NewBound, TargetElement));
        }

        protected override bool GetTargetIsEditable()
        {
            return GetIsEditable(TargetElement);
        }
        #endregion 

        #endregion
    }
}
