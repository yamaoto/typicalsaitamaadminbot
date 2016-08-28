using System.Runtime.Serialization;

namespace TsabWebApi.Models
{
    [DataContract]
    public class StickerModel
    {
        [DataMember(Name = "file_id")]
        public string FileId { get; set; }
        [DataMember(Name = "width")]
        public int Width { get; set; }
        [DataMember(Name = "height")]
        public int Height { get; set; }
        [DataMember(Name = "thumb")]
        public PhotoSizeModel Thumb { get; set; }
        [DataMember(Name = "emoji")]
        public string Emoji { get; set; }
        [DataMember(Name="file_size")]
        public int FileSize { get; set; }
    }
}