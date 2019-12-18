using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Composition;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using MoeFM.UWP.Controls;
using MoeFM.UWP.Pages;
using System.Threading.Tasks;

//“空白页”项模板在 http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409 上有介绍

namespace MoeFM.UWP
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
        }

        private readonly DispatcherTimer _timer = new DispatcherTimer() {Interval = TimeSpan.FromSeconds(2)};

        public Frame AppFrame => this.MainFrame;

        public ProgressRing Ring => this.ProgressRing;

        public MoeLeftMenu LeftMenu => this.MoeLeftMenu;

        private void MainFrame_OnNavigating(object sender, NavigatingCancelEventArgs e)
        {
            SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility = AppViewBackButtonVisibility.Collapsed;
            _timer.Tick += _timer_Tick;
        }

        private async void _timer_Tick(object sender, object e)
        {
            await this.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                this.MsgPop.IsOpen = false;
            });
            _timer.Stop();
        }

        public void ShowRing()
        {
            this.Ring.IsActive = true;
        }

        public void HideRing()
        {
            this.Ring.IsActive = false;
        }

        public void ShowMessage(string message)
        {
            this.TxtTip.Text = message;
            this.MsgPop.IsOpen = true;
            _timer.Start();
        }

        public void ShowLogin()
        {
            this.LoginPopContainer.Visibility = Visibility.Visible;
            this.LoginPop.IsOpen = true;
            this.LoginPop.Child = new UCLogin() {Width = LoginPop.Width, Height = LoginPop.Height};
        }

        public void HideLogin()
        {
            this.LoginPopContainer.Visibility = Visibility.Collapsed;
            this.LoginPop.IsOpen = false;
            this.LoginPop.Child = null;
        }

        private async void MsgPop_Tapped(object sender, TappedRoutedEventArgs e)
        {
            await this.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                this.MsgPop.IsOpen = false;
            });
            _timer.Stop();
            e.Handled = true;
        }

        private void Grid_Tapped(object sender, TappedRoutedEventArgs e)
        {
            LoginPopContainer.Visibility = Visibility.Collapsed;
            LoginPop.IsOpen = false;
        }
    }
}
