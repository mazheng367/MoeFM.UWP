using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Data.Xml.Dom;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Notifications;
using Windows.UI.Popups;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using MoeFM.UWP.Common;
using MoeFM.UWP.Entities;
using MoeFM.UWP.Entities.MoeFMEntity;
using MoeFM.UWP.Entities.Response;
using MoeFM.UWP.OAuth;
using MoeFM.UWP.Pages;
using MoeFM.UWP.ViewModel;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace MoeFM.UWP.Controls
{
    public sealed partial class MoeLeftMenu : UserControl, INotifyPropertyChanged
    {
        public static readonly DependencyProperty BorderWidthProperty = DependencyProperty.Register(
            "BorderWidth", typeof (double), typeof (MoeLeftMenu), new PropertyMetadata(default(double), BorderWidthChangedCallBack));

        private UserInfoViewModel _userinfo;

        public UserInfoViewModel UserInfo => _userinfo ?? (_userinfo = new UserInfoViewModel());

        public MoeLeftMenu()
        {
            this.InitializeComponent();
            this.IsLogin = OAuthHelper.IsLogin;
            if (this.IsLogin)
            {
                Task.Run(async () => await GetUserInfo()); //获取用户信息
            }
        }

        private bool _isLogin;

        public bool IsLogin
        {
            get { return _isLogin; }
            set
            {
                _isLogin = value;
                OnPropertyChanged();
            }
        }

        public double BorderWidth
        {
            get { return (double) GetValue(BorderWidthProperty); }
            set { SetValue(BorderWidthProperty, value); }
        }

        private Symbol _currentPage;

        public Symbol CurrentPage
        {
            get { return _currentPage; }
            set
            {
                _currentPage = value;
                //取消选中状态
                this.LsvSystemSettings.SelectedIndex = -1;
                this.LsvMyCollection.SelectedIndex = -1;
                switch (_currentPage)
                {
                    case Symbol.Rotate:
                        this.LsvMenuItem.SelectedIndex = 1;
                        break;
                    case Symbol.Audio:
                        this.LsvMenuItem.SelectedIndex = 2;
                        break;
                    case Symbol.Home:
                        this.LsvMenuItem.SelectedIndex = 0;
                        break;
                    case Symbol.Zoom:
                        this.LsvMenuItem.SelectedIndex = 3;
                        break;
                }
            }
        }

        public async Task GetUserInfo()
        {
            try
            {
                var url = OAuthHelper.GenerateRequestUrl("http://api.moefou.org/user/detail.json", true);
                HttpClient client = new HttpClient();
                var json = await client.GetStringAsync(new Uri(url));
                var response = JsonHelper.Parse<MoeResponse<MoeUserEntity>>(json);
                if (response?.response?.user == null)
                {
                    IsLogin = false;
                    return;
                }
                var moeUser = response.response.user;
                await this.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    this.UserInfo.Name = moeUser.user_nickname;
                    this.UserInfo.Avatar = new BitmapImage(new Uri(moeUser.user_avatar.medium));
                });
            }
            catch (HttpRequestException)
            {
                this.IsLogin = false;
                await OAuthHelper.Logout();
            }
            catch
            {
                this.IsLogin = false;
                await OAuthHelper.Logout();
            }
        }

        private static void BorderWidthChangedCallBack(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var leftMenu = (MoeLeftMenu) sender;
            var border1 = leftMenu?.FindName("Border1") as Border;
            if (border1 != null) border1.Width = (double) e.NewValue;
            var border2 = leftMenu?.FindName("Border2") as Border;
            if (border2 != null) border2.Width = (double) e.NewValue;
            var textBlock = border1?.FindName("LblMyCollectionTip") as TextBlock;
            if (textBlock == null) return;
            textBlock.Visibility = (int) Math.Abs((double) e.NewValue - 38) == 0 ? Visibility.Collapsed : Visibility.Visible;
        }

        private void lsvMenuItem_ItemClick(object sender, ItemClickEventArgs e)
        {
            var mainPage = Window.Current.Content as MainPage;
            if (mainPage == null) return;

            SymbolIcon icon = (SymbolIcon) VisualTreeHelper.GetChild((StackPanel) e.ClickedItem, 0);
            switch (icon.Symbol)
            {
                case Symbol.Rotate:
                    mainPage.AppFrame.Navigate(typeof (MusicPage), "Wiki");
                    break;
                case Symbol.Audio:
                    mainPage.AppFrame.Navigate(typeof (MusicPage), "Radio");
                    break;
                case Symbol.Home:
                    mainPage.AppFrame.Navigate(typeof (HomePage));
                    break;
                case Symbol.Zoom:
                    mainPage.AppFrame.Navigate(typeof (SearchPage));
                    break;
            }

            //取消选中状态
            this.LsvSystemSettings.SelectedIndex = -1;
            this.LsvMyCollection.SelectedIndex = -1;
        }

        private void lsvMyCollection_ItemClick(object sender, ItemClickEventArgs e)
        {
            this.LsvMenuItem.SelectedIndex = -1;
            this.LsvSystemSettings.SelectedIndex = -1;

            var item = e.ClickedItem as StackPanel;
            WikiType wikiType;
            Enum.TryParse(item?.Tag?.ToString(), true, out wikiType);
            var mainPage = Window.Current.Content as MainPage;
            mainPage?.AppFrame.Navigate(typeof (UserPage), new ClickInfo {Data = UserInfo, WikiType = wikiType});
        }

        private void lsvSystemSettings_ItemClick(object sender, ItemClickEventArgs e)
        {
            this.LsvMenuItem.SelectedIndex = -1;
            this.LsvMyCollection.SelectedIndex = -1;

            var mainPage = Window.Current.Content as MainPage;
            if (mainPage == null) return;
            SymbolIcon icon = VisualTreeHelper.GetChild((StackPanel) e.ClickedItem, 0) as SymbolIcon;
            switch (icon?.Symbol)
            {
                case Symbol.Setting:
                    mainPage.AppFrame.Navigate(typeof (SettingsPage));
                    break;
                case Symbol.AddFriend:
                    mainPage.ShowLogin();
                    break;
                default:
                    mainPage.AppFrame.Navigate(typeof (UserPage), new ClickInfo {Data = UserInfo});
                    break;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private async void BtnLogout_OnClick(object sender, RoutedEventArgs e)
        {
            await OAuthHelper.Logout();
            this.IsLogin = false;
        }
    }
}
