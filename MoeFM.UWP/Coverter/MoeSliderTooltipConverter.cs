using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Data;

namespace MoeFM.UWP.Coverter
{
    public class MoeSliderTooltipConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            double d = (double) value;
            var span = TimeSpan.FromSeconds(d);

            return string.Format("{0:D2}:{1:D2}", span.Hours*60 + span.Minutes, span.Seconds);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
