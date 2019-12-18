using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Storage;
using MoeFM.UWP.Common;

namespace MoeFM.UWP.OAuth
{
    public static class OAuthHelper
    {
        public static bool IsLogin => !string.IsNullOrEmpty(Convert.ToString(ApplicationData.Current.LocalSettings.Values[AppConst.AccessToken]));

        /// <summary>
        ///     生成资源访问URL
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public static string GenerateRequestUrl(string url)
        {
            var accessToken = Convert.ToString(ApplicationData.Current.LocalSettings.Values[AppConst.AccessToken]);
            string appParam;
            var uri = new Uri(url);
            var oAuth = new OAuthBase();
            if (!string.IsNullOrEmpty(accessToken))
            {
                var timeStamp = oAuth.GenerateTimeStamp();
                var nonce = oAuth.GenerateNonce();
                string nUrl, pa;
                var accessTokenSecret = ApplicationData.Current.LocalSettings.Values[AppConst.AccessTokenSecret].ToString();
                var signature = oAuth.GenerateSignature(uri, AppConst.MoeAppKey, AppConst.ConsumerSecret, accessToken, accessTokenSecret, "GET", timeStamp, nonce, string.Empty
                    , out nUrl, out pa);

                appParam = string.Format("{0}&oauth_signature={1}", pa, signature);
            }
            else
            {
                appParam = string.Format("api_key={0}&{1}&{2}", AppConst.MoeAppKey, uri.Query.Replace("?", string.Empty), "r=" + oAuth.GenerateTimeStamp());
            }
            var absoluteUri = string.IsNullOrEmpty(uri.Query) ? uri.AbsoluteUri : uri.AbsoluteUri.Replace(uri.Query, string.Empty);
            var requestUrl = string.Format("{0}?{1}", absoluteUri, appParam);
            return requestUrl;
        }

        /// <summary>
        ///     生成资源访问URL
        /// </summary>
        /// <param name="url"></param>
        /// <param name="useToken">是否采用AccessToken获取数据</param>
        /// <returns></returns>
        public static string GenerateRequestUrl(string url, bool useToken)
        {
            if (useToken)
            {
                return GenerateRequestUrl(url);
            }
            var uri = new Uri(url);
            var oAuth = new OAuthBase();
            var appParam = string.Format("api_key={0}&{1}&{2}", AppConst.MoeAppKey, uri.Query.Replace("?", string.Empty), "r=" + oAuth.GenerateTimeStamp());
            var absoluteUri = string.IsNullOrEmpty(uri.Query) ? uri.AbsoluteUri : uri.AbsoluteUri.Replace(uri.Query, string.Empty);
            var requestUrl = string.Format("{0}?{1}", absoluteUri, appParam);
            return requestUrl;
        }

        public static async Task GetTokenAsyncFromFileAsync()
        {
            var item = await ApplicationData.Current.LocalFolder.TryGetItemAsync(AppConst.TokenFileName);
            if (item == null) return;
            var json = await FileIO.ReadTextAsync((IStorageFile) item);
            if (string.IsNullOrEmpty(json)) return;
            var dictionary = JsonHelper.Parse<Dictionary<string, string>>(json);
            ApplicationData.Current.LocalSettings.Values[AppConst.AccessToken] = dictionary[AppConst.AccessToken];
            ApplicationData.Current.LocalSettings.Values[AppConst.AccessTokenSecret] = dictionary[AppConst.AccessTokenSecret];
        }

        public static async Task Logout()
        {
            ApplicationData.Current.LocalSettings.Values.Remove(AppConst.AccessToken);
            ApplicationData.Current.LocalSettings.Values.Remove(AppConst.AccessTokenSecret);
            //清除物理文件
            var item = await ApplicationData.Current.LocalFolder.TryGetItemAsync(AppConst.TokenFileName);
            if (item != null)
            {
                await item.DeleteAsync();
            }
        }
    }
}