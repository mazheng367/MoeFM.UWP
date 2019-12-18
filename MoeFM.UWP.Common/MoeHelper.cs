using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.Data.Xml;
using Windows.UI.Notifications;
using Windows.Data.Json;
using Windows.Data.Xml.Dom;
using Windows.Foundation;
using Windows.Foundation.Collections;
using MoeFM.UWP.Entities;
using Newtonsoft.Json;

namespace MoeFM.UWP.Common
{
    public sealed class MoeHelper
    {
        ///// <summary>
        ///// 填充音乐播放列表
        ///// </summary>
        ///// <returns></returns>
        //private static async Task<IList<PlayList>> FillDataToPlayList(string wikiId, string wikiType, bool useTask)
        //{
        //    List<PlayList> playList = new List<PlayList>();
        //    int pageIndex = 1;
        //    while (true)
        //    {
        //        string jsonStr = await GetJsonObject(wikiId, wikiType, pageIndex++);
        //        if (string.IsNullOrEmpty(jsonStr)) { continue; }
        //        JsonObject jsonObj = JsonObject.Parse(jsonStr);
        //        bool isTarget = jsonObj.GetNamedObject("response").GetNamedObject("information").GetNamedBoolean("is_target");
        //        if (!isTarget) //表示没有数据
        //        {
        //            break; //并且列表中已经存在数据，则退出循环
        //        }
        //        JsonArray jsonArray = jsonObj.GetNamedObject("response").GetNamedArray("playlist");
        //        if (jsonArray != null && jsonArray.Count > 0)
        //        {
        //            foreach (IJsonValue value in jsonArray)
        //            {
        //                var curObj = value.GetObject();
        //                var playItem = new PlayList();

        //                playItem.id = (int) curObj.GetNamedNumber("sub_id");
        //                playItem.artist = curObj.GetNamedString("Artist");
        //                playItem.cover = curObj.GetNamedObject("Cover").Stringify();
        //                playItem.sub_title = curObj.GetNamedString("SubTitle");
        //                playItem.url = curObj.GetNamedString("Url");
        //                playItem.wiki_title = curObj.GetNamedString("WikiTitle");
        //                playItem.fav_sub = string.Empty;
        //                if (curObj.ContainsKey("FavSub"))
        //                {
        //                    playItem.fav_sub = curObj["FavSub"].Stringify();
        //                }

        //                playList.Add(playItem);
        //            }
        //        }
        //    }
        //    return playList;
        //}

        ///// <summary>
        ///// 生成Json
        ///// </summary>
        ///// <param name="wikiId"></param>
        ///// <param name="wikiType"></param>
        ///// <param name="pageIndex"></param>
        ///// <returns></returns>
        //private static Task<string> GetJsonObject(string wikiId, string wikiType, int pageIndex)
        //{
        //    //string uri = string.Format("http://moe.fm/listen/playlist?api=json&perpage=30&{0}={1}&page={2}"
        //    //    , wikiType
        //    //    , wikiId
        //    //    , pageIndex.ToString());
        //    //uri = GenerateRequestUrl(uri);
        //    //try
        //    //{
        //    //    WebRequest request = WebRequest.Create(new Uri(uri));
        //    //    request.Method = "GET";
        //    //    var response = await request.GetResponseAsync();
        //    //    string jsonStr;
        //    //    using (StreamReader sr = new StreamReader(response.GetResponseStream()))
        //    //    {
        //    //        jsonStr = await sr.ReadToEndAsync();
        //    //    }
        //    //    return jsonStr;
        //    //}
        //    //catch (WebException)
        //    //{
        //    //    return string.Empty;
        //    //}
        //    return null;
        //}

        /// <summary>
        /// 播放日志
        /// </summary>
        /// <param name="json"></param>
        /// <returns></returns>
        public static IAsyncOperation<bool> AddLogAsync(string json)
        {
            return Task.Run(() => AddLogInner(json)).AsAsyncOperation();
        }

