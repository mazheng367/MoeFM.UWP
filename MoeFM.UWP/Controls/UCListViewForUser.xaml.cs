using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using MoeFM.UWP.Common;
using MoeFM.UWP.Entities;
using MoeFM.UWP.Pages;
using MoeFM.UWP.ViewModel;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace MoeFM.UWP.Controls
{
    public sealed partial class UCListViewForUser : UserControl
    {
        public UCListViewForUser()
        {
            this.InitializeComponent();
        }

        public ListView ListView => this.LsvSongList;

        public void SetItemsSource(object source)
        {
            this.ListView.ItemsSource = source;
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

        private void UIElement_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            e.Handled = true;
            var element = e.OriginalSource as UIElement;
            if (element == null) return;
            DependencyObject source = element;
            while (!(source is SymbolIcon) && !(source is FontIcon))
            {
                source = VisualTreeHelper.GetParent(source);
            }
            FrameworkElement icon = source as FontIcon;
            if (icon != null && "Play".Equals(icon.Tag))
            {
                var model = icon.DataContext as SubItemViewModel;
                if (model?.CanPlay == true)
                {
                    MoeOp.PlayWikiItem(model?.Id.ToString(CultureInfo.CurrentCulture));
                }
            }
            else if ((icon = source as FontIcon) != null && "GoToWiki".Equals(icon.Tag))
            {
                var model = icon.DataContext as SubItemViewModel;
                var mainPage = Window.Current.Content as MainPage;
                var fav = model?.Relation as MoeFavSub<MoeSub>;
                if (fav?.obj?.wiki != null)
                {
                    WikiType wikiType;
                    Enum.TryParse(fav.obj.wiki.wiki_type, true, out wikiType);
                    MoeAppCache.SaveData("PIVOTSELECTEDINDEX", 1);
                    mainPage?.AppFrame.Navigate(typeof(WikiDetailPage), new ClickInfo { Id = fav.obj.wiki.wiki_id.ToString(CultureInfo.CurrentCulture), WikiType = wikiType });
                }
            }
        }

        private void UIElement_OnDoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            var selectedItem = this.LsvSongList.SelectedItem;
            var model = selectedItem as SubItemViewModel;
            if (model?.CanPlay == true)
            {
                MoeOp.PlayWikiItem(model?.Id.ToString(CultureInfo.CurrentCulture));
            }
        }
    }
}
