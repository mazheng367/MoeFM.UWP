using System.Runtime.Serialization;

namespace MoeFM.UWP.Entities
{
    [DataContract]
    public class MusicInfo
    {
        [DataMember]
        public int Id { get; set; }

        [DataMember]
        public string Url { get; set; }

        [DataMember]
        public string SubTitle { get; set; }

        [DataMember]
        public MoeCover Cover { get; set; }

        [DataMember]
        public string Artist { get; set; }

        [DataMember]
        public string WikiTitle { get; set; }

        [DataMember]
        public bool FavSub { get; set; }

        [DataMember]
        public bool IsTarget { get; set; }

        [DataMember]
        public bool HasItems { get; set; }
    }
}