        private static async Task<bool> AddLogInner(string json)
        {

            const string fileName = "PlayList.lst";
            var localFolder = Windows.Storage.ApplicationData.Current.LocalFolder;
            var file = await localFolder.CreateFileAsync(fileName, Windows.Storage.CreationCollisionOption.OpenIfExists);
            var item = JsonConvert.DeserializeObject<dynamic>(json);

            item.playedTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            var context = await Windows.Storage.FileIO.ReadTextAsync(file);
            if (string.IsNullOrEmpty(context))
            {
                context = "[]";
            }
            var logs = JsonConvert.DeserializeObject<List<dynamic>>(context);
            if (logs.Count == 0)
            {
                logs.Add(JsonConvert.DeserializeObject<dynamic>(json));
                await Windows.Storage.FileIO.WriteTextAsync(file, JsonConvert.SerializeObject(logs));
            }
            else
            {
                var findItem = logs.FirstOrDefault(f => f.id == item.id);
                if (findItem != null)
                {
                    //如果找到以前播放的历史，则修改播放时间
                    var index = logs.IndexOf(findItem);
                    logs[index].playedTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                }
                else
                {
                    //加入播放历史
                    logs.Add(item);
                }
                await Windows.Storage.FileIO.WriteTextAsync(file, JsonConvert.SerializeObject(logs));
            }
            return true;
        }

        /// <summary>
        /// 截取字符
        /// </summary>
        /// <param name="str">要截取的信息</param>
        /// <param name="size">显示个数</param>
        /// <returns>字符串</returns>
        public static string GetShowInfo2(string str, int size)
        {
            if (string.IsNullOrEmpty(str)) { return string.Empty; }
            str = NoHtml(WebUtility.HtmlDecode(str));
            int alpha = 0, word = 0;
            for (int i = 0; i < str.Length; i++)
            {
                if (Encoding.UTF8.GetByteCount(str[i].ToString()) == 1)
                {
                    alpha++;
                }
                else
                {
                    word++;
                }
            }
            int newSize = word + alpha/2;
            if (newSize <= size)
            {
                return str;
            }
            char[] charbuffer = new char[str.Length];
            int position = 0;
            var utf8 = Encoding.UTF8;
            for (int index = 0; index < size && position < str.Length - 2;)
            {
                if (position == str.Length - 2)
                {
                    charbuffer[position] = str[position++];
                    charbuffer[position] = str[position++];
                    break;
                }
                if (utf8.GetByteCount(str[position].ToString()) == 1 && utf8.GetByteCount(str[position + 1].ToString()) == 1)
                {
                    charbuffer[position] = str[position++];
                    charbuffer[position] = str[position++];
                    index++;
                }
                else
                {
                    if (utf8.GetByteCount(str[position].ToString()) > 1)
                    {
                        index++;
                    }
                    charbuffer[position] = str[position++];
                }
            }
            if (position != charbuffer.Length)
            {
                char[] newBuffer = new char[position];
                for (int i = 0; i < position; i++)
                {
                    newBuffer[i] = charbuffer[i];
                }
                var s = new string(newBuffer);
                return s.Trim() + "...";
            }
            return Regex.Replace(new string(charbuffer), @"\s\0", string.Empty, RegexOptions.IgnoreCase) + "...";
        }

        /// <summary>
        /// 去除HTML标记
        /// </summary>
        /// <param name="noHtml">包括HTML的源码 </param>
        /// <returns>已经去除后的文字</returns>
        public static string NoHtml(string noHtml)
        {
            string htmlstring = noHtml;
            //删除脚本
            htmlstring = Regex.Replace(htmlstring, @"<script[^>]*?>.*?</script>", "", RegexOptions.IgnoreCase);
            //删除HTML
            htmlstring = Regex.Replace(htmlstring, @"<(.[^>]*)>", "", RegexOptions.IgnoreCase);
            //htmlstring = Regex.Replace(htmlstring, @"([\r\n])[\s]+", "", RegexOptions.IgnoreCase);
            htmlstring = Regex.Replace(htmlstring, @"-->", "", RegexOptions.IgnoreCase);
            htmlstring = Regex.Replace(htmlstring, @"<!--.*", "", RegexOptions.IgnoreCase);
            htmlstring = Regex.Replace(htmlstring, @"&(quot|#34);", "\"", RegexOptions.IgnoreCase);
            htmlstring = Regex.Replace(htmlstring, @"&(amp|#38);", "&", RegexOptions.IgnoreCase);
            htmlstring = Regex.Replace(htmlstring, @"&(lt|#60);", "<", RegexOptions.IgnoreCase);
            htmlstring = Regex.Replace(htmlstring, @"&(gt|#62);", ">", RegexOptions.IgnoreCase);
            htmlstring = Regex.Replace(htmlstring, @"&(nbsp|#160);", " ", RegexOptions.IgnoreCase);
            htmlstring = Regex.Replace(htmlstring, @"&(iexcl|#161);", "\xa1", RegexOptions.IgnoreCase);
            htmlstring = Regex.Replace(htmlstring, @"&(cent|#162);", "\xa2", RegexOptions.IgnoreCase);
            htmlstring = Regex.Replace(htmlstring, @"&(pound|#163);", "\xa3", RegexOptions.IgnoreCase);
            htmlstring = Regex.Replace(htmlstring, @"&(copy|#169);", "\xa9", RegexOptions.IgnoreCase);
            htmlstring = Regex.Replace(htmlstring, @"&#(\d+);", "", RegexOptions.IgnoreCase);
            string s = htmlstring.Replace("<", "").Replace(">", "");
            htmlstring = WebUtility.HtmlEncode(s).Trim();
            return htmlstring;
        }

