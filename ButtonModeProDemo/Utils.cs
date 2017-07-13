using BoardShow.Controls;
using CalabashFont;
using CalabashTools;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace BoardShow
{
    public static class Utils
    {
        #region 查找控件
        /// <summary>
        /// 用于查找控件的工具类：找到父控件、子控件
        /// </summary>
        public static class ControlSearchHelper
        {
            /// <summary>
            /// 查找父控件
            /// </summary>
            /// <typeparam name="T">父控件的类型</typeparam>
            /// <param name="obj">要找的是obj的父控件</param>
            /// <param name="name">想找的父控件的Name属性</param>
            /// <returns>目标父控件</returns>
            public static T GetParentControl<T>(DependencyObject depObj, string name) where T : FrameworkElement
            {
                DependencyObject parent = VisualTreeHelper.GetParent(depObj);

                while (parent != null)
                {
                    if (parent is T && (((T)parent).Name == name | string.IsNullOrEmpty(name)))
                    {
                        return (T)parent;
                    }

                    // 在上一级父控件中没有找到指定名字的控件，就再往上一级找
                    parent = VisualTreeHelper.GetParent(parent);
                }

                return null;
            }

            /// <summary>
            /// 查找子控件
            /// </summary>
            /// <typeparam name="T">子控件的类型</typeparam>
            /// <param name="obj">要找的是obj的子控件</param>
            /// <param name="name">想找的子控件的Name属性</param>
            /// <returns>目标子控件</returns>
            public static T GetChildControl<T>(DependencyObject depObj, string name) where T : FrameworkElement
            {
                DependencyObject child = null;
                T grandChild = null;

                for (int i = 0; i <= VisualTreeHelper.GetChildrenCount(depObj) - 1; i++)
                {
                    child = VisualTreeHelper.GetChild(depObj, i);

                    if (child is T && (((T)child).Name == name | string.IsNullOrEmpty(name)))
                    {
                        return (T)child;
                    }
                    else
                    {
                        // 在下一级中没有找到指定名字的子控件，就再往下一级找
                        grandChild = GetChildControl<T>(child, name);
                        if (grandChild != null)
                            return grandChild;
                    }
                }

                return null;
            }

            /// <summary>
            /// 获取所有同一类型的子控件
            /// </summary>
            /// <typeparam name="T">子控件的类型</typeparam>
            /// <param name="obj">要找的是obj的子控件集合</param>
            /// <param name="name">想找的子控件的Name属性</param>
            /// <returns>子控件集合</returns>
            public static List<T> GetChildControls<T>(DependencyObject depObj, string name) where T : FrameworkElement
            {
                DependencyObject child = null;
                List<T> childList = new List<T>();

                for (int i = 0; i <= VisualTreeHelper.GetChildrenCount(depObj) - 1; i++)
                {
                    child = VisualTreeHelper.GetChild(depObj, i);

                    if (child is T && (((T)child).Name == name || string.IsNullOrEmpty(name)))
                    {
                        childList.Add((T)child);
                    }

                    childList.AddRange(GetChildControls<T>(child, ""));
                }

                return childList;
            }

            public static IEnumerable<T> FindVisualChildren<T>(DependencyObject depObj) where T : DependencyObject
            {
                if (depObj != null)
                {
                    for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
                    {
                        DependencyObject child = VisualTreeHelper.GetChild(depObj, i);

                        if (child != null && child is T)
                            yield return (T)child;

                        foreach (T childOfChild in FindVisualChildren<T>(child))
                            yield return childOfChild;
                    }
                }
            }

            public static T FindVisualChild<T>(DependencyObject obj) where T : DependencyObject
            {
                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
                {
                    DependencyObject child = VisualTreeHelper.GetChild(obj, i);
                    if (child != null && child is T)
                        return (T)child;
                    else
                    {
                        T childOfChild = FindVisualChild<T>(child);
                        if (childOfChild != null)
                            return childOfChild;
                    }
                }
                return null;
            }

        }
        #endregion

        #region 控件布局
        public static class ControlLayoutHelper
        {
            public static Point SearchInsertLocation(Canvas canvas, Size size, int interval)
            {
                Point position = new Point(-1, -1);

                List<DraggableBorder> boarderList = Utils.ControlSearchHelper.GetChildControls<DraggableBorder>(canvas, "");

                if (boarderList.Count > 0)
                {
                    IEnumerable<IGrouping<double, DraggableBorder>> groupedBorders = boarderList.GroupBy(x => x.RectangleGeometry.Rect.Y)
                        .OrderBy(group => group.First().RectangleGeometry.Rect.Y); //按垂直位置分组,并将分组按Y坐标排序

                    double currentSearchVerticalLoc = 0;

                    foreach (IGrouping<double, DraggableBorder> group in groupedBorders)
                    {
                        double currentSearchHerizonLoc = 0;
                        IOrderedEnumerable<DraggableBorder> thisRowBorders = group.OrderBy(x => x.RectangleGeometry.Rect.X);

                        foreach (DraggableBorder border in thisRowBorders)
                        {
                            //检查当前行每个控件之前能否插入
                            double currentBorderHorizonLoc = Canvas.GetLeft(border);
                            double currentBorderVerticalLoc = Canvas.GetTop(border);

                            if (currentBorderVerticalLoc > currentSearchVerticalLoc)
                            {
                                //本行为空，直接添加在行首
                                position = new Point(currentSearchHerizonLoc, currentSearchVerticalLoc);
                                return position;
                            }

                            if (currentSearchHerizonLoc + size.Width + interval <= currentBorderHorizonLoc)
                            {
                                position = new Point(currentSearchHerizonLoc, currentSearchVerticalLoc);
                                return position;
                            }

                            currentSearchHerizonLoc = currentBorderHorizonLoc + border.Width + interval;
                        }

                        //检查当前行最后能否插入
                        if (currentSearchHerizonLoc + size.Width <= canvas.Width)
                        {
                            position = new Point(currentSearchHerizonLoc, currentSearchVerticalLoc);
                            return position;
                        }

                        currentSearchVerticalLoc += size.Height + interval;
                    }

                    //检查下一行能否插入
                    if (size.Width <= canvas.Width && currentSearchVerticalLoc + size.Height <= canvas.Height)
                    {
                        position = new Point(0, currentSearchVerticalLoc);
                        return position;
                    }
                }
                else
                {
                    //画布为空，添加第一个Border
                    if (size.Width <= canvas.Width && size.Height <= canvas.Height)
                    {
                        position = new Point(0, 0);
                        return position;
                    }
                }

                return position;
            }

            /// <summary>
            /// 在Canvas上绘制矩形
            /// </summary>
            /// <param name="canvas">绘制的画布</param>
            /// <param name="rect">绘制矩形的位置</param>
            public static Rectangle DrawRoundRect(Canvas canvas, Rect rect)
            {
                Rectangle rectangle = new Rectangle();
                Canvas.SetLeft(rectangle, rect.X);
                Canvas.SetTop(rectangle, rect.Y);
                rectangle.Stroke = new SolidColorBrush(Colors.Red);
                rectangle.StrokeThickness = 1;
                rectangle.Width = rect.Width;
                rectangle.Height = rect.Height;
                rectangle.RadiusX = 1;
                rectangle.RadiusY = 1;
                canvas.Children.Add(rectangle);

                return rectangle;
            }

            /// <summary>
            /// 根据定义绘制一个Border控件
            /// </summary>
            /// <param name="window">Border所在的窗口</param>
            /// <param name="canvas">Border所在的画布</param>
            /// <param name="buttonDef">按键定义对象</param>
            /// <param name="buttonDef">指示是否展示窗口</param>
            public static DraggableBorder DrawBorderOnCanvas(Window window, Canvas canvas, ButtonDefinition buttonDef, bool isShowWindow = false)
            {
                DraggableBorder draggableBorder = null;

                if (canvas != null && buttonDef != null)
                {
                    double canvasLeft = buttonDef.ButtonRect.Left;
                    double canvasTop = buttonDef.ButtonRect.Top;
                    double width = buttonDef.ButtonRect.Width;
                    double height = buttonDef.ButtonRect.Height;

                    draggableBorder = new DraggableBorder();
                    draggableBorder.SetValue(DraggableBorder.WidthProperty, width);
                    draggableBorder.SetValue(DraggableBorder.HeightProperty, height);
                    draggableBorder.SetValue(DraggableBorder.BorderBrushProperty, new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFA9BFD1")));
                    draggableBorder.SetValue(DraggableBorder.BorderThicknessProperty, new Thickness(1));
                    draggableBorder.SetValue(DraggableBorder.CornerRadiusProperty, new CornerRadius(3));
                    draggableBorder.SetValue(DraggableBorder.CursorProperty, Cursors.Hand);
                    draggableBorder.Style = (Style)window.FindResource("BorderStyle_NoText");
                    draggableBorder.KeyCode = buttonDef.KeyCode;
                    draggableBorder.KeyText = buttonDef.KeyText;
                    draggableBorder.ParentControl = canvas;
                    draggableBorder.RectangleGeometry = new RectangleGeometry()
                    {
                        Rect = new Rect(canvasLeft, canvasTop, width, height)
                    };

                    if (isShowWindow)
                    {
                        draggableBorder.SetValue(DraggableBorder.BackgroundProperty, new SolidColorBrush((Color)ColorConverter.ConvertFromString("#7FAAAAAA")));
                        draggableBorder.SetValue(DraggableBorder.BorderBrushProperty, new SolidColorBrush(Colors.White));
                        draggableBorder.SetValue(DraggableBorder.BorderThicknessProperty, new Thickness(2));

                        //设置阴影效果
                        //DropShadowEffect Color="#7FAAAAAA" ShadowDepth="0" Direction="273" BlurRadius="10" RenderingBias="Quality"
                        DropShadowEffect dropShadowEffect = new DropShadowEffect();
                        dropShadowEffect.Color = (Color)ColorConverter.ConvertFromString("#7FAAAAAA");
                        dropShadowEffect.ShadowDepth = 0.0;
                        dropShadowEffect.Direction = 273.0;
                        dropShadowEffect.BlurRadius = 10.0;
                        dropShadowEffect.RenderingBias = RenderingBias.Quality;
                        draggableBorder.SetValue(DraggableBorder.EffectProperty, dropShadowEffect);
                    }

                    TextBlock textBlock = new TextBlock();
                    textBlock.SetValue(TextBlock.TextWrappingProperty, TextWrapping.NoWrap);
                    textBlock.SetValue(TextBlock.HorizontalAlignmentProperty, HorizontalAlignment.Center);
                    textBlock.SetValue(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center);
                    textBlock.SetValue(TextBlock.FontSizeProperty, 28.0);
                    textBlock.SetValue(TextBlock.IsEnabledProperty, false);

                    if (isShowWindow)
                    {
                        textBlock.SetValue(TextBlock.ForegroundProperty, new SolidColorBrush(Colors.White));
                    }
                    else
                    {
                        textBlock.SetValue(TextBlock.ForegroundProperty, new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF738E9E")));
                    }

                    if (buttonDef.KeyCode > 0 && !string.IsNullOrEmpty(buttonDef.KeyText))
                    {
                        textBlock.SetValue(TextBlock.TextProperty, buttonDef.KeyText);
                    }

                    //if (textBlock.Text.Length > 1 && draggableBorder.KeyCode != 32)
                    //{
                    //    //非Space键，Text长度大于1嵌套ViewBox，防止超出范围
                    //    Viewbox viewBox = new Viewbox();
                    //    viewBox.Child = textBlock;
                    //    draggableBorder.Child = viewBox;
                    //}
                    //else
                    {
                        draggableBorder.Child = textBlock;
                    }

                    canvas.Children.Add(draggableBorder);

                    Canvas.SetLeft(draggableBorder, canvasLeft);
                    Canvas.SetTop(draggableBorder, canvasTop);
                }

                return draggableBorder;
            }

            /// <summary>
            /// 重画当前的Border，指定是否为字体嵌套ViewBox以防止字符超出Border显示范围
            /// </summary>
            /// <param name="draggableBordere">需要重绘的Border</param>
            /// <param name="isAddViewBox">是否需要添加ViewBox</param>
            //public static void ReDrawBorder(DraggableBorder draggableBorder, bool isAddViewBox = false)
            //{
            //    TextBlock textBlock = Utils.ControlSearchHelper.GetChildControl<TextBlock>(draggableBorder, "");

            //    if (textBlock != null)
            //    {
            //        //深拷贝控件
            //        string textBlockXaml = XamlWriter.Save(textBlock);
            //        TextBlock newTextBlock = XamlReader.Parse(textBlockXaml) as TextBlock;

            //        draggableBorder.Child = null;

            //        if (isAddViewBox)
            //        {
            //            Viewbox viewBox = new Viewbox();
            //            viewBox.Child = newTextBlock;
            //            draggableBorder.Child = viewBox;
            //        }
            //        else
            //        {
            //            draggableBorder.Child = newTextBlock;
            //        }
            //    }
            //}

            /// <summary>
            /// 根据定义绘制一个3D按键
            /// </summary>
            /// <param name="window">Border所在的窗口</param>
            /// <param name="canvas">Border所在的画布</param>
            /// <param name="buttonDef">按键定义对象</param>
            //public static void DrawBorderOnCanvasWithImage(Window window, Canvas canvas, ButtonDefinition buttonDef)
            //{
            //    try
            //    {
            //        if (canvas != null && buttonDef != null)
            //        {
            //            double canvasLeft = buttonDef.ButtonRect.Left;
            //            double canvasTop = buttonDef.ButtonRect.Top;
            //            double width = buttonDef.ButtonRect.Width;
            //            double height = buttonDef.ButtonRect.Height;

            //            Color textColor = Colors.Black;

            //            //调整设置
            //            switch (targetSetting.boardThemeType)
            //            {
            //                case BoardThemeType.斜角风格:
            //                    {
            //                        canvasLeft -= (canvasLeft / 50) * 4;
            //                        canvasTop += (canvasTop / 50) * 1;
            //                        textColor = Colors.White;

            //                        if (buttonDef.KeyCode == 32)
            //                        {
            //                            //Space按键
            //                            width = 144;
            //                        }
            //                    }
            //                    break;
            //                case BoardThemeType.立体风格:
            //                    {
            //                        canvasLeft -= (canvasLeft / 50) * 5;
            //                        canvasTop -= (canvasTop / 50) * 7;
            //                        textColor = Colors.Black;

            //                        if (buttonDef.KeyCode == 32)
            //                        {
            //                            //Space按键
            //                            width = 140;
            //                        }
            //                    }
            //                    break;
            //            }

            //            string imagePath = targetSetting.normalImagePath;
            //            Size textSize = targetSetting.normalTextSize;
            //            Point textPos = targetSetting.normalTextPos;

            //            if (buttonDef.KeyCode == 32)
            //            {
            //                //Space按键
            //                imagePath = targetSetting.spaceImagePath;
            //                textSize = targetSetting.spaceTextSize;
            //                textPos = targetSetting.spaceTextPos;
            //            }

            //            BitmapImage bitmapImage = ImageHelper.GetBitmapImage(imagePath);

            //            DraggableBorder draggableBorder = new DraggableBorder();
            //            draggableBorder.SetValue(DraggableBorder.WidthProperty, width);
            //            draggableBorder.SetValue(DraggableBorder.HeightProperty, height);
            //            draggableBorder.SetValue(DraggableBorder.BorderBrushProperty, new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFA9BFD1")));
            //            draggableBorder.SetValue(DraggableBorder.BorderThicknessProperty, new Thickness(0));
            //            draggableBorder.SetValue(DraggableBorder.CornerRadiusProperty, new CornerRadius(5, 5, 5, 5));
            //            draggableBorder.SetValue(DraggableBorder.CursorProperty, Cursors.Hand);
            //            draggableBorder.SetValue(DraggableBorder.BackgroundProperty, new ImageBrush(bitmapImage));
            //            draggableBorder.Style = (Style)window.FindResource("BorderStyle_NoText");
            //            draggableBorder.KeyCode = buttonDef.KeyCode;
            //            draggableBorder.KeyText = buttonDef.KeyText;
            //            draggableBorder.ParentControl = canvas;
            //            draggableBorder.RectangleGeometry = new RectangleGeometry()
            //            {
            //                Rect = new Rect(canvasLeft, canvasTop, width, height)
            //            };

            //            TextBlock textBlock = new TextBlock();
            //            textBlock.SetValue(TextBlock.TextWrappingProperty, TextWrapping.NoWrap);
            //            textBlock.SetValue(TextBlock.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            //            textBlock.SetValue(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center);
            //            textBlock.SetValue(TextBlock.FontSizeProperty, 20.0);
            //            textBlock.SetValue(TextBlock.FontWeightProperty, FontWeights.Normal);
            //            textBlock.SetValue(TextBlock.ForegroundProperty, new SolidColorBrush(textColor));

            //            //Image imageView = new Image();

            //            if (buttonDef.KeyCode > 0 && !string.IsNullOrEmpty(buttonDef.KeyText))
            //            {
            //                textBlock.SetValue(TextBlock.TextProperty, buttonDef.KeyText);
            //                //BitmapImage bitmapImage = ImageHelper.Render(imagePath, buttonDef.KeyText, 20, buttonDef.ButtonRect, new System.Drawing.PointF(10, 0));
            //                //imageView.Source = bitmapImage;
            //                //imageView.Source = ImageCache.GetImage("pic_3d.png", buttonDef.KeyCode, buttonDef.KeyText);
            //            }

            //            Canvas textCanvas = new Canvas();
            //            Viewbox viewBox = new Viewbox();

            //            viewBox.Width = textSize.Width;
            //            viewBox.Height = textSize.Height;
            //            viewBox.Child = textBlock;
            //            textCanvas.Children.Add(viewBox);

            //            Canvas.SetLeft(viewBox, textPos.X);
            //            Canvas.SetTop(viewBox, textPos.Y);

            //            draggableBorder.Child = textCanvas;
            //            canvas.Children.Add(draggableBorder);

            //            Canvas.SetLeft(draggableBorder, canvasLeft);
            //            Canvas.SetTop(draggableBorder, canvasTop);
            //        }
            //    }
            //    catch (Exception ex)
            //    {
            //        System.Console.WriteLine("Utils.ControlLayoutHelper.DrawBorderOnCanvasWithImage:" + ex.Message);
            //    }
            //}

            //    public static void RenderOneBorder(Window window, DraggableBorder draggableBorder, Settings settings,
            //        bool isPressed, bool is3DMode = false, bool isShowWindow = false)
            //    {
            //        try
            //        {
            //            window.Dispatcher.Invoke((ThreadStart)delegate ()
            //            {
            //                if (is3DMode)
            //                {
            //                    string imagePath = settings.normalImagePath;

            //                    if (isPressed)
            //                    {
            //                        if (draggableBorder.KeyCode == 32)
            //                        {
            //                            //Space按键
            //                            imagePath = settings.spaceClickImagePath;
            //                        }
            //                        else
            //                        {
            //                            imagePath = settings.normalClickImagePath;
            //                        }
            //                    }
            //                    else
            //                    {
            //                        if (draggableBorder.KeyCode == 32)
            //                        {
            //                            //Space按键
            //                            imagePath = settings.spaceImagePath;
            //                        }
            //                    }

            //                    BitmapImage bitmapImage = ImageHelper.GetBitmapImage(imagePath);
            //                    draggableBorder.SetValue(DraggableBorder.BackgroundProperty, new ImageBrush(bitmapImage));
            //                }
            //                else
            //                {
            //                    TextBlock tb = Utils.ControlSearchHelper.GetChildControl<TextBlock>(draggableBorder, "");

            //                    if (tb == null)
            //                    {
            //                        return;
            //                    }

            //                    if (settings.boardThemeType == BoardThemeType.简约风格)
            //                    {
            //                        FontSetting fontSetting = null;
            //                        Color background;

            //                        if (isPressed)
            //                        {
            //                            fontSetting = settings.buttonModePressedFontSetting;
            //                            background = settings.buttonModePressedBackground.color;
            //                        }
            //                        else
            //                        {
            //                            fontSetting = settings.buttonModeCommonFontSetting;
            //                            background = settings.buttonModeCommonBackground.color;
            //                        }

            //                        if (fontSetting != null)
            //                        {
            //                            if (tb.FontFamily != fontSetting.wpfFont.Family)
            //                            {
            //                                tb.FontFamily = fontSetting.wpfFont.Family;
            //                            }

            //                            if (tb.FontSize != fontSetting.wpfFont.fontSize)
            //                            {
            //                                tb.FontSize = fontSetting.wpfFont.fontSize;
            //                            }

            //                            if (tb.FontStyle != fontSetting.wpfFont.Style)
            //                            {
            //                                tb.FontStyle = fontSetting.wpfFont.Style;
            //                            }

            //                            if (tb.FontWeight != fontSetting.wpfFont.Weight)
            //                            {
            //                                tb.FontWeight = fontSetting.wpfFont.Weight;
            //                            }

            //                            if (tb.Opacity != (double)(fontSetting.textOpacity * 1.0 / 100))
            //                            {
            //                                tb.Opacity = (double)(fontSetting.textOpacity * 1.0 / 100);
            //                            }

            //                            if (fontSetting.underline)
            //                            {
            //                                tb.TextDecorations = TextDecorations.Underline;
            //                            }
            //                            else
            //                            {
            //                                tb.TextDecorations = null;
            //                            }

            //                            if (((SolidColorBrush)tb.Foreground).Color != fontSetting.fontColor.color && fontSetting.fontColor.color != background)
            //                            {
            //                                if (fontSetting.fontColor.color == Colors.Black)
            //                                {
            //                                    //避免纯黑透明
            //                                    fontSetting.fontColor.color.B++;
            //                                }

            //                                tb.Foreground = new SolidColorBrush(fontSetting.fontColor.color);
            //                            }
            //                        }

            //                        if (background != null)
            //                        {
            //                            //background.ScA = (float)0.5;

            //                            if (background == Colors.Black)
            //                            {
            //                                //避免纯黑透明
            //                                background.B++;
            //                            }

            //                            draggableBorder.Background = new SolidColorBrush(background);

            //                            if (isShowWindow)
            //                            {
            //                                //设置阴影效果
            //                                DropShadowEffect dropShadowEffect = new DropShadowEffect();
            //                                dropShadowEffect.Color = background;
            //                                dropShadowEffect.ShadowDepth = 0.0;
            //                                dropShadowEffect.Direction = 273.0;
            //                                dropShadowEffect.BlurRadius = 10.0;
            //                                dropShadowEffect.RenderingBias = RenderingBias.Quality;
            //                                draggableBorder.SetValue(DraggableBorder.EffectProperty, dropShadowEffect);
            //                            }
            //                        }
            //                    }
            //                    else
            //                    {
            //                        tb.FontFamily = new FontFamily("Microsoft YaHei UI");
            //                        tb.FontSize = 30.0;
            //                        tb.FontStyle = FontStyles.Normal;
            //                        tb.FontWeight = FontWeights.Normal;
            //                        tb.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF738E9E"));

            //                        if (draggableBorder.KeyCode > 0 && !string.IsNullOrEmpty(draggableBorder.KeyText))
            //                        {
            //                            draggableBorder.Style = (Style)window.FindResource("BorderStyle_WithText");
            //                        }
            //                        else
            //                        {
            //                            draggableBorder.Style = (Style)window.FindResource("BorderStyle_NoText");
            //                        }
            //                    }
            //                }
            //            });
            //        }
            //        catch (Exception ex)
            //        {
            //            System.Console.WriteLine("RenderOneBorder error: " + ex.Message);
            //        }
            //    }
        }
        #endregion

        #region 字符转换类
        public static class Converter
        {
            public static string KeyTextFromKey(Key key, out int widthGridCount, out int heightGridCount)
            {
                string result = string.Empty;
                widthGridCount = 2;
                heightGridCount = 2;

                //特殊符号转换
                switch (key)
                {
                    case Key.Space:
                        {
                            result = "Space";
                            widthGridCount = 8;
                        }
                        break;
                    case Key.Escape:
                        {
                            result = "Esc";
                        }
                        break;
                    case Key.CapsLock:
                        {
                            result = "CapsLock";
                            widthGridCount = 4;
                        }
                        break;
                    case Key.Enter:
                        {
                            result = "Enter";
                            widthGridCount = 6;
                        }
                        break;
                    case Key.Back:
                        {
                            result = "Backspace";
                            widthGridCount = 6;
                        }
                        break;
                    case Key.LeftShift:
                    case Key.RightShift:
                        {
                            result = "Shift";
                            widthGridCount = 5;
                        }
                        break;
                    case Key.Down:
                        {
                            result = "↓";
                        }
                        break;
                    case Key.Left:
                        {
                            result = "←";
                        }
                        break;
                    case Key.Up:
                        {
                            result = "↑";
                        }
                        break;
                    case Key.Right:
                        {
                            result = "→";
                        }
                        break;
                    case Key.PageDown:
                        {
                            result = "PgDn";
                            widthGridCount = 4;
                        }
                        break;
                    case Key.PageUp:
                        {
                            result = "PgUp";
                            widthGridCount = 4;
                        }
                        break;
                    case Key.Scroll:
                        {
                            result = "ScrL";
                            widthGridCount = 4;
                        }
                        break;
                    case Key.PrintScreen:
                        {
                            result = "PrSc";
                            widthGridCount = 4;
                        }
                        break;
                    case Key.LeftAlt:
                    case Key.RightAlt:
                        {
                            result = "Alt";
                            widthGridCount = 3;
                        }
                        break;
                    case Key.LeftCtrl:
                    case Key.RightCtrl:
                        {
                            result = "Ctrl";
                            widthGridCount = 3;
                        }
                        break;
                    case Key.Delete:
                        {
                            result = "Dele";
                            widthGridCount = 3;
                        }
                        break;
                    case Key.Home:
                        {
                            result = "Home";
                            widthGridCount = 4;
                        }
                        break;
                    case Key.Tab:
                        {
                            result = "Tab";
                            widthGridCount = 3;
                        }
                        break;
                    case Key.Insert:
                        {
                            result = "Insrt";
                            widthGridCount = 3;
                        }
                        break;
                    case Key.End:
                        {
                            result = "End";
                            widthGridCount = 3;
                        }
                        break;
                    case Key.LWin:
                    case Key.RWin:
                        {
                            result = "Win";
                            widthGridCount = 3;
                        }
                        break;
                    case Key.OemMinus:
                        {
                            result = "-";
                        }
                        break;
                    case Key.OemPlus:
                        {
                            result = "=";
                        }
                        break;
                    case Key.OemComma:
                        {
                            result = ",";
                        }
                        break;
                    case Key.OemPeriod:
                        {
                            result = ".";
                        }
                        break;
                    case Key.Oem1:
                        {
                            result = ";";
                        }
                        break;
                    case Key.Oem2:
                        {
                            result = "/";
                        }
                        break;
                    case Key.Oem3:
                        {
                            result = "`";
                        }
                        break;
                    case Key.Oem4:
                        {
                            result = "[";
                        }
                        break;
                    case Key.Oem5:
                        {
                            result = "\\";
                        }
                        break;
                    case Key.Oem6:
                        {
                            result = "]";
                        }
                        break;
                    case Key.Oem7:
                        {
                            result = "'";
                        }
                        break;
                    case Key.F1:
                    case Key.F2:
                    case Key.F3:
                    case Key.F4:
                    case Key.F5:
                    case Key.F6:
                    case Key.F7:
                    case Key.F8:
                    case Key.F9:
                    case Key.F10:
                    case Key.F11:
                    case Key.F12:
                        {
                            result = key.ToString();
                        }
                        break;
                    //小键盘
                    case Key.NumPad0:
                        {
                            result = "0";
                            widthGridCount = 3;
                        }
                        break;
                    case Key.NumPad1:
                        {
                            result = "1";
                        }
                        break;
                    case Key.NumPad2:
                        {
                            result = "2";
                        }
                        break;
                    case Key.NumPad3:
                        {
                            result = "3";
                        }
                        break;
                    case Key.NumPad4:
                        {
                            result = "4";
                        }
                        break;
                    case Key.NumPad5:
                        {
                            result = "5";
                        }
                        break;
                    case Key.NumPad6:
                        {
                            result = "6";
                        }
                        break;
                    case Key.NumPad7:
                        {
                            result = "7";
                        }
                        break;
                    case Key.NumPad8:
                        {
                            result = "8";
                        }
                        break;
                    case Key.NumPad9:
                        {
                            result = "9";
                        }
                        break;
                    case Key.NumLock:
                        {
                            result = "NumL";
                            widthGridCount = 4;
                        }
                        break;
                    case Key.Multiply:
                        {
                            result = "*";
                        }
                        break;
                    case Key.Divide:
                        {
                            result = "/";
                        }
                        break;
                    case Key.Subtract:
                        {
                            result = "-";
                            heightGridCount = 4;
                        }
                        break;
                    case Key.Add:
                        {
                            result = "+";
                            heightGridCount = 4;
                        }
                        break;
                    case Key.Decimal:
                        {
                            result = ".";
                        }
                        break;
                }

                if (string.IsNullOrEmpty(result))
                {
                    int keyCode = KeyInterop.VirtualKeyFromKey(key);

                    if ((keyCode >= 96 && keyCode <= 105) ||
                        (keyCode >= 48 && keyCode <= 57) ||
                        (keyCode >= 65 && keyCode <= 90))
                    {
                        //数字键和字母文本
                        result = ((char)keyCode).ToString().ToUpper();
                    }
                }

                return result;
            }

            public static FontStyle FontStyleFromString(string fontStyleString)
            {
                switch (fontStyleString)
                {
                    case "Normal": return FontStyles.Normal;
                    case "Italic": return FontStyles.Italic;
                    case "Oblique": return FontStyles.Oblique;
                }

                return FontStyles.Normal;
            }

            public static FontWeight FontWeightFromString(string fontWeightString)
            {
                switch (fontWeightString)
                {
                    case "Normal": return FontWeights.Normal;
                    case "Italic": return FontWeights.Black;
                    case "Oblique": return FontWeights.Bold;
                    case "DemiBold": return FontWeights.DemiBold;
                    case "ExtraBlack": return FontWeights.ExtraBlack;
                    case "ExtraBold": return FontWeights.ExtraBold;
                    case "ExtraLight": return FontWeights.ExtraLight;
                    case "Heavy": return FontWeights.Heavy;
                    case "Light": return FontWeights.Light;
                    case "Medium": return FontWeights.Medium;
                    case "Regular": return FontWeights.Regular;
                    case "SemiBold": return FontWeights.SemiBold;
                    case "Thin": return FontWeights.Thin;
                    case "UltraBlack": return FontWeights.UltraBlack;
                    case "UltraBold": return FontWeights.UltraBold;
                    case "UltraLight": return FontWeights.UltraLight;
                }

                return FontWeights.Normal;
            }
        }
        #endregion

        #region 图片操作类封装
        public static class ImageHelper
        {
            /// <summary>
            /// 解析字节数组成图片
            /// </summary>
            /// <param name="byteArray"></param>
            /// <returns></returns>
            public static BitmapImage ByteArrayToBitmapImage(byte[] byteArray)
            {
                BitmapImage bmp = null;
                try
                {
                    bmp = new BitmapImage();
                    bmp.BeginInit();
                    bmp.StreamSource = new MemoryStream(byteArray);
                    bmp.EndInit();
                }
                catch
                {
                    bmp = null;
                }
                return bmp;
            }


            /// <summary>
            /// 图片数据解析成字节流数组(用于存储到数据库)
            /// </summary>
            /// <param name="bmp"></param>
            /// <returns></returns>
            public static byte[] BitmapImageToByteArray(BitmapImage bmp)
            {
                byte[] byteArray = null;
                try
                {
                    Stream sMarket = bmp.StreamSource;
                    if (sMarket != null && sMarket.Length > 0)
                    {
                        sMarket.Position = 0;
                        using (BinaryReader br = new BinaryReader(sMarket))
                        {
                            byteArray = br.ReadBytes((int)sMarket.Length);
                        }
                    }
                }
                catch
                {
                }
                return byteArray;
            }

            /// <summary>
            /// 根据图片的路径解析成图片资源
            /// </summary>
            /// <param name="filePath"></param>
            /// <returns></returns>
            public static byte[] BitmapImageToByteArray(String filePath)
            {

                byte[] byteArray = null;
                if (File.Exists(filePath))
                    byteArray = File.ReadAllBytes(filePath);
                return byteArray;
            }

            /// <summary>
            /// 根据图片的相对路径 返回 BitmapImage对象的实例化
            /// </summary>
            /// <param name="imgPath">图片的相对路径(如:@"/images/star.png")</param>
            /// <returns></returns>
            public static BitmapImage GetBitmapImage(string imgPath)
            {
                try
                {
                    if (!imgPath.StartsWith("/"))
                    {
                        imgPath = "/" + imgPath;
                    }
                    return new BitmapImage(new Uri("Pack://application:,,," + imgPath));
                }
                catch
                {
                    return null;
                }
            }

            /// <summary>
            /// 根据图片的相对路径 获取Image对象
            /// </summary>
            /// <param name="imgPath">图片的相对路径(如:@"/images/star.png")</param>
            /// <returns></returns>
            public static Image GetImage(string imgPath)
            {
                if (File.Exists(imgPath))
                {
                    Image im = new Image();
                    im.Source = GetBitmapImage(imgPath);
                    return im;
                }
                else
                    return null;
            }

            /// <summary>
            /// 根据图片的相对路径 获取ImageBrush对象 (此对象资源可以直接用于绑定控件的Background属性)
            /// </summary>
            /// <param name="imgPath">图片的相对路径(如:@"/images/star.png")</param>
            /// <returns></returns>
            public static ImageBrush GetImageBrush(string imgPath)
            {
                if (File.Exists(imgPath))
                {
                    ImageBrush ib = new ImageBrush();
                    ib.ImageSource = GetBitmapImage(imgPath);
                    return ib;
                }
                else
                    return null;
            }


            /// <summary>
            /// 把BitmapImage转换为Bitmap
            /// </summary>
            /// <param name="bitmapImage"></param>
            /// <returns></returns>
            public static System.Drawing.Bitmap BitmapFromBitmapImage(BitmapImage bitmapImage)
            {
                return new System.Drawing.Bitmap(bitmapImage.StreamSource);
            }

            /// <summary>
            /// 把Bitmap转换为BitmapImage
            /// </summary>
            /// <param name="bitmap"></param>
            /// <returns></returns>
            public static BitmapImage BitmapImageFromBitmap(System.Drawing.Bitmap bitmap)
            {
                BitmapSource bs = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(bitmap.GetHbitmap(), IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                return (BitmapImage)bs;
            }

            public static BitmapImage Render(string imagePath, string text, float fontSize, Rect rect, System.Drawing.PointF position)
            {

                byte[] buffer = new byte[1024];

                using (var stream = System.Reflection.Assembly.GetExecutingAssembly().
                    GetManifestResourceStream(("BoardShow.pic_3d.png")))
                {
                    if (stream != null)
                    {
                        stream.Read(buffer, 24, 104);
                    }
                }

                BitmapImage bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = new MemoryStream(buffer);
                bitmapImage.EndInit();
                bitmapImage.Freeze();

                if (bitmapImage.StreamSource == null)
                {
                    return null;
                }

                System.Drawing.Bitmap bitmap = BitmapFromBitmapImage(bitmapImage);

                try
                {
                    System.Drawing.Graphics g = System.Drawing.Graphics.FromImage(bitmap);

                    // Render the background image if set.
                    if (!string.IsNullOrEmpty(imagePath) && CalabashTools.FileIO.ExistFile(imagePath))
                    {
                        g.DrawImage(System.Drawing.Image.FromFile(imagePath), new System.Drawing.Rectangle(0, 0, 48, 48));

                        //绘制字体
                        System.Drawing.Font drawFont = new System.Drawing.Font("Microsoft YaHei", fontSize);
                        System.Drawing.SolidBrush sbrush = new System.Drawing.SolidBrush(System.Drawing.Color.Black);
                        g.DrawString(text, drawFont, sbrush, position);
                        MemoryStream ms = new MemoryStream();
                        bitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Bmp);
                    }
                }
                catch (Exception ex)
                {
                    System.Console.WriteLine("Utils.ImageHelper.DrawText:" + ex.Message);
                }

                return BitmapImageFromBitmap(bitmap);
            }
        }
        #endregion

        #region 按键渲染图片缓存

        public enum LayoutIDs
        {
            Layout1 = 1,
            Layout2 = 2,
            Layout3 = 3,
            Layout4 = 4,
            Layout5 = 5,
            Layout6 = 6,
        };

        public enum BoardIDs
        {
            Board1 = 1,
            Board2 = 2
        };

        public static class ImageCache
        {
            /// <summary>
            /// 图片存放的主路径
            /// </summary>
            private static string ImagesFolder = System.IO.Path.Combine(Environment.CurrentDirectory, Encrypt.TEXT("CacheImages"));

            /// <summary>
            /// 缓存构建好的Bitmap.
            /// </summary>
            private static Dictionary<string, System.Drawing.Bitmap> Board1ImageBuffer = new Dictionary<string, System.Drawing.Bitmap>();

            /// <summary>
            /// 缓存构建好的Bitmap.
            /// </summary>
            private static Dictionary<string, System.Drawing.Bitmap> Board2ImageBuffer = new Dictionary<string, System.Drawing.Bitmap>();

            /// <summary>
            /// 重新初始化图片缓存
            /// </summary>
            /// <param name="layoutId">布局信息</param>
            public static void Init(LayoutIDs layoutId)
            {
                ClearCache();

                string board1CacheDir = System.IO.Path.Combine(ImagesFolder, layoutId.ToString(), BoardIDs.Board1.ToString());
                string board2CacheDir = System.IO.Path.Combine(ImagesFolder, layoutId.ToString(), BoardIDs.Board2.ToString());

                try
                {
                    FileInfo[] board1ImageFiles = CalabashTools.FileIO.ListSubFiles(board1CacheDir);

                    foreach (FileInfo imageFile in board1ImageFiles)
                    {
                        System.Drawing.Bitmap bitmap = new System.Drawing.Bitmap(
                            System.Drawing.Image.FromFile(imageFile.FullName));

                        if (bitmap != null)
                        {
                            Board1ImageBuffer.Add(imageFile.FullName, bitmap);
                        }
                    }

                    FileInfo[] board2ImageFiles = CalabashTools.FileIO.ListSubFiles(board2CacheDir);

                    foreach (FileInfo imageFile in board2ImageFiles)
                    {
                        System.Drawing.Bitmap bitmap = new System.Drawing.Bitmap(System.Drawing.Image.FromFile(imageFile.FullName));

                        if (bitmap != null)
                        {
                            Board2ImageBuffer.Add(imageFile.FullName, bitmap);
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Console.WriteLine("ImageCache.Init Faild," + ex.Message);
                }
            }

            /// <summary>
            /// 清除当前图片的缓存
            /// </summary>
            public static void ClearCache()
            {
                Board1ImageBuffer.Clear();
                Board2ImageBuffer.Clear();
            }

            /// <summary>
            /// 根据布局ID、键盘Id和有序的按键序列获得指定的图片路径
            /// </summary>
            /// <param name="layoutId">布局ID</param>
            /// <param name="boardId">键盘ID</param>
            /// <param name="keyCodes">升序的按键码序列（如果为空，则表示无按键按下，文件名为"None"</param>
            /// <returns></returns>
            public static string GetImagePath(LayoutIDs layoutId, string themeName, BoardIDs boardId, int[] keyCodes)
            {
                //按键码序列升序排序
                Array.Sort(keyCodes);

                string boardDir = System.IO.Path.Combine(ImagesFolder, layoutId.ToString(), themeName, boardId.ToString());
                string specificPath = boardDir;

                string fileName = string.Empty;

                if (keyCodes.Length > 0)
                {
                    specificPath = System.IO.Path.Combine(boardDir, keyCodes.Length.ToString());

                    foreach (int keyCode in keyCodes)
                    {
                        if (string.IsNullOrEmpty(fileName))
                        {
                            fileName += keyCode;
                        }
                        else
                        {
                            fileName += "_" + keyCode;
                        }
                    }
                }
                else
                {
                    fileName = "None";
                }

                specificPath = System.IO.Path.Combine(specificPath, fileName + ".png");

                return specificPath;
            }

            /// <summary>
            /// 根据指定的布局ID，键盘ID和有序的按键序列获得图片缓存
            /// </summary>
            /// <param name="layoutId">布局ID</param>
            /// <param name="boardId">键盘ID</param>
            /// <param name="keyCodes">升序的按键码序列（如果为空，则表示无按键按下，文件名为"None"</param>
            /// <returns></returns>
            public static System.Drawing.Bitmap GetImage(LayoutIDs layoutId, string themeName, BoardIDs boardId, int[] keyCodes)
            {
                string specificPath = GetImagePath(layoutId, themeName, boardId, keyCodes);

                if (!CalabashTools.FileIO.ExistFile(specificPath))
                {
                    return null;
                }

                if (boardId == BoardIDs.Board1)
                {
                    if (!Board1ImageBuffer.ContainsKey(specificPath))
                    {
                        return Board1ImageBuffer[specificPath];
                    }
                }
                else
                {
                    if (!Board2ImageBuffer.ContainsKey(specificPath))
                    {
                        return Board2ImageBuffer[specificPath];
                    }
                }

                return null;
            }

            public static bool SaveImage(System.Drawing.Bitmap bitmap, LayoutIDs layoutId, string themeName, BoardIDs boardId, int[] keyCodes)
            {
                string layoutDir = System.IO.Path.Combine(ImagesFolder, layoutId.ToString());
                string boardDir = System.IO.Path.Combine(ImagesFolder, layoutId.ToString(), themeName, boardId.ToString());
                string oneKeyDir = System.IO.Path.Combine(boardDir, "1");
                string twoKeyDir = System.IO.Path.Combine(boardDir, "2");
                string threeKeyDir = System.IO.Path.Combine(boardDir, "3");
                string specificPath = GetImagePath(layoutId, themeName, boardId, keyCodes);

                if (!Directory.Exists(layoutDir))
                {
                    Directory.CreateDirectory(layoutDir);
                }

                if (!Directory.Exists(boardDir))
                {
                    Directory.CreateDirectory(boardDir);
                }

                if (!Directory.Exists(oneKeyDir))
                {
                    Directory.CreateDirectory(oneKeyDir);
                }

                if (!Directory.Exists(twoKeyDir))
                {
                    Directory.CreateDirectory(twoKeyDir);
                }

                if (!Directory.Exists(threeKeyDir))
                {
                    Directory.CreateDirectory(threeKeyDir);
                }

                try
                {
                    bitmap.Save(specificPath);
                }
                catch (Exception ex)
                {
                    System.Console.WriteLine("ImageCache.SaveImage Failed," + ex.Message);
                    return false;
                }

                return true;
            }

            private static BitmapImage BitmapImageFromBitmap(System.Drawing.Bitmap bitmap)
            {
                if (bitmap == null) return null;

                BitmapImage bitmapImage = new BitmapImage();

                try
                {
                    using (System.IO.MemoryStream ms = new System.IO.MemoryStream())
                    {
                        bitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Bmp);
                        bitmapImage.BeginInit();
                        bitmapImage.StreamSource = ms;
                        bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                        bitmapImage.EndInit();
                        bitmapImage.Freeze();
                    }
                }
                catch (Exception ex)
                {
                    System.Console.WriteLine(ex.Message);
                }

                return bitmapImage;
            }
        }

        #endregion

        #region 窗口动画
        public static class WindowAnimations
        {
            /// <summary>
            /// 创建淡入动画
            /// </summary>
            /// <param name="targetWindow">目标窗口</param>
            /// <param name="duration">动画时长</param>
            public static void FadeInAnimation(Window targetWindow, double duration = 300)
            {
                Storyboard storyboard = new Storyboard();
                DoubleAnimation animation1 = CreateDoubleAnimation(0, 1, targetWindow, Window.OpacityProperty, duration);
                storyboard.Children.Add(animation1);

                storyboard.Completed += (sender1, e1) =>
                {
                    storyboard.Stop();
                    storyboard = null;
                };

                storyboard.Begin();
            }

            /// <summary>
            /// 创建淡出动画
            /// </summary>
            /// <param name="targetWindow">目标窗口</param>
            /// <param name="duration">动画时长</param>
            public static void FadeOutAnimation(Window targetWindow, double duration = 300)
            {
                Storyboard storyboard = new Storyboard();
                DoubleAnimation animation1 = CreateDoubleAnimation(1, 0, targetWindow, Window.OpacityProperty, duration);
                storyboard.Children.Add(animation1);

                storyboard.Completed += (sender1, e1) =>
                {
                    storyboard.Stop();
                    storyboard = null;
                    targetWindow.Close();
                };

                storyboard.Begin();
            }

            /// <summary>
            /// 创建DoubleAnimation
            /// </summary>
            /// <param name="from">初始值</param>
            /// <param name="to">结束值</param>
            /// <param name="target">目标DependencyObject</param>
            /// <param name="property">目标属性</param>
            /// <param name="duration">动画时长</param>
            /// <returns></returns>
            private static DoubleAnimation CreateDoubleAnimation(double? from, double? to, DependencyObject target, DependencyProperty property, double duration = 100)
            {
                DoubleAnimation animation = new DoubleAnimation();
                animation.From = from;
                animation.To = to;
                animation.Duration = TimeSpan.FromMilliseconds(duration);
                Storyboard.SetTarget(animation, target);
                Storyboard.SetTargetProperty(animation, new PropertyPath(property));
                return animation;
            }

            /// <summary>
            /// 创建DoubleAnimation
            /// </summary>
            /// <param name="from">初始值</param>
            /// <param name="to">结束值</param>
            /// <param name="target">目标DependencyObject</param>
            /// <param name="property">目标属性</param>
            /// <param name="duration">动画时长</param>
            /// <returns></returns>
            private static DoubleAnimation CreateDoubleAnimation(double? from, double? to, DependencyObject target, string property, double duration = 100)
            {
                DoubleAnimation animation = new DoubleAnimation();
                animation.From = from;
                animation.To = to;
                animation.Duration = TimeSpan.FromMilliseconds(duration);
                Storyboard.SetTarget(animation, target);
                Storyboard.SetTargetProperty(animation, new PropertyPath(property));
                return animation;
            }
        }

        #endregion
    }
}
