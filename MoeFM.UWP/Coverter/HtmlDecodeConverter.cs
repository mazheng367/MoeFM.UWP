using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Data;

namespace MoeFM.UWP.Coverter
{
    internal class HtmlDecodeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value == null) return null;
            string s = value.ToString();
            if (string.IsNullOrEmpty(s)) return string.Empty;
            return WebUtility.HtmlDecode(s);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
