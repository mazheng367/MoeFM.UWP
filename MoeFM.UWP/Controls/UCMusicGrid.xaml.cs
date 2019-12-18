using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using MoeFM.UWP.Pages;
using MoeFM.UWP.ViewModel;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace MoeFM.UWP.Controls
{
    public sealed partial class UCMusicGrid : UserControl
    {
        public UCMusicGrid()
        {
            this.InitializeComponent();
        }

        public event RoutedEventHandler Refresh;

        public object ItemsSource { get; private set; }

        public void SetItemsSource(object source)
        {
            this.ItemsSource = source;
            this.GridViewMusicInfo.ItemsSource = source;
        }

        public WikiType WikiType { get; set; }

        public static readonly DependencyProperty DisplayTitleProperty = DependencyProperty.Register(
            "DisplayTitle", typeof (string), typeof (UCMusicGrid), new PropertyMetadata(default(string), PropertyChangedCallBack));

        public string DisplayTitle
        {
            get { return (string) GetValue(DisplayTitleProperty); }
            set { SetValue(DisplayTitleProperty, value); }
        }

        public static void PropertyChangedCallBack(DependencyObject o, DependencyPropertyChangedEventArgs args)
        {
            var uc = o as UCMusicGrid;
            var text = uc?.FindName("TxtDisplayTitle") as TextBlock;
            if (text != null) text.Text = (args.NewValue ?? string.Empty).ToString();
        }

        private async void UCHomePage_OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            await Task.Delay(600);
            await this.Dispatcher.RunAsync(CoreDispatcherPriority.Low, () =>
            {
                var itemsContainer = this.GridViewMusicInfo.ItemsPanelRoot as ItemsWrapGrid;
                if (itemsContainer == null) return;
                if (double.IsNaN(itemsContainer.ItemWidth))
                {
                    itemsContainer.ItemWidth = 204;
                    itemsContainer.ItemHeight = 230;
                }

                if (itemsContainer.ItemWidth >= 210)
                {
                    itemsContainer.ItemWidth = 210;
                    itemsContainer.ItemHeight = 230;
                    return;
                }

                //缩放比例
                double scalePercent, newSizeWidth = e.NewSize.Width - 20;
                //计算默认项宽高时，每行的项数
                int rowItemCount = 0;
                if (e.NewSize.Width - e.PreviousSize.Width > 400) //扩大宽度过大时,首先测试每项的最大宽度
                {
                    rowItemCount = (int) (newSizeWidth/210);
                    var tmpOutWidth = newSizeWidth - rowItemCount*210;
                    itemsContainer.ItemWidth = 209;
                    itemsContainer.ItemHeight = 229;
                    if (tmpOutWidth < 20) return;
                    goto L001;
                }

                rowItemCount = (int) (newSizeWidth/itemsContainer.ItemWidth);
                if (rowItemCount > this.GridViewMusicInfo.Items?.Count) return;

                if (rowItemCount < 3) //每行最少显示3个项目
                {
                    scalePercent = (newSizeWidth/3)/itemsContainer.ItemWidth;
                    itemsContainer.ItemWidth = itemsContainer.ItemWidth*scalePercent;
                    itemsContainer.ItemHeight = itemsContainer.ItemHeight*scalePercent;
                    return;
                }
                L001:
                var outWidth = newSizeWidth - rowItemCount*itemsContainer.ItemWidth;
                if (outWidth < 20) return;

                double newWidth, newHeight;
                if (outWidth >= 80 && outWidth <= 210 && itemsContainer.ItemWidth > 160) //当空白很大时，减少每项的宽高
                {
                    rowItemCount = rowItemCount + 1;

                    double tmpWidth = newSizeWidth/rowItemCount;
                    scalePercent = tmpWidth/itemsContainer.ItemWidth;
                }
                else
                {
                    var itemPlusWidth = outWidth/rowItemCount;
                    if (itemPlusWidth < 1) return;
                    //计算缩放比例
                    scalePercent = (itemsContainer.ItemWidth + itemPlusWidth)/itemsContainer.ItemWidth;
                }

                //根据比例计算宽高
                newWidth = itemsContainer.ItemWidth*scalePercent;
                newHeight = itemsContainer.ItemHeight*scalePercent;

                if (newWidth >= 210) return;
                itemsContainer.ItemWidth = newWidth;
                itemsContainer.ItemHeight = newHeight;
            });
        }

        private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
            Refresh?.Invoke(sender, e);
        }

        private void GridViewMusicInfo_OnItemClick(object sender, ItemClickEventArgs e)
        {
            var model = e.ClickedItem as HomePageViewModel;
            var mainPage = Window.Current.Content as MainPage;
            mainPage?.AppFrame.Navigate(typeof (WikiDetailPage), new ClickInfo() {Id = (model?.Id), WikiType = WikiType});
        }
    }
}
