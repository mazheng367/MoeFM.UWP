using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Media.Imaging;
using MoeFM.UWP.Common;

namespace MoeFM.UWP.ViewModel
{
    public class HomePageViewModel
    {
        public string Id { get; set; }

        public BitmapImage Cover { get; set; }

        public string Title { get; set; }

        public string Type { get; set; }

        public string SectionTitle { get; set; }
    }
}
