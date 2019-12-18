using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using MoeFM.UWP.Common;
using MoeFM.UWP.Entities;

namespace MoeFM.UWP.ViewModel
{
    public class MusicInfoViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private string _title = "o(^▽^)o";
        private string _album = "o(^▽^)o";
        private ImageSource _cover = new BitmapImage(new Uri("ms-appx:///Images/noimg.png"));
        private bool _subFav = false;

        public int? Id { get; set; }

        /// <summary>
        /// 标题
        /// </summary>
        public string Title
        {
            get { return _title; }
            set
            {
                _title = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// 专辑
        /// </summary>
        public string Album
        {
            get { return _album; }
            set
            {
                _album = value;
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

        public bool SubFav
        {
            get { return _subFav; }
            set
            {
                _subFav = value;
                OnPropertyChanged();
            }
        }

        public WikiType WikiType { get; set; }

        public void UpdateData(MusicInfo info)
        {
            if (info == null) return;
            this.Title = info.SubTitle;
            this.Album = info.WikiTitle;
            this.Cover = new BitmapImage(new Uri(info.Cover.small));
            this.SubFav = info.FavSub;
            this.Id = info.Id;
        }
    }
}
