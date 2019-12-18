using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
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
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using MoeFM.UWP.Common;
using MoeFM.UWP.Entities;
using MoeFM.UWP.Entities.MoeFMEntity;
using MoeFM.UWP.Entities.Response;
using MoeFM.UWP.OAuth;
using MoeFM.UWP.ViewModel;

// “空白页”项模板在 http://go.microsoft.com/fwlink/?LinkId=234238 上提供

namespace MoeFM.UWP.Pages
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class UserPage : Page
    {
        private readonly ViewItemList<HomePageViewModel> _wikiItems;
        private readonly ViewItemList<HomePageViewModel> _radioItems;
        private readonly ViewItemList<SubItemViewModel> _musicItems;

        public UserPage()
        {
            this.InitializeComponent();
            _wikiItems = new ViewItemList<HomePageViewModel>(LoadWiki) {PageSize = 30, PageIndex = 1};
            _radioItems = new ViewItemList<HomePageViewModel>(LoadRadio) {PageSize = 30, PageIndex = 1};
            _musicItems = new ViewItemList<SubItemViewModel>(LoadMusic) {PageSize = 30, PageIndex = 1};

            EventHandler loadStart = (s, e) => (Window.Current.Content as MainPage)?.ShowRing();
            EventHandler loadCompleted = (s, e) => (Window.Current.Content as MainPage)?.HideRing();

            _wikiItems.LoadStart += loadStart;
            _wikiItems.LoadCompleted += loadCompleted;

            _radioItems.LoadStart += loadStart;
            _radioItems.LoadCompleted += loadCompleted;

            _musicItems.LoadStart += loadStart;
            _musicItems.LoadCompleted += loadCompleted;
        }

        private UserInfoViewModel _userinfo;

        public UserInfoViewModel UserInfo => _userinfo ?? (_userinfo = new UserInfoViewModel());

        private async void BtnLogout_OnClick(object sender, RoutedEventArgs e)
        {
            var mainPage = Window.Current.Content as MainPage;
            if (mainPage == null) return;
            mainPage.LeftMenu.IsLogin = false;
            await OAuthHelper.Logout();
            mainPage.AppFrame.Navigate(typeof (HomePage));
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            var clickInfo = e.Parameter as ClickInfo;
            if (clickInfo != null)
            {
                UserInfo.Avatar = (clickInfo.Data as UserInfoViewModel)?.Avatar;
                UserInfo.Name = (clickInfo.Data as UserInfoViewModel)?.Name;
                if (string.IsNullOrEmpty(UserInfo.Name) && UserInfo.Avatar == null)
                {
                    Task.Run(async () =>
                    {
                        var moeUser = await MoeOp.GetUserInfo();
                        if (moeUser == null)
                        {
                            await this.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                            {
                                BtnLogout_OnClick(null, null);
                            });
                            return;
                        }
                        await this.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                        {
                            this.UserInfo.Name = moeUser.user_nickname;
                            this.UserInfo.Avatar = new BitmapImage(new Uri(moeUser.user_avatar.medium));
                        });
                    });
                }
            }
            if (e.NavigationMode == NavigationMode.Back)
            {
                this.Pivot.SelectedIndex = MoeAppCache.GetData<int?>("PIVOTSELECTEDINDEX").GetValueOrDefault(0);
            }

            this.LsvSongList.SetItemsSource(_musicItems);
            this.LsvWiki.SetItemsSource(_wikiItems);
            this.LsvRadio.SetItemsSource(_radioItems);
            
            base.OnNavigatedTo(e);
        }

        private async Task<List<HomePageViewModel>> LoadWiki(int pageIndex, int pageSize)
        {
            return await LoadWikiOrRadio(WikiType.Music, pageIndex, pageSize);
        }

        private async Task<List<HomePageViewModel>> LoadRadio(int pageIndex, int pageSize)
        {
            return await LoadWikiOrRadio(WikiType.Radio, pageIndex, pageSize);
        }

        private async Task<List<HomePageViewModel>> LoadWikiOrRadio(WikiType wikiType, int pageIndex, int pageSize)
        {
            string dataKey = string.Concat($"MYFAV{wikiType.ToString().ToUpper()}DATA", pageIndex);
            var favs = MoeAppCache.GetData<List<MoeFavSub<MoeWiki>>>(dataKey);
            if (favs == null || favs.Count == 0)
            {
                var url = $"http://api.moefou.org/user/favs/wiki.json?obj_type={wikiType.ToString().ToLower()}&page={pageIndex}&perpage={pageSize}";
                var response = await MoeOp.GetMoeResponseEntity<MoeResponse<MoeFavSubEntity<MoeWiki>>>(url);
                if (response?.response?.favs == null || response?.response?.favs.Count == 0) return null;
                MoeAppCache.SaveData(dataKey, response.response.favs);
                favs = response.response.favs;
                var info = response.response.information;
                if (info.count <= info.page*info.perpage)
                {
                    if (wikiType == WikiType.Music)
                    {
                        _wikiItems.CompleteLoad();
                    }
                    else
                    {
                        _radioItems.CompleteLoad();
                    }
                }
            }
            var models = favs?.Where(item => item.obj != null).Select(item => new HomePageViewModel
            {
                Id = item.obj.wiki_id.ToString(CultureInfo.CurrentCulture),
                Cover = new BitmapImage(new Uri(item.obj.wiki_cover.square)),
                Title = WebUtility.HtmlDecode(item.obj.wiki_title),
                Type = item.obj.wiki_type,
            });
            return models?.ToList();
        }

        private async Task<List<SubItemViewModel>> LoadMusic(int pageIndex, int pageSize)
        {
            string dataKey = string.Concat("MYFAVSONGDATA", pageIndex);
            var favs = MoeAppCache.GetData<List<MoeFavSub<MoeSub>>>(dataKey);
            if (favs == null || favs.Count == 0)
            {
                var url = $"http://api.moefou.org/user/favs/sub.json?page={pageIndex}&perpage={pageSize}&fav_type=1%2C2";
                await MoeOp.WriteLog(OAuthHelper.GenerateRequestUrl(url));
                var response = await MoeOp.GetMoeResponseEntity<MoeResponse<MoeFavSubEntity<MoeSub>>>(url);
                if (response?.response?.favs == null || response.response?.favs?.Count == 0) return null;
                MoeAppCache.SaveData(dataKey, response.response.favs);
                favs = response.response.favs;
                var info = response.response.information;
                if (info.count <= info.page*info.perpage)
                {
                    _musicItems.CompleteLoad();
                }
            }
            var models = favs?.Where(sub => sub?.obj != null).Select(sub => new SubItemViewModel
            {
                Title = WebUtility.HtmlDecode(sub.obj.sub_title),
                Id = sub.obj.sub_id,
                CanPlay = sub.obj.sub_upload?.Count > 0,
                Time = sub.obj.sub_upload?.Count > 0 ? sub.obj.sub_upload[0].up_data.time : "未上传",
                WikiName = WebUtility.HtmlDecode(sub.obj.wiki.wiki_title),
                WikiCover = new BitmapImage(new Uri(sub.obj.wiki.wiki_cover.medium)),
                Relation = sub
            });
            return models?.ToList();
        }

        private void LsvRadio_OnItemClick(object sender, ItemClickEventArgs e)
        {
            var model = e.ClickedItem as HomePageViewModel;
            var mainPage = Window.Current.Content as MainPage;
            MoeAppCache.SaveData("PIVOTSELECTEDINDEX", this.Pivot.SelectedIndex);
            mainPage?.AppFrame.Navigate(typeof (WikiDetailPage), new ClickInfo {Id = (model?.Id), WikiType = WikiType.Radio});
        }

        private void LsvWiki_OnItemClick(object sender, ItemClickEventArgs e)
        {
            var model = e.ClickedItem as HomePageViewModel;
            var mainPage = Window.Current.Content as MainPage;
            MoeAppCache.SaveData("PIVOTSELECTEDINDEX", this.Pivot.SelectedIndex);
            mainPage?.AppFrame.Navigate(typeof (WikiDetailPage), new ClickInfo {Id = (model?.Id), WikiType = WikiType.Music});
        }

        private void WikiPlay_OnClick(object sender, RoutedEventArgs e)
        {
            MoeOp.PlayFavSub();
        }

        private async void Pivot_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.Pivot.SelectedIndex == 2)
            {
                await this.LsvSongList.ListView.LoadMoreItemsAsync();
            }
        }
    }
}
