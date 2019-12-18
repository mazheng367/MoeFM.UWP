using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.ApplicationModel;
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

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace MoeFM.UWP.Controls
{
    public sealed partial class UCGridView : UserControl
    {
        public UCGridView()
        {
            this.InitializeComponent();
        }

        public GridView GridView => this.GridViewMusicInfo;

        public event EventHandler<ItemClickEventArgs> ItemClick;

        public void SetItemsSource(object source)
        {
            this.GridViewMusicInfo.ItemsSource = source;
        }

        private async void UCHomePage_OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (DesignMode.DesignModeEnabled) return;
            await Task.Delay(300);
            await this.Dispatcher.RunAsync(CoreDispatcherPriority.Low, () =>
            {
                var itemsContainer = this.GridViewMusicInfo.ItemsPanelRoot as ItemsWrapGrid;
                if (itemsContainer == null) return;
                if (double.IsNaN(itemsContainer.ItemWidth))
                {
                    itemsContainer.ItemWidth = 184;
                    itemsContainer.ItemHeight = 210;
                }

                if (itemsContainer.ItemWidth >= 190)
                {
                    itemsContainer.ItemWidth = 190;
                    itemsContainer.ItemHeight = 210;
                    return;
                }

                //缩放比例
                double scalePercent, newSizeWidth = e.NewSize.Width;
                //计算默认项宽高时，每行的项数
                int rowItemCount = 0;
                if (e.NewSize.Width - e.PreviousSize.Width > 400) //扩大宽度过大时,首先测试每项的最大宽度
                {
                    rowItemCount = (int)(newSizeWidth / 190);
                    var tmpOutWidth = newSizeWidth - rowItemCount * 190;
                    itemsContainer.ItemWidth = 189;
                    itemsContainer.ItemHeight = 209;
                    if (tmpOutWidth < 20) return;
                    goto L001;
                }

                rowItemCount = (int)(newSizeWidth / itemsContainer.ItemWidth);
                if (rowItemCount > this.GridViewMusicInfo.Items?.Count) return;

                if (rowItemCount < 3) //每行最少显示3个项目
                {
                    scalePercent = (newSizeWidth / 3) / itemsContainer.ItemWidth;
                    itemsContainer.ItemWidth = itemsContainer.ItemWidth * scalePercent;
                    itemsContainer.ItemHeight = itemsContainer.ItemHeight * scalePercent;
                    return;
                }
                L001:
                var outWidth = newSizeWidth - rowItemCount * itemsContainer.ItemWidth;
                if (outWidth < 20) return;

                double newWidth, newHeight;
                if (outWidth >= 70 && outWidth <= 190)//当空白很大时，减少每项的宽高
                {
                    rowItemCount = rowItemCount + 1;

                    double tmpWidth = newSizeWidth / rowItemCount;
                    scalePercent = tmpWidth / itemsContainer.ItemWidth;
                }
                else
                {
                    var itemPlusWidth = outWidth / rowItemCount;
                    if (itemPlusWidth < 1) return;
                    //计算缩放比例
                    scalePercent = (itemsContainer.ItemWidth + itemPlusWidth) / itemsContainer.ItemWidth;
                }

                //根据比例计算宽高
                newWidth = itemsContainer.ItemWidth * scalePercent;
                newHeight = itemsContainer.ItemHeight * scalePercent;

                if (newWidth >= 190) return;
                itemsContainer.ItemWidth = newWidth;
                itemsContainer.ItemHeight = newHeight;
            });
        }

        private void GridViewMusicInfo_OnItemClick(object sender, ItemClickEventArgs e)
        {
            ItemClick?.Invoke(sender, e);
        }
    }
}
