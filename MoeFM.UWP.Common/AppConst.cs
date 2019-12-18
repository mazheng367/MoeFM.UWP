using System;
using System.Threading.Tasks;
using Windows.Data.Json;
using Windows.Foundation;
using Windows.Storage;
using UnicodeEncoding = Windows.Storage.Streams.UnicodeEncoding;

namespace MoeFM.UWP.Common
{
    public static class AppConst
    {
        public static string TokenFileName => "TokenFileName.bin";

        public static string AccessToken => "access_token";

        public static string AccessTokenSecret => "access_token_secret";

        public static string MoeAppKey => "96e9b6fd2b0d408290031f1f36b8ff1a053bb80f7";

        public static string ConsumerSecret => "a684fb82fe6407af53fffb805ff3e2cf";

        public static readonly string AppStatusKey = "__APPSTATUS__";

        public static readonly string RunningStatusKey = "__APPLICATIONRUNNINGSTATUS__";

        public static IAsyncOperation<bool> GetAppKeyAsync()
        {
            return Task.Run(async () =>
            {
                var item = await ApplicationData.Current.LocalFolder.TryGetItemAsync(TokenFileName);
                var file = item as StorageFile;
                if (file == null) //如果没有授权，则返回原始APP
                {ApplicationData.Current.LocalSettings.Values.Remove(AccessToken);
                    ApplicationData.Current.LocalSettings.Values.Remove(AccessTokenSecret);
                    return false;
                }
                var data = await FileIO.ReadTextAsync(file, UnicodeEncoding.Utf8);
                var jObj = JsonObject.Parse(data);

                ApplicationData.Current.LocalSettings.Values[AccessToken] = jObj.GetNamedString(AccessToken);
                ApplicationData.Current.LocalSettings.Values[AccessTokenSecret] = jObj.GetNamedString("access_token_secret");
                return true;
            }).AsAsyncOperation();
        }

        public static readonly string[] HotTags =
        {
            "东方", "VOCALOID", "OST", "TV动画OP/ED", "初音未来", "miku", "幽閉サテライト", "supercell", "key", "罪恶王冠", "東方project", "sound_horizon", "C81", "治愈", "同人", "EGOIST", "Fate/Zero", "罪恶王冠", "洛天依", "TV动画原声集", "初音", "TRANCE", "游戏人生", "senya", "GalGame", "泽野弘之", "jazz", "上海アリス幻樂団", "化物语", "clannad", "来自风平浪静的明天", "loveLive!", "梶浦由記", "ClariS", "Lovelive", "=v=", "东方project", "某科学的超电磁炮", "少女病", "op", "东京喰种", "Kalafina", "魂音泉", "东京食尸鬼", "动漫", "eva", "fripSide", "麻枝准", "空の境界", "IDOLM@STER", "唱见", "ryo", "神曲", "花洛兮", "あの日見た花の名前を僕達はまだ知らない", "凋叶棕", "命运石之门", "soundhorizon", "EXITTUNES", "伪物语", "Fate", "日常", "歌い手", "茶太", "银魂", "jam", "白色相簿2", "ed", "NICONICO", "高达", "刀剑神域", "djmax", "gumi", "鋼の錬金術師", "空之境界", "已上传", "二次元", "falcom", "RURUTIA", "穢翼のユースティア",
            "ACG", "k-on", "恶魔之谜", "c78", "nico", "zun", "project", "志方あきこ", "air", "KOKIA", "SH", "茅野爱衣", "只有神知道的世界", "缘之空", "c80", "OP/ED", "香菜", "补完", "菅野よう子", "梶浦由纪", "水樹奈々", "Ryu☆", "花泽香菜", "魔法少女まどか☆マギカ", "魔法少女小圆", "V+", "AngelBeats！", "空白", "钢琴", "進撃の巨人", "jubeat", "鹿乃", "虫师", "中二病でも恋がしたい！", "女神异闻录", "未闻花名", "那朵花", "exit", "game", "久石让", "remix", "氷菓", "BGM", "kotoko", "konami", "空之轨迹", "坂本真绫", "V+Miku", "c79", "歌ってみた", "俺の妹がこんなに可愛いわけがない", "no_game_no_life", "霜月はるか", "机巧少女不会受伤", "偶像大师", "菅野洋子", "天依酱", "高达UC", "c82", "Angel_Beats", "水星领航员", "偽物語", "beatmaniaIIDX", "VGM", "bemani", "hardcore", "俺妹", "新海诚", "rewrite", "光之美少女", "ANGELBEATS", "妖精的尾巴", "幸运星", "silver_forest", "交响乐", "目黒将司", "魔塔大陆", "新番", "V家",
            "Lia", "舰娘", "ia", "96猫", "纯音乐", "Own", "天门", "天使", "夶愛丶", "sound", "神前晓", "秒速五厘米", "じん(自然の敵P)", "澤野弘之", "華鳥風月", "ef", "花洛兮·洛天依", "东京喰種", "宫崎骏", "gwave", "灼眼的夏娜", "狼与香辛料", "最终幻想", "东方原声集", "WA2", "进击的巨人", "凉宫春日的忧郁", "物语音乐", "horizon", "催眠的な彼女", "翻唱", "夏目友人帳", "家庭教师", "GuiltyCrown", "little", "発熱巫女～ず", "Suara", "中二病", "刀剑神域", "东方正作", "吉他", "洛奇", "nagi", "炮姐", "初音未来感谢祭", "阳炎project", "触手猴", "sao", "舌尖上的东京", "ff", "燃", "AngelBeats！", "作业用", "ExitTrance", "自制", "角色歌", "holly", "凉宫", "水树奈奈", "Kanon", "GDM", "Steins；Gate", "无法直视", "Gal", "天降之物", "一周的朋友", "AB", "gosick", "FateZero", "加速世界", "M3-29", "梶浦由記", "空の境界", "梶浦由记", "钢琴", "古川本舗", "wow", "eufonius", "东京吃货", "love", "Okawari"
        };
    }
}
