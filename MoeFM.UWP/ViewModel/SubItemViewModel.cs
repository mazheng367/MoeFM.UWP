using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Media.Imaging;

namespace MoeFM.UWP.ViewModel
{
    public class SubItemViewModel
    {
        public string RowIndex { get; set; }
        public int Id { get; set; }
        public string Title { get; set; }
        public string WikiName { get; set; }
        public string Time { get; set; }
        public bool Fav { get; set; }
        public bool CanPlay { get; set; }
        public object Relation { get; set; } //关联对象
        public BitmapImage WikiCover { get; set; }
        public bool CurrentPlaying { get; set; } //正在播放的歌曲
    }
}
