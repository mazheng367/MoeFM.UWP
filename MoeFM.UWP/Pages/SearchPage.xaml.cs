using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.CompilerServices;
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
using MoeFM.UWP.ViewModel;
using Newtonsoft.Json;

// “空白页”项模板在 http://go.microsoft.com/fwlink/?LinkId=234238 上提供

namespace MoeFM.UWP.Pages
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class SearchPage : Page, INotifyPropertyChanged
    {
        private readonly ViewItemList<HomePageViewModel> _searchItems;

        private string _keyword;

        public string Keyword
        {
            get { return _keyword; }
            set
            {
                _keyword = value;
                OnPropertyChanged();
            }
        }

        private const string SavedDataKey = "SEARCHDATAINFO";

        public SearchPage()
        {
            this.InitializeComponent();
            SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility = AppViewBackButtonVisibility.Visible;
            SystemNavigationManager.GetForCurrentView().BackRequested += SearchPage_BackRequested;
            _searchItems = new ViewItemList<HomePageViewModel>(SearchData) {PageIndex = 1, PageSize = 30};
            _searchItems.LoadStart += (s, e) => (Window.Current.Content as MainPage)?.ShowRing();
            _searchItems.LoadCompleted += async (s, e) =>
            {
                var cacheInfo = MoeAppCache.GetData<MusicCacheInfo>(SavedDataKey);
                if (cacheInfo?.ItemPosition > 0)
                {
                    await this.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
                    {
                        await Task.Delay(1000);
                        this.ListViewSearchItem.GridView.UpdateLayout();
                        if (_searchItems.Count > cacheInfo.ItemPosition && cacheInfo.ItemPosition > 0)
                        {
                            this.ListViewSearchItem.GridView.ScrollIntoView(_searchItems[cacheInfo.ItemPosition]);
                        }
                        cacheInfo.ItemPosition = 0;
                    });
                }
                (Window.Current.Content as MainPage)?.HideRing();
            };
        }

        private void SearchPage_BackRequested(object sender, BackRequestedEventArgs e)
        {
            e.Handled = true;
            var frame = Window.Current.Content as MainPage;
            //由于占内存太大，不保留详细页面历史
            if (frame?.AppFrame.CanGoBack == true)
            {
                MoeAppCache.Remove(SavedDataKey);
                frame.AppFrame.Navigate(typeof (HomePage));
            }
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            this.ListViewSearchItem.SetItemsSource(_searchItems);

            if (e.NavigationMode == NavigationMode.New)
            {
                MoeAppCache.Remove(SavedDataKey);
            }
            else if (e.NavigationMode == NavigationMode.Back) //恢复缓存中的数据
            {
                var cacheInfo = MoeAppCache.GetData<MusicCacheInfo>(SavedDataKey);
                Keyword = cacheInfo?.Keyword;
            }
            var mainPage = Window.Current.Content as MainPage;
            if (mainPage != null) mainPage.LeftMenu.CurrentPage = Symbol.Zoom;
            base.OnNavigatedTo(e);
        }

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            SystemNavigationManager.GetForCurrentView().BackRequested -= SearchPage_BackRequested;
            base.OnNavigatingFrom(e);
        }

        private async void AutoSuggestBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            MoeAppCache.Remove(SavedDataKey);
            Keyword = args.QueryText;
            _searchItems.Reset();
            await this.ListViewSearchItem.GridView.LoadMoreItemsAsync();
        }

        private async Task<List<HomePageViewModel>> SearchData(int pageIndex, int pageSize)
        {
            if (string.IsNullOrEmpty(Keyword)) return null;
            string uri = MoeHelper.GenerateRequestUrl($"http://api.moefou.org/search/wiki.json?wiki_type=music%2Cradio&keyword={Keyword}&perpage={pageSize}&page={pageIndex}");
            try
            {
                List<MoeWiki> wikiList = new List<MoeWiki>();
                var cacheInfo = MoeAppCache.GetData<MusicCacheInfo>(SavedDataKey);
                if (cacheInfo == null)
                {
                    cacheInfo = new MusicCacheInfo();
                    MoeAppCache.SaveData(SavedDataKey, cacheInfo);
                }
                if (cacheInfo.MoeWikis.Count == 0 || cacheInfo.CurrentPageIndex < pageIndex)
                {
                    HttpClient client = new HttpClient();
                    var json = await client.GetStringAsync(new Uri(uri));
                    var moe = JsonHelper.Parse<MoeResponse<MoeWikiEntity>>(json);
                    if (moe?.response?.wikis == null || moe.response.wikis?.Count == 0) return null;
                    wikiList.AddRange(moe.response.wikis);
                    cacheInfo.CurrentPageIndex = pageIndex;
                    cacheInfo.Keyword = Keyword;
                    cacheInfo.MoeWikis.AddRange(wikiList);
                    var info = moe.response.information;
                    if (info.count <= info.page*info.perpage)
                    {
                        _searchItems.CompleteLoad();
                    }
                }
                else
                {
                    wikiList.AddRange(cacheInfo.MoeWikis);
                    _searchItems.PageIndex = cacheInfo.CurrentPageIndex;
                }

                var models = wikiList.Select(item => new HomePageViewModel
                {
                    Id = item.wiki_id.ToString(CultureInfo.CurrentCulture),
                    Cover = new BitmapImage(new Uri(item.wiki_cover.large)),
                    Title = WebUtility.HtmlDecode(item.wiki_title),
                    Type = item.wiki_type
                });
                return models?.ToList();
            }
            catch (HttpRequestException ex)
            {
                throw new ErrorCanContinueException(ex.Message);
            }
            catch (JsonException)
            {
                return null;
            }
        }

        private void ListViewSearchItem_OnItemClick(object sender, ItemClickEventArgs e)
        {
            var model = e.ClickedItem as HomePageViewModel;
            var cacheInfo = MoeAppCache.GetData<MusicCacheInfo>(SavedDataKey);
            if (cacheInfo != null)
            {
                var dp = ListViewSearchItem.GridView.ContainerFromItem(e.ClickedItem);
                cacheInfo.ItemPosition = ListViewSearchItem.GridView.IndexFromContainer(dp);
            }
            var mainPage = Window.Current.Content as MainPage;
            WikiType wikiType = WikiType.Music;
            if (!string.IsNullOrEmpty(model?.Type))
            {
                var str = model.Type.Substring(0, 1).ToUpper() + model.Type.Substring(1);
                Enum.TryParse(str, out wikiType);
            }
            mainPage?.AppFrame.Navigate(typeof (WikiDetailPage), new ClickInfo {Id = (model?.Id), WikiType = wikiType});
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
