using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using MoeFM.UWP.ViewModel;

namespace MoeFM.UWP.Selector
{
    public class ListViewItemStyleSelector : StyleSelector
    {
        public Style AlternativeItemStyle { get; set; }

        public Style ItemStyle { get; set; }

        public Style DisabledStyle { get; set; }

        protected override Style SelectStyleCore(object item, DependencyObject container)
        {
            var subItem = item as SubItemViewModel;
            if (subItem != null && !subItem.CanPlay && DisabledStyle != null) return DisabledStyle;
            var control = ItemsControl.ItemsControlFromItemContainer(container) as ListView;
            var itemIndex = control?.IndexFromContainer(container);
            if ((itemIndex + 1)%2 == 0 && AlternativeItemStyle != null)
            {
                return AlternativeItemStyle;
            }
            return ItemStyle ?? base.SelectStyleCore(item, container);
        }
    }
}
