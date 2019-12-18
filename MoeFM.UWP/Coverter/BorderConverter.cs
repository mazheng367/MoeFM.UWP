using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Data;

namespace MoeFM.UWP.Coverter
{
    public class BorderConverter:IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            bool open = (bool) value;
            return open ? 320 : 38;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
