using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using MoeFM.UWP.Common;
using MoeFM.UWP.OAuth;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace MoeFM.UWP.Controls
{
    public sealed partial class UCLogin : UserControl
    {
        private readonly MoeOAuth _oAuth = new MoeOAuth();

        public UCLogin()
        {
            this.InitializeComponent();
            this.WebView.NavigationStarting += WebView_NavigationStarting;
            this.WebView.NavigationCompleted += WebView_NavigationCompleted;
        }

        private async void WebView_NavigationStarting(WebView sender, WebViewNavigationStartingEventArgs args)
        {
            var mainPage = Window.Current.Content as MainPage;
            mainPage?.ShowRing();

            if (args.Uri != null && args.Uri.ToString().IndexOf("oauth/verifier", StringComparison.Ordinal) > -1)
            {
                await this.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    this.WebView.NavigateToString("授权中，请不要关闭窗口，授权即将完成.....");
                });
                //验证验证码
                await MoeVerifier(args.Uri.ToString());
            }
        }

        private  void WebView_NavigationCompleted(WebView sender, WebViewNavigationCompletedEventArgs args)
        {
            var mainPage = Window.Current.Content as MainPage;
            mainPage?.HideRing();
        }

        private void WebView_Loaded(object sender, RoutedEventArgs e)
        {
            Task.Run(async () =>
            {
                await _oAuth.GetRequestTokenAsync();
                var authUrl = await _oAuth.GetAuthorizeUrlAsync();
                await this.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    this.WebView.Navigate(new Uri(authUrl));
                });
            });
        }

        private async Task MoeVerifier(string uri)
        {
            var success = await _oAuth.GetAccessTokenAsync(uri);
            if (success)
            {
                var mainPage = Window.Current.Content as MainPage;
                if (mainPage == null) return;
                MoeHelper.ShowNotify("授权成功 (￣▽￣)");
                mainPage.HideLogin();
                mainPage.LeftMenu.IsLogin = true;
                await mainPage.LeftMenu.GetUserInfo();
            }
        }
    }
}
