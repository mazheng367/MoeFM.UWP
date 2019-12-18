using System.Runtime.Serialization;

namespace MoeFM.UWP.Entities
{
    [DataContract]
    public enum MessageType
    {
        PlayNext = 3,
        PlayPrev = 4,
        SetPosition = 5,
        FillMusicInfo = 6,
        RepeatOne = 7,
        Shuffle = 8,
        PlayWiki = 20,
        PlayWikiItem = 21,
        PlayFavSub = 22,
        // ReSharper disable once InconsistentNaming
        RefreshMusicUI = 70,
        MediaPlay = 80,
        MediaPause = 81,
        BackgroundInited = 99,
        CancelTask = 9999
    }


    [DataContract]
    public class MoeMessage
    {
        [DataMember]
        public MessageType Command { get; set; }

        [DataMember]
        public MusicInfo Item { get; set; }

        [DataMember]
        public double Position { get; set; }

        [DataMember]
        public string DataId { get; set; }

        [DataMember]
        public string WikiType { get; set; }
    }
}