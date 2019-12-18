 // ReSharper disable InconsistentNaming

using System.Collections.Generic;

namespace MoeFM.UWP.Entities
{
    public class PlayListItem
    {
        public long up_id { get; set; }
        public string url { get; set; }
        public string stream_length { get; set; }
        public string stream_time { get; set; }
        public long file_size { get; set; }
        public string file_type { get; set; }
        public long wiki_id { get; set; }
        public string wiki_type { get; set; }
        public MoeCover cover { get; set; }
        public string title { get; set; }
        public string wiki_title { get; set; }
        public string wiki_url { get; set; }
        public long sub_id { get; set; }
        public string sub_type { get; set; }
        public string sub_title { get; set; }
        public string sub_url { get; set; }
        public string artist { get; set; }
        public Dictionary<string, object> fav_wiki { get; set; }
        public MoeFavSub<object> fav_sub { get; set; }
    }
}