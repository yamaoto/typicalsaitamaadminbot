using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace TsabSharedLib.ImageSearch
{
    [SearchEngine]
    public class TumblrSearchEngine: ISearchEngine
    {
        private readonly WebClient _client;

        public TumblrSearchEngine()
        {
            _client = new WebClient();
        }

        public string EngineName { get; } = "Tumblr";
        public IEnumerable<ISearchResultItem> Search(string tag, int total, DateTime? after)
        {
            var resultItems = new List<TumblrSearchResultItem>();
            var count = 0;
            int? timestamp = null;
            var tryCount = 0;
            const int maxTryCount = 5;
            while (count < total)
            {
                TumblrSearchResultItem[] items;
                try
                {
                    items = _search(tag, after, timestamp);
                }
                catch 
                {
                    if (tryCount < maxTryCount)
                    {
                        tryCount++;                        
                        continue;
                    }
                    else
                    {
                        break;
                    }
                }
                timestamp = items.Last()?.Timestamp;
                count += items.Length;
                resultItems.AddRange(items);
            }
            return resultItems;
        }

        private TumblrSearchResultItem[] _search(string tag, DateTime? after, int? timestamp)
        {
            var url = $"http://api.tumblr.com/v2/tagged?tag={tag}&api_key={ConfigStorage.TumblrKey}";
            if (timestamp.HasValue)
                url += "&before=" + timestamp.Value;
            var resultData = _client.DownloadString(url);
            dynamic json = JObject.Parse(resultData);
            if (json?.meta?.msg != "OK")
            {
                return null;
            }
            var result = new List<TumblrSearchResultItem>();
            foreach (var item in json.response)
            {
                if (item.type != "photo")
                    continue;
                var creationDate = DateTime.Parse((string) item.date);
                if(after.HasValue && creationDate<after)
                    continue;
                var photos = (JArray) item.photos;
                var group = photos.Count > 1 ? Guid.NewGuid().ToString("N") : null;
                var tags = new List<string>();
                foreach (string photoTag in item.tags)
                {
                    tags.Add(photoTag);
                }
                foreach (var photo in item.photos)
                {
                    
                    var resultItem = new TumblrSearchResultItem()
                    {
                        ItemUrl = item.post_url,
                        ImageUrl = photo.original_size.url,
                        Description = item.summary,
                        Tags = tags.ToArray(),
                        Group = group,
                        Timestamp = item.timestamp,
                        Score = item.note_count
                    };
                    result.Add(resultItem);
                }
            }
            return result.ToArray();
        }
    }

    public class TumblrSearchResultItem : ISearchResultItem
    {
        public string ItemUrl { get; set; }
        public string ImageUrl { get; set; }
        public string Description { get; set; }
        public string[] Tags { get; set; }
        public string Engine { get; set; } ="Tumblr";
        public string Group { get; set; }
        public int Score { get; set; }
        public int Timestamp { get; set; }
    }
}
