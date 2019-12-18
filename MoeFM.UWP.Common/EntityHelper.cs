using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation.Collections;
using MoeFM.UWP.Entities;
using Newtonsoft.Json;

namespace MoeFM.UWP.Common
{
    public static class EntityHelper
    {
        public static string MediaKey => "MediaKey";

        public static MoeMessage ParseMoeMessage(ValueSet o)
        {
            if (o == null) return null;
            if (!o.ContainsKey(MediaKey)) return null;
            var mediaTansfer = JsonConvert.DeserializeObject<MoeMessage>(Convert.ToString(o[MediaKey]));
            return mediaTansfer;
        }

        public static ValueSet GetTransfer(MessageType command, MusicInfo sendValue = null)
        {
            ValueSet message = new ValueSet();
            //发送消息，后台播放器已经生成完成
            message.Add(MediaKey, JsonConvert.SerializeObject(new MoeMessage {Command = command, Item = sendValue}));
            return message;
        }
    }
}
