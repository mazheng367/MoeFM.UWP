using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.Data.Json;
using MoeFM.UWP.Entities;
using Newtonsoft.Json;

namespace MoeFM.UWP.Common
{
    /// <summary>
    /// 萌否页面分析器
    /// </summary>
    public sealed class MoePage
    {
        public List<MoeWiki> GetNewMusic(string html)
        {
            if (string.IsNullOrEmpty(html))
            {
                return new List<MoeWiki>(0);
            }
            var regex = new Regex(@"<div\s+class=""show-box mb-2 cleared"">(?>(<div[^>]*>(?<o>)|</div>(?<-o>)|(?:(?!</?div\b)[\s\S])))*(?(o)(?!))</div>");
            var matches = regex.Matches(html);
            if (matches == null || matches.Count == 0)
            {
                return new List<MoeWiki>(0);
            }
            var list = new List<MoeWiki>();
            var regDiv = new Regex(@"<div\s+class=""item item-\d+"">(?>(<div[^>]*>(?<o>)|</div>(?<-o>)|(?:(?!</?div\b)[\s\S])))*(?(o)(?!))</div>");
            foreach (Match m in matches)
            {
                var contents = regDiv.Matches(m.Groups[0].Value).Cast<Match>().Select(s => s.Groups[0].Value);
                foreach (var content in contents)
                {
                    MoeWiki model = new MoeWiki();
                    //提取url
                    var url = Regex.Match(content, @"href\s*=\s*""(?<Url>[^""]*)""").Groups["Url"].Value;
                    model.wiki_url = url;
                    model.wiki_id = Convert.ToInt32(Regex.Match(url, @"(?>[^/])+$").Groups[0].Value);
                    model.wiki_title = Regex.Match(content, @"<a\s+class=""item-title""\s+[^>]*>(?<wiki_name>.*(?=</a>))</a>").Groups["wiki_name"].Value;
                    model.wiki_cover = new MoeCover() {square = Regex.Match(content, @"src\s*=\s*""(?<src>[^""]*)""").Groups["src"].Value};
                    list.Add(model);
                }
            }
            return list;
        }
    }
}
