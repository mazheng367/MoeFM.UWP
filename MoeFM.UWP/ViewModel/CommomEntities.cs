using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MoeFM.UWP.Entities;

namespace MoeFM.UWP.ViewModel
{
    public class BarData
    {
        public string Text { get; set; }

        public string Value { get; set; }
    }

    public class MusicCacheInfo
    {
        private List<MoeWiki> _pagedData;

        public List<MoeWiki> MoeWikis => _pagedData ?? (_pagedData = new List<MoeWiki>());
        public int CurrentPageIndex { get; set; } = 1;
        public int ItemPosition { get; set; }
        public string Time { get; set; }
        public string Alpha { get; set; }
        public string Keyword { get; set; }
    }
}
