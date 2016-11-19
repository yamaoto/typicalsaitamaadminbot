using Newtonsoft.Json;

namespace TsabSharedLib
{
    public class PhotoUploadResult
    {
        [JsonProperty(PropertyName = "server")]
        public string Server { get; set; }
        [JsonProperty(PropertyName = "photos_list")]
        public string PhotosList { get; set; }
        [JsonProperty(PropertyName = "hash")]
        public string Hash { get; set; }
        [JsonProperty(PropertyName = "aid")]
        public string Aid { get; set; }
    }

    public class WallPhotoUploadResult
    {
        [JsonProperty(PropertyName = "server")]
        public string Server { get; set; }
        [JsonProperty(PropertyName = "photo")]
        public string Photo { get; set; }
        [JsonProperty(PropertyName = "hash")]
        public string Hash { get; set; }
    }
}