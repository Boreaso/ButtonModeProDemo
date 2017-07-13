using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using BoardShow.Controls;
using System.Windows.Media.Animation;
using BoardShow;
using BoardShow.Dialogs;
using CalabashDialog;
using ButtonModeProDemo.Dialogs;
using UICommon.Controls;

namespace ButtonModeProDemo
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public static Setting mainSetting = null;
        //public DragControlHelper dragControlHelper = null;

        #region DraggableBorde相关变量
        /// <summary>
        /// 按下左边
        /// </summary>
        private Point startPoint;

        /// <summary>
        /// 画布1 Border集合
        /// </summary>
        List<DraggableBorder> canvasBorderList = new List<DraggableBorder>();

        /// <summary>
        /// 缓存当前设置的按钮
        /// </summary>
        private Dictionary<int, string> boardKeyCodes = new Dictionary<int, string>();

        /// <summary>
        /// 单元格边长
        /// </summary>
        private const int GRID_SIZE = 24;

        /// <summary>
        /// 单元格间距
        /// </summary>
        private const int GRID_INTERVAL = 2;

        /// <summary>
        /// 每个DraggableBorder的索引
        /// </summary>
        private int DraggableBorderIndex = 0;

        /// <summary>
        /// 用于高亮Border防止位置的矩形
        /// </summary>
        private Rectangle hithLightRectangle = null;
        #endregion

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            InitSettings();

            //初始化事件
            this.UICommon_DragControlHelper.DragCompleted += UICommon_DragControlHelper_DragCompleted;
        }

        private void UICommon_DragControlHelper_DragCompleted(object Sender, DragChangedEventArgs e)
        {
            //System.Console.WriteLine((Sender as DragControlHelper).TargetElement);
            DraggableBorder targetObj = (Sender as DragControlHelper).TargetElement as DraggableBorder;

            if (targetObj != null && targetObj.Width != targetObj.RectangleGeometry.Rect.Width ||
                targetObj.Height != targetObj.RectangleGeometry.Rect.Height)
            {
                //draggableBorder支持拖动改变大小，修正RectangleGeometry
                targetObj.RectangleGeometry = new RectangleGeometry()
                {
                    Rect = new Rect(Canvas.GetLeft(targetObj), Canvas.GetTop(targetObj), targetObj.Width, targetObj.Height)
                };
            }
        }

        private void InitSettings()
        {
            mainSetting = new Setting();

            foreach (BoardLayout layout in mainSetting.savedBoardLayouts)
            {
                ComboBoxItem newItem = new ComboBoxItem();
                newItem.Content = layout.LayoutName;
                this.ComboBox_BoardLayout.Items.Add(newItem);
            }

            this.ComboBox_BoardLayout.SelectedIndex = 0;

            if (this.ComboBox_BoardLayout.SelectedIndex >= 0 &&
                this.ComboBox_BoardLayout.Items.Count > this.ComboBox_BoardLayout.SelectedIndex)
            {
                mainSetting.currentBoardLayout = mainSetting.savedBoardLayouts[this.ComboBox_BoardLayout.SelectedIndex];

                this.ResetBoardLayout(mainSetting.currentBoardLayout.BoardButtonDefs);
            }
        }

        private void InitDraggableBorders(Canvas previewCanvas)
        {
            this.canvasBorderList = Utils.ControlSearchHelper.GetChildControls<DraggableBorder>(previewCanvas, "");

            //初始化预览面板
            for (int i = 0; i < this.canvasBorderList.Count; i++)
            {
                DraggableBorder draggableBorder = this.canvasBorderList[i];

                InitDraggableBorder(draggableBorder, i);
            }
        }

        private void InitDraggableBorder(DraggableBorder draggableBorder, int index)
        {
            draggableBorder.Index = index;
            draggableBorder.MyDragEvent += draggableBorder_MyDragEvent;
            draggableBorder.MyMouseLeftButtonDown += draggableBorder_MyMouseLeftButtonDown;
            draggableBorder.MyMouseLeftButtonUp += draggableBorder_MyMouseLeftButtonUp;
            draggableBorder.MyMouseDownClick += draggableBorder_MyMouseDownClick;
            //添加右键菜单
            ContextMenu contextMenu = new ContextMenu();
            MenuItem deleteMenuItem = new MenuItem();
            deleteMenuItem.Header = "删除";
            deleteMenuItem.Click += (sender1, e1) =>
            {
                Canvas parentCanvas = draggableBorder.Parent as Canvas;

                if (parentCanvas.Children.Contains(draggableBorder))
                {
                    parentCanvas.Children.Remove(draggableBorder);

                    if (this.canvasBorderList.Contains(draggableBorder))
                    {
                        this.canvasBorderList.Remove(draggableBorder);
                    }

                    if (this.boardKeyCodes.ContainsKey(draggableBorder.KeyCode))
                    {
                        this.boardKeyCodes.Remove(draggableBorder.KeyCode);
                    }
                }
            };
            contextMenu.Items.Add(deleteMenuItem);

            MenuItem resizeMenuItem = new MenuItem();
            resizeMenuItem.Header = "设置大小";
            resizeMenuItem.Click += (sender1, e1) =>
            {
                //设置属性
                DragControlHelper.SetIsEditable(draggableBorder, true);    //允许编辑
                DragControlHelper.SetIsSelectable(draggableBorder, true);  //允许选中
                DragControlHelper.SetIsAutoResetES(draggableBorder, true); //焦点回到Canvas取消编辑状态

                //引发TargetElementChanged事件
                this.PreviewCanvas.Focus();
                this.UICommon_DragControlHelper.TargetElement = draggableBorder;
                draggableBorder.Focus();
            };
            contextMenu.Items.Add(resizeMenuItem);

            draggableBorder.ContextMenu = contextMenu;

            Canvas.SetZIndex(draggableBorder, 0);
        }

        private void Button_Add_Click(object sender, RoutedEventArgs e)
        {
            KeyInputDialog dialog = new KeyInputDialog("按键设置", "请按下按键：", -1, "");

            dialog.OKButton.Click += (s1, e1) =>
            {
                if (dialog.KeyCode != -1 && !string.IsNullOrEmpty(dialog.KeyText))
                {
                    if (this.boardKeyCodes.ContainsKey(dialog.KeyCode))
                    {
                        DialogTip warningDialog = new DialogTip("警告", "该按键已经设置。");
                        warningDialog.ShowDialog();
                        return;
                    }

                    Size insertBoardSize = new Size(GRID_SIZE * dialog.WidthGridCount + (dialog.WidthGridCount - 1) * GRID_INTERVAL, 
                                                    GRID_SIZE * dialog.HeightGridCount + (dialog.HeightGridCount - 1) * GRID_INTERVAL);

                    Point insertLocation = Utils.ControlLayoutHelper.SearchInsertLocation(this.PreviewCanvas, insertBoardSize, GRID_INTERVAL);

                    if (insertLocation.X == -1 || insertLocation.Y == -1)
                    {
                        DialogTip warningDialog = new DialogTip("警告", "没有合适的位置插入按钮。");
                        warningDialog.ShowDialog();
                        return;
                    }

                    //绘制Border
                    Rect borderRect = new Rect(insertLocation.X, insertLocation.Y, insertBoardSize.Width, insertBoardSize.Height);
                    ButtonDefinition bDef = new ButtonDefinition(dialog.KeyCode, dialog.KeyText, borderRect);
                    DraggableBorder draggableBorder = Utils.ControlLayoutHelper.DrawBorderOnCanvas(this, this.PreviewCanvas, bDef);

                    this.InitDraggableBorder(draggableBorder, this.DraggableBorderIndex++);

                    this.canvasBorderList.Add(draggableBorder);
                    this.boardKeyCodes.Add(dialog.KeyCode, dialog.KeyText);
                }

                dialog.Close();
            };

            dialog.ShowDialog();
        }

        private void Button_Save_Click(object sender, RoutedEventArgs e)
        {
            LayoutNameDialog layoutNameDialog = new LayoutNameDialog("保存布局", "布局名称");

            layoutNameDialog.OKButton.Click += (sender1, e1) =>
            {
                if (string.IsNullOrEmpty(layoutNameDialog.LayoutNameTextBox.Text))
                {
                    DialogWarning dialog = new DialogWarning("提示", "请输入布局名称。");
                    dialog.ShowDialog();
                    return;
                }

                BoardLayout newBoardLayout = mainSetting.BuildBoardLayout(this.canvasBorderList, layoutNameDialog.LayoutNameTextBox.Text);
                mainSetting.SaveBoardLayoutToJsonFile(newBoardLayout.LayoutName, newBoardLayout, true);
                mainSetting.savedBoardLayouts.Add(newBoardLayout);

                ComboBoxItem newItem = new ComboBoxItem();
                newItem.Content = newBoardLayout.LayoutName;
                this.ComboBox_BoardLayout.Items.Add(newItem);
                this.ComboBox_BoardLayout.SelectedItem = newItem;

                layoutNameDialog.Close();
            };

            layoutNameDialog.ShowDialog();
        }

        private void Button_Delete_Click(object sender, RoutedEventArgs e)
        {
            DialogQuestion dialogQuestion = new DialogQuestion("提示", "确定删除布局吗？");
            dialogQuestion.ShowDialog();

            if (dialogQuestion.returnValue == DialogQuestion.DialogReturn.OK)
            {
                if (this.ComboBox_BoardLayout.SelectedIndex >= 0)
                {
                    mainSetting.savedBoardLayouts.Remove(mainSetting.currentBoardLayout);
                    mainSetting.RemoveBoardLayoutFile(mainSetting.currentBoardLayout.LayoutName);
                    this.ComboBox_BoardLayout.Items.RemoveAt(this.ComboBox_BoardLayout.SelectedIndex);

                    if (this.ComboBox_BoardLayout.Items.Count > 0)
                    {
                        this.ComboBox_BoardLayout.SelectedIndex = this.ComboBox_BoardLayout.SelectedIndex - 1 >= 0 ? this.ComboBox_BoardLayout.SelectedIndex : 0;
                    }
                }
            }
        }

        private void ComboBox_BoardLayout_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox comboBox = sender as ComboBox;

            if (comboBox.SelectedIndex >= 0 && mainSetting != null &&
                mainSetting.savedBoardLayouts.Count > comboBox.SelectedIndex)
            {
                mainSetting.currentBoardLayout = mainSetting.savedBoardLayouts[comboBox.SelectedIndex];

                this.ResetBoardLayout(mainSetting.currentBoardLayout.BoardButtonDefs);
            }
        }

        private void ClearDraggableBorders(Canvas parentCanvas)
        {
            UIElement[] elements = new UIElement[parentCanvas.Children.Count];
            parentCanvas.Children.CopyTo(elements, 0);

            foreach (var element in elements)
            {
                DraggableBorder border = element as DraggableBorder;
                if (border != null && parentCanvas.Children.Contains(border))
                {
                    parentCanvas.Children.Remove(border);
                }
            }
        }
        #region DraggableBorder相关

        private void draggableBorder_MyDragEvent(object sender, MouseEventArgs e)
        {
            DraggableBorder draggableBorder = sender as DraggableBorder;
            Canvas parentCanvas = draggableBorder.Parent as Canvas;

            double borderLeft = draggableBorder.CurrentLocation.X - this.startPoint.X;
            double borderTop = draggableBorder.CurrentLocation.Y - this.startPoint.Y;

            Canvas.SetLeft(draggableBorder, borderLeft);
            Canvas.SetTop(draggableBorder, borderTop);

            //计算当前拖动位置应该归属的块
            double gridLeft = Canvas.GetLeft(this.hithLightRectangle);
            double gridTop = Canvas.GetTop(this.hithLightRectangle);
            if ((borderLeft - gridLeft) >= (GRID_SIZE + GRID_INTERVAL))
            {
                gridLeft += (GRID_SIZE + GRID_INTERVAL);
            }
            else if ((borderLeft - gridLeft) <= -(GRID_SIZE + GRID_INTERVAL))
            {
                gridLeft -= (GRID_SIZE + GRID_INTERVAL);
            }
            if ((borderTop - gridTop) >= (GRID_SIZE + GRID_INTERVAL))
            {
                gridTop += (GRID_SIZE * 2 + GRID_INTERVAL * 2);
            }
            else if ((borderTop - gridTop) <= -(GRID_SIZE + GRID_INTERVAL))
            {
                gridTop -= (GRID_SIZE * 2 + GRID_INTERVAL * 2);
            }

            Rect gridRect = new Rect(gridLeft, gridTop, draggableBorder.Width, draggableBorder.Height);
            this.PutHighLightRectangle(parentCanvas, this.hithLightRectangle, gridRect);

            //判断是否允许放置在当前位置，不合法则不显示
            bool isAllowHighLight = this.canvasBorderList.All(x => !this.IsBorderOverlap(gridRect, x.RectangleGeometry.Rect)) &&
                                    gridLeft >= 0 && gridLeft + draggableBorder.Width <= this.PreviewCanvas.Width &&
                                    gridTop >= 0 && gridTop + draggableBorder.Height <= this.PreviewCanvas.Height;
            if (isAllowHighLight)
            {
                if (!parentCanvas.Children.Contains(this.hithLightRectangle))
                {
                    parentCanvas.Children.Add(this.hithLightRectangle);
                }

                draggableBorder.RectangleGeometry = new RectangleGeometry() { Rect = gridRect };
            }
            else
            {
                if (parentCanvas.Children.Contains(this.hithLightRectangle))
                {
                    parentCanvas.Children.Remove(this.hithLightRectangle);
                }
            }

            //计算Border中心点位置
            Point point = new Point(borderLeft + draggableBorder.Width / 2, borderTop + draggableBorder.Height / 2);

            DraggableBorder coverBorder = null;
            if (draggableBorder.ParentControl.Equals(this.PreviewCanvas))
            {
                coverBorder = canvasBorderList.Find(p => p.RectangleGeometry.FillContains(point));
            }
            if (coverBorder != null && !coverBorder.Equals(draggableBorder))
            {
                if (draggableBorder.RenderSize == coverBorder.RenderSize)
                {
                    //元素大小相同的情况下可以交换位置，否则不可交换
                    RectangleGeometry tempRect = coverBorder.RectangleGeometry;
                    coverBorder.RectangleGeometry = draggableBorder.RectangleGeometry;
                    draggableBorder.RectangleGeometry = tempRect;
                    this.PutControl(coverBorder);
                }
            }
        }

        private void draggableBorder_MyMouseDownClick(object sender, RoutedEventArgs e)
        {

        }

        private void draggableBorder_MyMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DraggableBorder draggableBorder = sender as DraggableBorder;
            Canvas parentCanvas = draggableBorder.Parent as Canvas;

            //置顶
            Canvas.SetZIndex(draggableBorder, 1);

            //捕获鼠标
            draggableBorder.CaptureMouse();

            this.startPoint = Mouse.GetPosition(draggableBorder);

            if (this.canvasBorderList.Contains(draggableBorder))
            {
                this.canvasBorderList.Remove(draggableBorder);
            }

            if (parentCanvas.Children.Contains(this.hithLightRectangle))
            {
                parentCanvas.Children.Remove(this.hithLightRectangle);
            }

            if (this.hithLightRectangle == null)
            {
                this.hithLightRectangle = Utils.ControlLayoutHelper.DrawRoundRect(parentCanvas, draggableBorder.RectangleGeometry.Rect);
            }

            if (!parentCanvas.Children.Contains(this.hithLightRectangle))
            {
                parentCanvas.Children.Add(this.hithLightRectangle);
            }

            this.PutHighLightRectangle(parentCanvas, this.hithLightRectangle, draggableBorder.RectangleGeometry.Rect);
        }

        private void draggableBorder_MyMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            DraggableBorder draggableBorder = sender as DraggableBorder;
            Canvas parentCanvas = draggableBorder.Parent as Canvas;

            //释放鼠标
            draggableBorder.ReleaseMouseCapture();

            //取消置顶
            //Canvas.SetZIndex(draggableBorder, 0);

            this.PutControl(draggableBorder);

            if (!this.canvasBorderList.Contains(draggableBorder))
            {
                this.canvasBorderList.Add(draggableBorder);
            }

            if (parentCanvas.Children.Contains(this.hithLightRectangle))
            {
                parentCanvas.Children.Remove(this.hithLightRectangle);
            }
            this.hithLightRectangle = null;
        }

        private DoubleAnimation CreateAnimation(double? from, double? to, DependencyObject value, string property, long durationMs = 100)
        {
            DoubleAnimation animation = new DoubleAnimation();
            animation.From = from;
            animation.To = to;
            animation.Duration = TimeSpan.FromMilliseconds(durationMs);
            Storyboard.SetTarget(animation, value);
            Storyboard.SetTargetProperty(animation, new PropertyPath(property));
            return animation;
        }

        private void PutControl(DraggableBorder draggableBorder)
        {
            double left = Canvas.GetLeft(draggableBorder);
            double top = Canvas.GetTop(draggableBorder);

            Storyboard storyboard = new Storyboard();
            DoubleAnimation animation = CreateAnimation(left, draggableBorder.RectangleGeometry.Rect.Left, draggableBorder, "(Canvas.Left)");
            storyboard.Children.Add(animation);
            animation = CreateAnimation(top, draggableBorder.RectangleGeometry.Rect.Top, draggableBorder, "(Canvas.Top)");
            storyboard.Children.Add(animation);
            storyboard.Completed += (sender1, e1) =>
            {
                storyboard.Stop();
                storyboard = null;
                Canvas.SetLeft(draggableBorder, draggableBorder.RectangleGeometry.Rect.Left);
                Canvas.SetTop(draggableBorder, draggableBorder.RectangleGeometry.Rect.Top);

                //取消置顶
                Canvas.SetZIndex(draggableBorder, 0);
            };

            storyboard.Begin();
        }

        private void PutHighLightRectangle(Canvas canvas, Rectangle rectangle, Rect destLocation)
        {
            if ((Canvas.GetLeft(rectangle) == destLocation.X &&
                Canvas.GetTop(rectangle) == destLocation.Y) ||
                ((destLocation.X + rectangle.Width > canvas.Width) ||
                destLocation.Y + rectangle.Height > canvas.Height))
            {
                return;
            }

            Canvas.SetLeft(rectangle, destLocation.X);
            Canvas.SetTop(rectangle, destLocation.Y);
        }

        private void ResetBoardLayout(List<ButtonDefinition> buttonDefs)
        {
            this.canvasBorderList.Clear();
            this.boardKeyCodes.Clear();
            this.ClearDraggableBorders(this.PreviewCanvas);

            foreach (ButtonDefinition bDef in mainSetting.currentBoardLayout.BoardButtonDefs)
            {
                Utils.ControlLayoutHelper.DrawBorderOnCanvas(this, this.PreviewCanvas, bDef);
                boardKeyCodes.Add(bDef.KeyCode, bDef.KeyText);
            }

            this.InitDraggableBorders(this.PreviewCanvas);
        }

        /// <summary>
        /// 判断两个矩形是否相交
        /// </summary>
        /// <param name="r1"></param>
        /// <param name="r2"></param>
        /// <returns></returns>
        private bool IsBorderOverlap(Rect r1, Rect r2)
        {
            return !(r1.Left > r2.Right || r1.Top > r2.Bottom || r2.Left > r1.Right || r2.Top > r1.Bottom);
        }
        #endregion
    }
}
