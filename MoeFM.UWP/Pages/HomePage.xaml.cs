using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
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
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using MoeFM.UWP.Common;
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
    public sealed partial class HomePage : Page
    {
        public enum LoadingStatus
        {
            Loading = 1,
            Completed = 2
        }

        class HomeDataInitStatus
        {
            public LoadingStatus NewMusicStatus { get; set; } = LoadingStatus.Loading;
            public LoadingStatus HotMusicAndRadioStatus { get; set; } = LoadingStatus.Loading;
            public LoadingStatus TagMusicStatus { get; set; } = LoadingStatus.Loading;

            public bool AllCompleted()
            {
                return NewMusicStatus == LoadingStatus.Completed && HotMusicAndRadioStatus == LoadingStatus.Completed && TagMusicStatus == LoadingStatus.Completed;
            }
        }

        public HomePage()
        {
            this.InitializeComponent();
            _ringTimer = new DispatcherTimer {Interval = TimeSpan.FromMilliseconds(1000)};
            _ringTimer.Tick += _ringTimer_Tick;
            _ringTimer.Stop();
        }
        
        #region 缓存键常量

        const string TagDataKey = "HOMEPAGEHOTTAGWIKISDATA";
        const string NewMusicDataKey = "HOMEPAGENEWWIKISDATA";
        const string RadioMusicDataKey = "HOMEPAGEMUSICRADIODATA";
        
        #endregion

        private readonly Random _random = new Random();

        private readonly DispatcherTimer _ringTimer;

        private readonly HomeDataInitStatus _initedState = new HomeDataInitStatus();

        #region 进度条

        private void _ringTimer_Tick(object sender, object e)
        {
            if (_initedState.AllCompleted())
            {
                _ringTimer.Stop();
                SwitchRing(0);
            }
        }

        private void SwitchRing(int switchFlag)
        {
            var mainPage = (Window.Current.Content as MainPage);
            if (mainPage == null) return;
            mainPage.Ring.IsActive = switchFlag == 1;
        }

        private void StartRing(LoadingStatus status)
        {
            var mainPage = Window.Current.Content as MainPage;
            if (mainPage != null) mainPage.Ring.IsActive = true;
            status = LoadingStatus.Loading;
            _ringTimer.Start();
        }

        #endregion

        /// <summary>
        /// 获取热门电台和和热门歌曲
        /// </summary>
        /// <returns></returns>
        private async Task<List<HomePageViewModel>[]> GetMusicAndRadio()
        {
            List<HomePageViewModel>[] list = new List<HomePageViewModel>[2];
            var homeData = MoeAppCache.GetData<MoeResponse<MoeHomePageEntity>>(RadioMusicDataKey);
            if (homeData == null)
            {
                var requestUrl = MoeHelper.GenerateRequestUrl("http://moe.fm/explore?api=json&hot_radios=1&hot_musics=1");
                HttpClient client = new HttpClient();
                string json;
                try
                {
                    json = await client.GetStringAsync(requestUrl);
                }
                catch (Exception)
                {
#if DEBUG
                    throw;
#endif
                    return new List<HomePageViewModel>[2];
                }
                homeData = JsonHelper.Parse<MoeResponse<MoeHomePageEntity>>(json);
                MoeAppCache.SaveData(RadioMusicDataKey, homeData);
            }

            var musicData = new List<HomePageViewModel>(homeData.response.hot_musics.Count);
            //热门音乐
            musicData.AddRange(homeData.response.hot_musics?.Select(item => new HomePageViewModel
            {
                Id = item.wiki_id.ToString(CultureInfo.CurrentCulture),
                Cover = new BitmapImage(new Uri(item.wiki_cover.large)),
                Title = WebUtility.HtmlDecode(item.wiki_title),
                Type = "Music"
            }));
            list[0] = musicData;

            //流行电台
            var radioData = new List<HomePageViewModel>(homeData.response.hot_radios.Count);
            radioData.AddRange(homeData.response.hot_radios?.Select(item => new HomePageViewModel
            {
                Id = item.wiki_id.ToString(CultureInfo.CurrentCulture),
                Cover = new BitmapImage(new Uri(item.wiki_cover.large)),
                Title = WebUtility.HtmlDecode(item.wiki_title),
                Type = "Radio"
            }));
            list[1] = radioData;

            return list;
        }

        /// <summary>
        /// 加载最新专辑
        /// </summary>
        private async Task<List<HomePageViewModel>> GetNewMusic()
        {
            string html = MoeAppCache.GetData<string>(NewMusicDataKey);
            try
            {
                if (string.IsNullOrEmpty(html))
                {
                    HttpClient client = new HttpClient();
                    html = await client.GetStringAsync("http://moe.fm/?r=" + DateTime.Now.Ticks.ToString(CultureInfo.CurrentCulture));
                    MoeAppCache.SaveData(NewMusicDataKey, html);
                }
            }
            catch (Exception)
            {
#if DEBUG
                throw;
#endif
                return new List<HomePageViewModel>(0);
            }

            MoePage moePage = new MoePage();
            var moeWikis = moePage.GetNewMusic(html);
            var newWiki = new List<HomePageViewModel>(moeWikis.Count);
            newWiki.AddRange(moeWikis.Select(wiki => new HomePageViewModel
            {
                Id = wiki.wiki_id.ToString(CultureInfo.CurrentCulture),
                Cover = new BitmapImage(new Uri(wiki.wiki_cover.square)),
                Title = WebUtility.HtmlDecode(wiki.wiki_title),
                Type = "Music"
            }));

            return newWiki;
        }

        /// <summary>
        /// 随机加载标签音乐
        /// </summary>
        /// <returns></returns>
        private async Task<List<HomePageViewModel>> GetTagMusic()
        {
            var tagName = _random.Next(0, AppConst.HotTags.Length);
            int thisYear = DateTime.Now.Year, lastYear = thisYear - 10;
            var hotTagWiki = MoeAppCache.GetData<MoeResponse<MoeWikiEntity>>(TagDataKey);
            try
            {
                if (hotTagWiki == null)
                {
                    string url = $"http://api.moefou.org/wikis.json?wiki_type=music&perpage=10&date={lastYear},{thisYear}&tag={AppConst.HotTags[tagName]}";
                    HttpClient client = new HttpClient();
                    string json = await client.GetStringAsync(MoeHelper.GenerateRequestUrl(url));
                    hotTagWiki = JsonHelper.Parse<MoeResponse<MoeWikiEntity>>(json);
                    MoeAppCache.SaveData(TagDataKey, hotTagWiki);
                    MoeAppCache.SaveData(TagDataKey + "NAME", tagName);
                }
                else
                {
                    tagName = MoeAppCache.GetData<int?>(TagDataKey + "NAME").GetValueOrDefault();
                }
            }
            catch (Exception ex) //网络连接错误，返回错误信息
            {
#if DEBUG
                Debug.WriteLine(ex);
                Debug.WriteLine($"http://api.moefou.org/wikis.json?wiki_type=music&perpage=10&date={lastYear},{thisYear}&tag={AppConst.HotTags[tagName]}");
                throw;
#endif
                return new List<HomePageViewModel>(0);
            }

            var dataEntity = new List<HomePageViewModel>(hotTagWiki.response.wikis.Count);
            dataEntity.AddRange(hotTagWiki.response.wikis.Select(item => new HomePageViewModel
            {
                Id = item.wiki_id.ToString(CultureInfo.CurrentCulture),
                Cover = new BitmapImage(new Uri(item.wiki_cover.square)),
                Title = WebUtility.HtmlDecode(item.wiki_title),
                Type = "Music",
                SectionTitle = AppConst.HotTags[tagName]
            }));
            return dataEntity;
        }

        private async void BindMusicAndRadio()
        {
            var modelMusicRadio = await GetMusicAndRadio();
            //绑定热门音乐和电台
            this.LstHotMusic.SetItemsSource(modelMusicRadio.First());
            this.LstHotRadio.SetItemsSource(modelMusicRadio.Last());

            _initedState.HotMusicAndRadioStatus = LoadingStatus.Completed;
        }

        private async void BindNewMusic()
        {
            var modelNewMusic = await GetNewMusic();
            //绑定最新专辑
            this.LstNewWiki.SetItemsSource(modelNewMusic);
            _initedState.NewMusicStatus = LoadingStatus.Completed;
        }

        private async void BindTagMusic()
        {
            var modelTags = await GetTagMusic();
            //绑定标签
            this.LstHotTagMusic.SetItemsSource(modelTags);
            if (modelTags.Count > 0)
            {
                this.LstHotTagMusic.DisplayTitle = $"{modelTags[0].SectionTitle} · · ·";
            }
            _initedState.TagMusicStatus = LoadingStatus.Completed;
        }

        private void FillData()
        {
            SwitchRing(1);
            _ringTimer.Start();

            BindMusicAndRadio();
            BindNewMusic();
            BindTagMusic();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            var mainPage = Window.Current.Content as MainPage;
            if (mainPage != null) mainPage.LeftMenu.CurrentPage = Symbol.Home;
            if (MoeOp.GetNetworkStatus() == -1)
            {
                MoeOp.ShowMessage("网络貌似断开了，请重新检查〒_〒");
            }
            FillData();
            base.OnNavigatedTo(e);
        }

        private void LstNewWiki_OnRefresh(object sender, RoutedEventArgs e)
        {
            MoeAppCache.Remove(NewMusicDataKey);
            StartRing(_initedState.NewMusicStatus);
            BindNewMusic();
        }
        
        private void LstHotTagMusic_OnRefresh(object sender, RoutedEventArgs e)
        {
            MoeAppCache.Remove(TagDataKey);
            MoeAppCache.Remove(TagDataKey + "NAME");
            StartRing(_initedState.TagMusicStatus);
            BindTagMusic();
        }

        private void LstHotMusic_OnRefresh(object sender, RoutedEventArgs e)
        {
            MoeAppCache.Remove(RadioMusicDataKey);
            StartRing(_initedState.HotMusicAndRadioStatus);
            BindMusicAndRadio();
        }

        private void LstHotRadio_OnRefresh(object sender, RoutedEventArgs e)
        {
            MoeAppCache.Remove(RadioMusicDataKey);
            StartRing(_initedState.HotMusicAndRadioStatus);
            BindMusicAndRadio();
        }
    }
}
