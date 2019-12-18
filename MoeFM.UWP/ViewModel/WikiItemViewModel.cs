using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using MoeFM.UWP.Entities;

namespace MoeFM.UWP.ViewModel
{
    public class WikiItemViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private string _wikiTitle = "努力加载中(￣▽￣)";

        private string _wikiArtist = "未知艺术家(～￣▽￣～)";

        private ImageSource _cover = new BitmapImage(new Uri("ms-appx:///Images/noimage2.jpg"));

        private bool _subFav = false;

        private string _description = "什么都没有留下(●'◡'●)";

        public string Id { get; set; }

        /// <summary>
        /// 标题
        /// </summary>
        public string WikiTitle
        {
            get { return _wikiTitle; }
            set
            {
                _wikiTitle = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// 艺术家
        /// </summary>
        public string WikiArtist
        {
            get { return _wikiArtist; }
            set
            {
                _wikiArtist = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// 封面
        /// </summary>
        public ImageSource Cover
        {
            get { return _cover; }
            set
            {
                _cover = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// 封面
        /// </summary>
        public string Description
        {
            get { return _description; }
            set
            {
                _description = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// 是否收藏
        /// </summary>
        public bool SubFav
        {
            get { return _subFav; }
            set
            {
                _subFav = value;
                OnPropertyChanged();
            }
        }

        public MoeCover WikiCover { get; set; }
    }
}
