using MoeFM.UWP.Common;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Core;
using Windows.UI.StartScreen;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using MoeFM.UWP.Coverter;
using MoeFM.UWP.Entities;
using MoeFM.UWP.Entities.MoeFMEntity;
using MoeFM.UWP.Entities.Response;
using MoeFM.UWP.OAuth;
using MoeFM.UWP.ViewModel;
using Newtonsoft.Json;

// “空白页”项模板在 http://go.microsoft.com/fwlink/?LinkId=234238 上提供

namespace MoeFM.UWP.Pages
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class WikiDetailPage : Page
    {
        private double _oriHeight = 0;

        private WikiItemViewModel _vm;

        private WikiItemViewModel ViewModel => _vm ?? (_vm = new WikiItemViewModel());

        private readonly ViewItemList<SubItemViewModel> _itemsSource;

        private string _wikiId;
        private WikiType _wikiType;
        private bool _radioLoaded;

        public WikiDetailPage()
        {
            this.InitializeComponent();
            SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility = AppViewBackButtonVisibility.Visible;
            SystemNavigationManager.GetForCurrentView().BackRequested += WikiDetailPage_BackRequested;

            _itemsSource = new ViewItemList<SubItemViewModel>(LoadWikiSubs) {PageIndex = 1, PageSize = 30};

            _itemsSource.LoadStart += (s, e) => (Window.Current.Content as MainPage)?.ShowRing();
            _itemsSource.LoadCompleted += (s, e) => (Window.Current.Content as MainPage)?.HideRing();
        }

        #region 导航事件

        

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            ClickInfo info = e.Parameter as ClickInfo;
            _wikiId = info?.Id;
            _wikiType = (info?.WikiType).GetValueOrDefault(WikiType.Music);
            (Window.Current.Content as MainPage)?.ShowRing();
            LoadWikiDetail();
        }

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            SystemNavigationManager.GetForCurrentView().BackRequested -= WikiDetailPage_BackRequested;
            base.OnNavigatingFrom(e);
        }

        private void WikiDetailPage_BackRequested(object sender, BackRequestedEventArgs e)
        {
            e.Handled = true;
            var frame = Window.Current.Content as MainPage;
            if (frame?.AppFrame.CanGoBack == true)
            {
                frame.AppFrame.GoBack();
            }
        }

        #endregion

        #region 加载方法

        private async void LoadWikiDetail()
        {
            if (string.IsNullOrEmpty(_wikiId)) return;
            MoeWiki wikiItem = MoeAppCache.GetData<MoeWiki>(_wikiId);
            if (wikiItem == null)
            {
                var entity = await MoeOp.GetMoeResponseEntity<MoeResponse<MoeWikiItemEntity>>($"http://api.moefou.org/{_wikiType.ToString().ToLower()}/detail.json?wiki_id={_wikiId}");
                if (entity?.response != null)
                {
                    MoeAppCache.SaveData(_wikiId, entity.response.wiki);
                }
            }
            wikiItem = MoeAppCache.GetData<MoeWiki>(_wikiId);
            if (wikiItem != null)
            {
                ViewModel.Id = wikiItem.wiki_id.ToString(CultureInfo.CurrentCulture);
                ViewModel.WikiTitle = WebUtility.HtmlDecode(wikiItem.wiki_title);
                ViewModel.Cover = new BitmapImage(new Uri(wikiItem.wiki_cover.large));
                ViewModel.WikiCover = wikiItem.wiki_cover;
                string artist = FindMetaValue(wikiItem, "艺术家");
                if (!string.IsNullOrEmpty(artist))
                {
                    ViewModel.WikiArtist = WebUtility.HtmlDecode(artist);
                }
                ViewModel.SubFav = wikiItem.wiki_user_fav != null;
                var description = FindMetaValue(wikiItem, "简介");
                if (!string.IsNullOrEmpty(description))
                {
                    ViewModel.Description = MoeHelper.NoHtml(WebUtility.HtmlDecode(description));
                }
            }
            await Task.Delay(200);
            this.BtnCollapse.Visibility = this.ViewerDescription.ScrollableHeight.Equals(0D) ? Visibility.Collapsed : Visibility.Visible;

            this.LsvSongList.ItemsSource = _itemsSource; //加载完头部，再绑定列表
        }

        private async Task<List<SubItemViewModel>> LoadWikiSubs(int pageIndex, int pageSize)
        {
            try
            {
                List<SubItemViewModel> models;
                if (string.IsNullOrEmpty(_wikiId)) return null;
                if (_wikiType == WikiType.Music)
                {
                    models = await LoadWiki(pageIndex, pageSize);
                }
                else
                {
                    models = await LoadRadio(pageIndex, pageSize);
                }
                return models?.ToList();
            }
            catch (JsonException ex)
            {
                throw new ErrorCanContinueException(ex.Message);
            }
        }

        private async Task<List<SubItemViewModel>> LoadWiki(int pageIndex, int pageSize)
        {
            string dataKey = string.Concat(_wikiId, "LISTVIEWDATA", pageIndex);

            var subs = MoeAppCache.GetData<List<MoeSub>>(dataKey);
            if (subs == null || subs.Count == 0)
            {
                var entity = await MoeOp.GetMoeResponseEntity<MoeResponse<MoeSubEntity>>($"http://api.moefou.org/music/subs.json?wiki_id={_wikiId}&page={pageIndex}&prepage={pageSize}");
                if (entity?.response?.subs != null)
                {
                    MoeAppCache.SaveData(dataKey, entity.response?.subs);
                    var info = entity.response.information;
                    if (info.count <= info.page * info.perpage)
                    {
                        _itemsSource.CompleteLoad();
                    }
                }
                subs = MoeAppCache.GetData<List<MoeSub>>(dataKey);
            }
            else
            {
                await Task.Delay(200);
            }
            var models = subs?.Select((sub, index) => new SubItemViewModel
            {
                Title = WebUtility.HtmlDecode(sub.sub_title),
                Id = sub.sub_id,
                WikiName = WebUtility.HtmlDecode(this.TxtWikiTitle.Text),
                CanPlay = sub.sub_upload?.Count > 0,
                Time = sub.sub_upload?.Count > 0 ? sub.sub_upload[0].up_data.time : "未上传",
                Relation = sub,
                RowIndex = ((pageIndex - 1)*pageSize + (index+1)).ToString("00")
            });
            return models?.ToList();
        }

        private async Task<List<SubItemViewModel>> LoadRadio(int pageIndex, int pageSize)
        {
            if (_radioLoaded)
            {
                return new List<SubItemViewModel>(0);
            }

            string dataKey = string.Concat(_wikiId, "LISTVIEWRADIODATA", pageIndex);
            var relationships = MoeAppCache.GetData<List<MoeRelationship>>(dataKey);
            if (relationships == null || relationships.Count == 0)
            {
                var entity = await MoeOp.GetMoeResponseEntity<MoeResponse<MoeRelationshipEntity>>($"http://api.moefou.org/radio/relationships.json?wiki_id={_wikiId}&page={pageIndex}&prepage={pageSize}");
                if (entity?.response?.relationships != null)
                {
                    MoeAppCache.SaveData(dataKey, entity?.response?.relationships);
                    var info = entity.response.information;
                    if (info.count <= info.page * info.perpage)
                    {
                        _itemsSource.CompleteLoad();
                    }
                }
                relationships = MoeAppCache.GetData<List<MoeRelationship>>(dataKey);
            }
            else
            {
                await Task.Delay(200);
            }
            var models = relationships?.Where(r => r?.obj != null).Select((sub,index) => new SubItemViewModel
            {
                Title = WebUtility.HtmlDecode(sub.obj.sub_title),
                Id = sub.obj.sub_id,
                WikiName = WebUtility.HtmlDecode(this.TxtWikiTitle.Text),
                CanPlay = sub.obj.sub_upload?.Count > 0,
                Time = sub.obj.sub_upload?.Count > 0 ? sub.obj.sub_upload[0].up_data.time : "未上传",
                Relation = sub,
                RowIndex = ((pageIndex - 1) * pageSize + (index + 1)).ToString("00")
            });
            _radioLoaded = true;
            return models?.ToList();
        }

        #endregion

        #region 一般方法

        /// <summary>
        /// 专辑收藏
        /// </summary>
        /// <returns></returns>
        private async Task WikiCollection()
        {
            var result = ViewModel.SubFav ?
                await MoeOp.UnLike(ViewModel.Id, _wikiType == WikiType.Music ? FavType.Music : FavType.Radio) :
                await MoeOp.Like(ViewModel.Id, _wikiType == WikiType.Music ? FavType.Music : FavType.Radio);

            if (result) //更新状态
            {
                MoeWiki wikiItem = MoeAppCache.GetData<MoeWiki>(_wikiId);
                if (wikiItem != null)
                {
                    wikiItem.wiki_user_fav = ViewModel.SubFav ? null : new object();
                }
                ViewModel.SubFav = !ViewModel.SubFav;
            }
        }

        /// <summary>
        /// 固定到开始屏幕
        /// </summary>
        /// <returns></returns>
        private async Task PinToStart()
        {
            try
            {
                var folder = await ApplicationData.Current.LocalFolder.CreateFolderAsync("TileImageCaches", CreationCollisionOption.OpenIfExists);
                IStorageItem item = await folder.TryGetItemAsync($"TileImage-{ViewModel.Id}");
                if (item == null)
                {
                    //下载图片
                    HttpClient client = new HttpClient();
                    var buffer = await client.GetByteArrayAsync(ViewModel.WikiCover.medium);
                    item = await folder.CreateFileAsync($"TileImage-{ViewModel.Id}");
                    await FileIO.WriteBytesAsync((IStorageFile) item, buffer);
                }
                Uri logo = new Uri($"ms-appdata:///local/TileImageCaches/{item.Name}");
                string arguments = JsonHelper.Stringify(new ClickInfo {Id = ViewModel.Id, WikiType = _wikiType});
                SecondaryTile secondaryTile = new SecondaryTile(ViewModel.Id, ViewModel.WikiTitle, arguments, logo, TileSize.Square150x150);
                secondaryTile.VisualElements.ShowNameOnSquare150x150Logo = true;
                secondaryTile.VisualElements.ForegroundText = ForegroundText.Light;
                var pinned = await secondaryTile.RequestCreateAsync();
                if (pinned) MoeOp.ShowMessage("固定成功");
            }
            catch (Exception)
            {
                ;
            }
        }

        #endregion

        private string FindMetaValue(MoeWiki wikiItem, string key)
        {
            var value = wikiItem.wiki_meta?.FirstOrDefault(item => Convert.ToString(item["meta_key"]) == key)?["meta_value"] ?? string.Empty;
            return WebUtility.HtmlDecode(value.ToString() ?? string.Empty);
        }

        private void BtnCollapse_OnClick(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button == null) return;
            if (button.Content?.ToString() == "")
            {
                if (_oriHeight.CompareTo(0D) == 0)
                {
                    _oriHeight = this.GridDescription.ActualHeight;
                }
                this.GridDescription.Height = this.GridDescription.ActualHeight + this.ViewerDescription.ScrollableHeight;
            }
            else
            {
                this.GridDescription.Height = _oriHeight;
            }

        }

        private void ViewerDescription_OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (this.ViewerDescription.ScrollableHeight.Equals(0D) && this.GridWikiDetail.ActualHeight <= 197D)
            {
                this.BtnCollapse.Visibility = Visibility.Collapsed;
            }
            else
            {
                this.BtnCollapse.Visibility = Visibility.Visible;
            }
        }

        private void LsvSongList_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ListView view = sender as ListView;
            if (view == null) return;

            if (e.AddedItems.Count > 0)
            {
                var viewItem = view.ContainerFromItem(e.AddedItems[0]) as ListViewItem;
                DataTemplate dataTemplate = this.Resources["SubItemSelectedTemplate"] as DataTemplate;
                if (viewItem != null && dataTemplate != null) viewItem.ContentTemplate = dataTemplate;
            }
            if (e.RemovedItems.Count > 0)
            {
                var viewItem = view.ContainerFromItem(e.RemovedItems[0]) as ListViewItem;
                DataTemplate dataTemplate = this.Resources["SubItemTemplate"] as DataTemplate;
                if (viewItem != null && dataTemplate != null) viewItem.ContentTemplate = dataTemplate;
            }
        }

        private async void WikiItem_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            e.Handled = true;
            var element = e.OriginalSource as UIElement;
            if (element == null) return;
            DependencyObject source = element;
            while (source != null && !(source is FontIcon))
            {
                source = VisualTreeHelper.GetParent(source);
            }
            FrameworkElement icon = source as FontIcon;
            if ("Play".Equals(icon?.Tag))
            {
                var model = icon?.DataContext as SubItemViewModel;
                if (model?.CanPlay == true)
                {
                    MoeOp.PlayWikiItem(model.Id.ToString(CultureInfo.CurrentCulture));
                }
            }
            else if ("Collect".Equals(icon?.Tag))
            {
                var model = icon.DataContext as SubItemViewModel;
                await MoeOp.Like(model?.Id.ToString(CultureInfo.CurrentCulture), FavType.Song);
            }
        }

        private void LsvSongList_OnDoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            var selectedItem = this.LsvSongList.SelectedItem;
            var model = selectedItem as SubItemViewModel;
            if (model?.CanPlay == true)
            {
                MoeOp.PlayWikiItem(model?.Id.ToString(CultureInfo.CurrentCulture));
            }
        }

        private async void StackPanel_Tapped(object sender, TappedRoutedEventArgs e)
        {
            e.Handled = true;
            var element = e.OriginalSource as DependencyObject;
            int deep = 0;
            if (element == null) return;
            while (!(element is Button) && deep < 15)
            {
                element = VisualTreeHelper.GetParent(element);
                deep++;
            }
            if (!(element is Button)) return;
            var button = element as Button;
            switch (button.Tag?.ToString() ?? string.Empty)
            {
                case "play":
                    MoeOp.PlayWiki(ViewModel.Id, _wikiType);
                    break;
                case "collection":
                    button.IsEnabled = false; //禁止多次点击
                    await WikiCollection();
                    button.IsEnabled = true;
                    break;
                case "pin":
                    button.IsEnabled = false; //禁止多次点击
                    await PinToStart();
                    button.IsEnabled = true;
                    break;
            }
        }
    }
}
