using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
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

namespace BoardShow.Dialogs
{
    /// <summary>
    /// UpdatedAutoCloseDialog.xaml 的交互逻辑
    /// </summary>
    public partial class UpdatedAutoCloseDialog : Window
    {
        private DispatcherTimer timer = new DispatcherTimer();

        /// <summary>
        /// UpdatedAutoCloseDialog构造
        /// </summary>
        /// <param name="owner">父窗口</param>
        /// <param name="content">显示内容</param>
        /// <param name="delaySeconds">等待消失的秒数</param>
        public UpdatedAutoCloseDialog(Window owner, string content = " 设置已更新", int delaySeconds = 1)
        {
            InitializeComponent();
            this.Owner = owner;
            this.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            this.UpdatedAutoCloseDialog_TextBox.Text = content;
            timer.Interval = new TimeSpan(0, 0, 0, delaySeconds);
            timer.Tick += timer_Tick;
            timer.Start();
        }

        private void timer_Tick(object sender, EventArgs e)
        {
            timer.Stop();

            Utils.WindowAnimations.FadeOutAnimation(this, 200);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Utils.WindowAnimations.FadeInAnimation(this, 200);
        }
    }
}
