using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation.Collections;
using Windows.Media.Playback;
using Windows.Networking.Connectivity;
using Windows.Storage;
using Windows.UI.Xaml;
using MoeFM.UWP.Entities;
using MoeFM.UWP.Entities.MoeFMEntity;
using MoeFM.UWP.OAuth;
using MoeFM.UWP.Entities.Response;
using Newtonsoft.Json;

namespace MoeFM.UWP.Common
{
    internal static class MoeOp
    {
        /// <summary>
        /// 后台播放器是否已经初始化完成
        /// </summary>
        public static bool BackgroundPlayerInited => (ApplicationData.Current.LocalSettings.Values["BackgroundPlayerState"] as Boolean?).GetValueOrDefault();

        /// <summary>
        /// 收藏
        /// </summary>
        /// <param name="id"></param>
        /// <param name="favType"></param>
        /// <returns></returns>
        public static async Task<bool> Like(string id, FavType favType)
        {
            try
            {
                if (!OAuthHelper.IsLogin)
                {
                    ShowMessage("登录后才能使用收藏功能哦~~~");
                    return false;
                }
                var urlTmp = $"http://api.moefou.org/fav/add.json?fav_obj_id={id}&fav_obj_type={favType.ToString().ToLower()}&fav_type=1";
                var uri = OAuthHelper.GenerateRequestUrl(urlTmp, true);
                HttpClient client = new HttpClient();
                var result = await client.GetStringAsync(new Uri(uri));
                ShowMessage("收藏成功");
                //清空缓存
                MoeAppCache.RemoveLeft($"MYFAV{favType.ToString().ToUpper()}DATA");
                return true;
            }
            catch (HttpRequestException)
            {
                ShowMessage("收藏失败，请重试");
                return false;
            }
        }

        /// <summary>
        /// 取消收藏
        /// </summary>
        /// <param name="id"></param>
        /// <param name="favType"></param>
        /// <returns></returns>
        public static async Task<bool> UnLike(string id, FavType favType)
        {
            try
            {
                var urlTmp = $"http://api.moefou.org/fav/delete.json?fav_obj_id={id}&fav_obj_type={favType.ToString().ToLower()}";
                var uri = OAuthHelper.GenerateRequestUrl(urlTmp, true);
                HttpClient client = new HttpClient();
                await client.GetStringAsync(new Uri(uri));
                ShowMessage("取消成功");
                //清空缓存
                MoeAppCache.RemoveLeft($"MYFAV{favType.ToString().ToUpper()}DATA");
                return true;
            }
            catch (HttpRequestException)
            {
                ShowMessage("取消失败，请重试");
                return false;
            }
        }

        /// <summary>
        /// 播放专辑
        /// </summary>
        /// <param name="wikiId"></param>
        /// <param name="wikiType"></param>
        public static void PlayWiki(string wikiId, WikiType wikiType)
        {
            ValueSet set = new ValueSet {{EntityHelper.MediaKey, JsonHelper.Stringify(new MoeMessage {Command = MessageType.PlayWiki, DataId = wikiId, WikiType = wikiType.ToString().ToLower()})}};
            SendMessageToBackground(set);
        }

        /// <summary>
        /// 播放专辑中单曲
        /// </summary>
        /// <param name="subId"></param>
        public static void PlayWikiItem(string subId)
        {
            ValueSet set = new ValueSet {{EntityHelper.MediaKey, JsonHelper.Stringify(new MoeMessage {Command = MessageType.PlayWikiItem, DataId = subId})}};
            SendMessageToBackground(set);
        }

        /// <summary>
        /// 播放收藏的曲目
        /// </summary>
        public static void PlayFavSub()
        {
            ValueSet set = new ValueSet {{EntityHelper.MediaKey, JsonHelper.Stringify(new MoeMessage {Command = MessageType.PlayFavSub})}};
            SendMessageToBackground(set);
        }

        /// <summary>
        /// 播放器发送后台方法
        /// </summary>
        /// <param name="set"></param>
        public static void SendMessageToBackground(ValueSet set)
        {
            var initState = ApplicationData.Current.LocalSettings.Values["BackgroundPlayerState"] as Boolean?;
            if (initState == false || set == null) return;
            BackgroundMediaPlayer.SendMessageToBackground(set);
        }

        /// <summary>
        /// 获取数据
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="url"></param>
        /// <param name="useToken"></param>
        /// <returns></returns>
        public static async Task<T> GetMoeResponseEntity<T>(string url, bool useToken = true)
        {
            try
            {
                var status = MoeOp.GetNetworkStatus();
                if (status == -1)
                {
                    ShowMessage("网络貌似断开了，请重新检查〒_〒");
                    return default(T);
                }
                var uri = new Uri(OAuthHelper.GenerateRequestUrl(url, useToken));
                WebRequest request = WebRequest.Create(uri);
                var response = await request.GetResponseAsync();
                string json;
                using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                {
                    json = await reader.ReadToEndAsync();
                }
                return JsonHelper.Parse<T>(json);
            }
            catch (WebException ex)
            {
                var response = ex.Response as HttpWebResponse;
                if (response?.StatusCode == HttpStatusCode.Unauthorized) //如果是未授权错误，退出登录，重新获取
                {
                    await OAuthHelper.Logout();
                    return await GetMoeResponseEntity<T>(url, false);
                }
                return default(T);
            }
            catch (JsonException)
            {
                return default(T);
            }
            catch (Exception)
            {
                return default(T);
            }
        }

        public static async Task WriteLog(string info)
        {
            try
            {
                IStorageItem file = await ApplicationData.Current.LocalCacheFolder.TryGetItemAsync("INFO.log") ?? await ApplicationData.Current.LocalCacheFolder.CreateFileAsync("ERROR.log", CreationCollisionOption.OpenIfExists);
                StringBuilder builder = new StringBuilder();
                builder.AppendLine($"----------------------START LOG Date:{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")}----------------------");
                builder.AppendLine(info);
                builder.AppendLine("-----------------------END LOG----------------------");
                await FileIO.AppendTextAsync((StorageFile) file, builder.ToString());
            }
            catch
            {

            }
        }

        public static async Task<MoeUser> GetUserInfo()
        {
            try
            {
                var url = OAuthHelper.GenerateRequestUrl("http://api.moefou.org/user/detail.json", true);
                HttpClient client = new HttpClient();
                var json = await client.GetStringAsync(new Uri(url));
                var response = JsonHelper.Parse<MoeResponse<MoeUserEntity>>(json);
                return response?.response?.user;
            }
            catch (HttpRequestException)
            {
                return null;
            }
            catch
            {
                return null;
            }
        }

        public static void ShowMessage(string message)
        {
            if (string.IsNullOrEmpty(message)) return;
            (Window.Current.Content as MainPage)?.ShowMessage(message);
        }

        /// <summary>
        /// 获取当前网络环境
        /// </summary>
        /// <returns></returns>
        public static int GetNetworkStatus()
        {
            ConnectionProfile profile = NetworkInformation.GetInternetConnectionProfile();
            if (profile == null || (profile.IsWwanConnectionProfile == false && profile.IsWlanConnectionProfile == false)) //网络中断
            {
                return -1;
            }
            if (profile.IsWwanConnectionProfile) //移动网络
            {
                return 2;
            }
            if (profile.IsWlanConnectionProfile) //wifi
            {
                return 1;
            }
            return 0; //未知网络
        }
    }
}
