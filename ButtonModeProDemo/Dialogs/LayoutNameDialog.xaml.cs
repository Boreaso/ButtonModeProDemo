using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace ButtonModeProDemo.Dialogs
{
    /// <summary>
    /// LayoutNameDialog.xaml 的交互逻辑
    /// </summary>
    public partial class LayoutNameDialog : Window
    {
        private Color WindowBorderBrushColor = Color.FromArgb(0xFF, 0x2E, 0x9D, 0xCD);
        private Color WindowBackgroundColor = Color.FromArgb(0xFF, 0x2E, 0x9D, 0xCD);
        public string KeyText = string.Empty;
        public string LayoutName = string.Empty;

        public LayoutNameDialog(string title, string content, string layoutName = "")
        {
            InitializeComponent();

            this.TitleTextBlock.Text = title;
            this.ContentBlock.Text = content;
            this.LayoutName = layoutName;

            this.WindowStartupLocation = WindowStartupLocation.CenterScreen;

            SourceInitialized += new EventHandler(DeleteAltMenuWindow_SourceInitialized);

            this.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (ThreadStart)delegate ()
            {
                this.Topmost = true;
                this.Activate();
            });

            this.LayoutNameTextBox.Focus();

            #region 设置对话框颜色风格
            this.BorderBrush = new SolidColorBrush(this.WindowBorderBrushColor);
            this.Background = new SolidColorBrush(this.WindowBackgroundColor);
            LogoImage.Source = new BitmapImage(new Uri("BoardShow.Images.Logo.BoardShowLogo.ico", UriKind.Relative));
            #endregion
        }

        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            base.OnPreviewKeyDown(e);
        }

        protected override void OnKeyUp(KeyEventArgs e)
        {
            base.OnKeyUp(e);
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }

        private void x_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            returnValue = DialogReturn.OK;
        }

        public DialogReturn returnValue = DialogReturn.Cancel;
        public enum DialogReturn
        {
            Cancel = 0,
            OK = 1
        }

        protected override void OnLostKeyboardFocus(KeyboardFocusChangedEventArgs e)
        {
            base.OnLostKeyboardFocus(e);
        }

        #region 屏蔽alt+space菜单
        [DllImport("user32.dll", EntryPoint = "GetSystemMenu")]
        public static extern IntPtr GetSystemMenu(IntPtr hwnd, int revert);
        [DllImport("user32.dll", EntryPoint = "RemoveMenu")]
        public static extern int RemoveMenu(IntPtr hmenu, int npos, int wflags);
        [DllImport("user32.dll", EntryPoint = "GetMenuItemCount")]
        public static extern int GetMenuItemCount(IntPtr hmenu);

        public const int MF_BYPOSITION = 0x0400;
        public const int MF_DISABLED = 0x0002;

        void DeleteAltMenuWindow_SourceInitialized(object sender, EventArgs e)
        {
            IntPtr handle = new System.Windows.Interop.WindowInteropHelper(this).Handle;
            IntPtr hmenu = GetSystemMenu(handle, 0);
            int cnt = GetMenuItemCount(hmenu);
            for (int i = cnt - 1; i >= 0; i--)
            {
                RemoveMenu(hmenu, i, MF_DISABLED | MF_BYPOSITION);
            }
        }
        #endregion
    }
}
