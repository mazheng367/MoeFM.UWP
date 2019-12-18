using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
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
    ///     可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class MusicPage : Page
    {
        private const string SavedDataKey = "DETAILWIKIINFO";

        private readonly ViewItemList<HomePageViewModel> _wikiItems;
        private string _alphaValue = string.Empty;

        private string _timeValue = string.Empty;

        private WikiType _wikiType;

        public MusicPage()
        {
            InitializeComponent();
            SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility = AppViewBackButtonVisibility.Visible;
            SystemNavigationManager.GetForCurrentView().BackRequested += MusicPage_BackRequested;
            _wikiItems = new ViewItemList<HomePageViewModel>(LoadWikiData) {PageIndex = 1};
            ListViewMusic.SetItemsSource(_wikiItems);

            _wikiItems.LoadStart += (s, e) => (Window.Current.Content as MainPage)?.ShowRing();
            _wikiItems.LoadCompleted += async (s, e) =>
            {
                var cacheInfo = MoeAppCache.GetData<MusicCacheInfo>(SavedDataKey);
                if (cacheInfo?.ItemPosition > 0)
                {
                    await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
                    {
                        await Task.Delay(1000);
                        ListViewMusic.GridView.UpdateLayout();
                        if (_wikiItems.Count > cacheInfo.ItemPosition && cacheInfo.ItemPosition > 0)
                        {
                            ListViewMusic.GridView.ScrollIntoView(_wikiItems[cacheInfo.ItemPosition]);
                        }
                        cacheInfo.ItemPosition = 0;
                    });
                }
                (Window.Current.Content as MainPage)?.HideRing();
            };
        }

        private void MusicPage_BackRequested(object sender, BackRequestedEventArgs e)
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

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            SystemNavigationManager.GetForCurrentView().BackRequested -= MusicPage_BackRequested;
            base.OnNavigatingFrom(e);
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (e.NavigationMode == NavigationMode.New)
            {
                MoeAppCache.Remove(SavedDataKey);
            }
            else if (e.NavigationMode == NavigationMode.Back) //恢复缓存中的数据
            {
                var cacheInfo = MoeAppCache.GetData<MusicCacheInfo>(SavedDataKey);
                _timeValue = cacheInfo?.Time;
                _alphaValue = cacheInfo?.Alpha;
            }
            BindSearchBar();
            if ("Radio".Equals(e.Parameter))
            {
                _wikiType = WikiType.Radio;
            }
            var mainPage = Window.Current.Content as MainPage;
            if (mainPage != null) mainPage.LeftMenu.CurrentPage = _wikiType == WikiType.Music ? Symbol.Rotate : Symbol.Audio;
            base.OnNavigatedTo(e);
        }

        private void BindSearchBar()
        {
            var alphas = new List<BarData> {new BarData {Text = "#", Value = string.Empty}};
            alphas.AddRange(Enumerable.Range(65, 26).Select(i => new BarData {Text = ((char) i).ToString(), Value = ((char) i).ToString()}));
            GvAlpha.ItemsSource = alphas;
            GvAlpha.SelectedIndex = 0;
            if (!string.IsNullOrEmpty(_alphaValue))
            {
                var index = alphas.FindIndex(item => _alphaValue.Equals(item.Value));
                if (index > 0) GvAlpha.SelectedIndex = index;
            }
            GvAlpha.SelectionChanged -= GvAlpha_SelectionChanged;
            GvAlpha.SelectionChanged += GvAlpha_SelectionChanged;

            var times = new List<BarData> {new BarData {Text = "全部", Value = string.Empty}};
            for (var i = DateTime.Now.Year; i >= 2006; i--)
            {
                times.Add(new BarData {Text = i.ToString(), Value = i.ToString()});
            }
            times.Add(new BarData {Text = "更早", Value = "old"});
            GvTime.ItemsSource = times;
            GvTime.SelectedIndex = 0;
            if (!string.IsNullOrEmpty(_timeValue))
            {
                var index = times.FindIndex(item => _timeValue.Equals(item.Value));
                if (index > 0) GvTime.SelectedIndex = index;
            }
            GvTime.SelectionChanged -= GvTime_SelectionChanged;
            GvTime.SelectionChanged += GvTime_SelectionChanged;
        }

        private Uri GetPostUri(int pageIndex)
        {
            var uri = $"http://api.moefou.org/wikis.json?wiki_type={_wikiType.ToString().ToLower()}&perpage={_wikiItems.PageSize}&page={pageIndex}";
            if (!string.IsNullOrEmpty(_alphaValue))
            {
                uri = uri + $"&initial={_alphaValue}";
            }
            if (!string.IsNullOrEmpty(_timeValue))
            {
                if (!_timeValue.Equals("old"))
                {
                    uri = uri + $"&date={_timeValue + "-01"},{_timeValue + "-12"}";
                }
                else
                {
                    uri = uri + $"&date={1989 + "-01"},2006-12";
                }
            }
            return new Uri(MoeHelper.GenerateRequestUrl(uri));
        }

        private async Task<List<HomePageViewModel>> LoadWikiData(int pageIndex, int pageSize)
        {
            try
            {
                var cacheInfo = MoeAppCache.GetData<MusicCacheInfo>(SavedDataKey);
                var wikiList = new List<MoeWiki>();
                if (cacheInfo == null)
                {
                    cacheInfo = new MusicCacheInfo();
                    MoeAppCache.SaveData(SavedDataKey, cacheInfo);
                }
                if (cacheInfo.MoeWikis.Count == 0 || cacheInfo.CurrentPageIndex < pageIndex)
                {
                    var client = new HttpClient();
                    var json = await client.GetStringAsync(GetPostUri(pageIndex));
                    var entity = JsonHelper.Parse<MoeResponse<MoeWikiEntity>>(json);
                    if (entity?.response?.wikis == null || entity.response?.wikis.Count == 0) return new List<HomePageViewModel>(0);

                    wikiList.AddRange(entity.response?.wikis);
                    cacheInfo.CurrentPageIndex = pageIndex;
                    cacheInfo.Alpha = _alphaValue;
                    cacheInfo.Time = _timeValue;
                    cacheInfo.MoeWikis.AddRange(wikiList);
                }
                else
                {
                    wikiList.AddRange(cacheInfo.MoeWikis);
                    _wikiItems.PageIndex = cacheInfo.CurrentPageIndex;
                }
                var models = wikiList.Select(item => new HomePageViewModel
                {
                    Id = item.wiki_id.ToString(CultureInfo.CurrentCulture),
                    Cover = new BitmapImage(new Uri(item.wiki_cover.large)),
                    Title = WebUtility.HtmlDecode(item.wiki_title),
                    Type = _wikiType.ToString()
                });
                return models?.ToList();
            }
            catch (JsonException ex)
            {
                throw new ErrorCanContinueException(ex.Message);
            }
        }

        private async void GvTime_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            MoeAppCache.Remove(SavedDataKey);
            _timeValue = (e.AddedItems[0] as BarData)?.Value;
            _wikiItems.Reset();
            await ListViewMusic.GridView.LoadMoreItemsAsync();
        }

        private async void GvAlpha_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            MoeAppCache.Remove(SavedDataKey);
            _alphaValue = (e.AddedItems[0] as BarData)?.Value;
            _wikiItems.Reset();
            await ListViewMusic.GridView.LoadMoreItemsAsync();
        }

        private void ListViewMusic_OnItemClick(object sender, ItemClickEventArgs e)
        {
            var model = e.ClickedItem as HomePageViewModel;
            var cacheInfo = MoeAppCache.GetData<MusicCacheInfo>(SavedDataKey);
            if (cacheInfo != null)
            {
                var dp = ListViewMusic.GridView.ContainerFromItem(e.ClickedItem);
                cacheInfo.ItemPosition = ListViewMusic.GridView.IndexFromContainer(dp);
            }
            var mainPage = Window.Current.Content as MainPage;
            mainPage?.AppFrame.Navigate(typeof (WikiDetailPage), new ClickInfo {Id = (model?.Id), WikiType = _wikiType});
        }
    }
}