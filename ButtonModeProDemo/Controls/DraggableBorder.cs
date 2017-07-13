using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls.Primitives;
using System.Windows;
using System.Diagnostics;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Interop;
using System.Windows.Controls;
using System.Runtime.InteropServices;
using System.Windows.Shapes;
using System.ComponentModel;

namespace BoardShow.Controls
{
    public partial class DraggableBorder : Border
    {
        [DllImport("user32.dll")]
        static extern bool ClipCursor(ref Rect lpRect);

        [DllImport("user32.dll")]
        static extern bool ClipCursor(IntPtr lpRect);

        private Stopwatch stopwatch = new Stopwatch();

        private Point originalPosition = new Point(0, 0);

        private Point previousLocation = new Point(0, 0);

        private Point currentLocation = new Point(0, 0);

        private double canvasLeft = 0;

        private double canvasTop = 0;

        private bool isMouseDown = false;

        public bool IsDragEnabled { get; set; } = true;

        public event RoutedEventHandler MyMouseDownClick;

        public event MouseButtonEventHandler MyMouseLeftButtonDown;

        public event MouseButtonEventHandler MyMouseLeftButtonUp;

        public event MouseEventHandler MyDragEvent;

        public DraggableBorder()
        {
            this.AllowDrop = true;
        }

        public int KeyCode
        {
            get;
            set;
        }

        public string KeyText
        {
            get;
            set;
        }

        public Point CurrentLocation
        {
            get { return this.currentLocation; }
        }

        public int Index
        {
            get;
            set;
        }

        public RectangleGeometry RectangleGeometry
        {
            get;
            set;
        }

        public FrameworkElement ParentControl
        {
            get;
            set;
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            if (this.IsDragEnabled && this.ParentControl != null)
            {
                this.stopwatch.Reset();
                this.stopwatch.Start();

                this.canvasLeft = Canvas.GetLeft(this);
                this.canvasTop = Canvas.GetTop(this);

                Point point = Mouse.GetPosition(this.ParentControl);
                this.originalPosition.X = point.X;
                this.originalPosition.Y = point.Y;

                if (MyMouseLeftButtonDown != null && RectangleGeometry.FillContains(point))
                {
                    MyMouseLeftButtonDown(this, new MouseButtonEventArgs(InputManager.Current.PrimaryMouseDevice, 0, MouseButton.Left));
                    isMouseDown = true;
                }
            }

            base.OnMouseDown(e);
        }

        protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            if (this.IsDragEnabled && this.ParentControl != null)
            {
                //释放鼠标移动范围
                //this.ReleaseMouseCursor();

                stopwatch.Stop();

                Point point = Mouse.GetPosition(this.ParentControl);

                if (this.IsEnabled && point.X == this.originalPosition.X
                    &&  point.Y == this.originalPosition.Y 
                    && stopwatch.ElapsedMilliseconds < 500)
                {
                    // 点击
                    if (MyMouseDownClick != null)
                    {
                        MyMouseDownClick(this, null);
                    }
                }

                if (MyMouseLeftButtonUp != null && ((point.X < RectangleGeometry.Rect.Width && point.X > 0 && point.Y < RectangleGeometry.Rect.Height && point.Y > 0) || isMouseDown))
                {
                    MyMouseLeftButtonUp(this, new MouseButtonEventArgs(InputManager.Current.PrimaryMouseDevice, 0, MouseButton.Left));
                    isMouseDown = false;
                }
            }

            base.OnMouseLeftButtonUp(e);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (this.IsDragEnabled && this.ParentControl != null)
            {
                this.currentLocation = Mouse.GetPosition(this.ParentControl);

                if (this.MyDragEvent != null && this.isMouseDown)
                {
                    this.MyDragEvent(this, e);
                }

                this.previousLocation = this.currentLocation;
            }
        }
    }
}