        /// <summary>
        /// 获取后缀名
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        public static string GetExtension(string filename)
        {
            return Path.GetExtension(filename);
        }

        public static string HtmlEncode(string text)
        {
            return WebUtility.HtmlEncode(text);
        }

        public static string HtmlDecode(string text)
        {
            var decodeHtml = WebUtility.HtmlDecode(text);
            return WebUtility.HtmlDecode(NoHtml(decodeHtml));
        }

        /// <summary>
        /// 生成资源访问URL
        /// </summary>
        /// <param name="url"></param>
        /// <param name="useToken">是否采用AccessToken获取数据</param>
        /// <returns></returns>
        public static string GenerateRequestUrl(string url, bool useToken = false)
        {
            Uri uri = new Uri(url);
            var appParam = string.Format("api_key={0}&{1}&{2}", AppConst.MoeAppKey, uri.Query.Replace("?", string.Empty), "r=" + new Random().Next().ToString());
            string absoluteUri = string.IsNullOrEmpty(uri.Query) ? uri.AbsoluteUri : uri.AbsoluteUri.Replace(uri.Query, string.Empty);
            var requestUrl = string.Format("{0}?{1}", absoluteUri, appParam);
            return requestUrl;
        }

        public static void ShowNotify(string message)
        {            
            var toastXml = ToastNotificationManager.GetTemplateContent(ToastTemplateType.ToastImageAndText01);
            toastXml.GetElementsByTagName("text")[0].AppendChild(toastXml.CreateTextNode(message)); //文字
            var imageElem = (XmlElement) toastXml.GetElementsByTagName("image")[0];
            imageElem?.SetAttribute("src", "ms-appx:///Images/myhomepage_fans_header.png");
            ToastNotificationManager.CreateToastNotifier().Show(new ToastNotification(toastXml));
        }

        public static void ShowTileNotify(string message, string image)
        {
            var showTile = Windows.Storage.ApplicationData.Current.LocalSettings.Values["_show_tile_"] as bool?;
            if (showTile == false)
            {
                TileUpdateManager.CreateTileUpdaterForApplication().Clear();
                return;
            }
            TileUpdateManager.CreateTileUpdaterForApplication().EnableNotificationQueue(true);
            var tileType = TileTemplateType.TileWide310x150ImageAndText01;
            var tileXml = TileUpdateManager.GetTemplateContent(tileType);
            ((XmlElement) tileXml.GetElementsByTagName("image")[0]).SetAttribute("src", image);
            tileXml.GetElementsByTagName("text")[0].InnerText = message;
            TileUpdateManager.CreateTileUpdaterForApplication().Update(new TileNotification(tileXml));

            var tileType2 = TileTemplateType.TileSquare150x150PeekImageAndText04;
            var tileXml2 = TileUpdateManager.GetTemplateContent(tileType2);
            ((XmlElement) tileXml2.GetElementsByTagName("image")[0]).SetAttribute("src", image);
            tileXml2.GetElementsByTagName("text")[0].InnerText = message;
            TileUpdateManager.CreateTileUpdaterForApplication().Update(new TileNotification(tileXml2));
        }
    }
}
