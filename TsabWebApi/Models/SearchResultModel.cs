using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;
using kasthack.vksharp;
using TsabSharedLib;

namespace TsabWebApi.Models
{
    [DataContract]
    public class SearchResultModel
    {
        [DataMember(Name = "items")]
        public IEnumerable<SearchResultItemModel> Items { get; set; }
        [DataMember(Name="tag")]
        public string Tag { get; set; }
        [IgnoreDataMember]
        public int UserId { get; set; }
        [IgnoreDataMember]
        public int Position { get; set; }
    }

    [DataContract]
    public class SearchResultItemModel : ISearchResultItem
    {
        public SearchResultItemModel()
        {
            
        }
        public SearchResultItemModel(ISearchResultItem item)
        {
            ItemUrl = item.ItemUrl;
            ImageUrl = item.ImageUrl;
            Description = item.Description;
            Tags = item.Tags;
            Engine = item.Engine;
            Group = item.Group;
            Score = item.Score;

            ThumbUrl = "http://typical-saitama-admin-bot.azurewebsites.net/thumb?src=" + Uri.EscapeUriString(ImageUrl);
        }

        [DataMember(Name = "thumb")]
        public string ThumbUrl { get; set; }
        [DataMember(Name="itemUrl")]
        public string ItemUrl { get; set; }
        [DataMember(Name="imageTag")]
        public string ImageUrl { get; set; }
        [DataMember(Name="description")]
        public string Description { get; set; }
        [DataMember(Name = "tags")]
        public string[] Tags { get; set; }
        [DataMember(Name="engine")]
        public string Engine { get; set; }
        [DataMember(Name="tag")]
        public string Group { get; set; }
        [DataMember(Name="score")]
        public int Score { get; set; }
    }
}