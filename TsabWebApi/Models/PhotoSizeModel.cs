using System.Runtime.Serialization;

namespace TsabWebApi.Models
{
    [DataContract]
    public class PhotoSizeModel
    {
        [DataMember(Name = "file_id")]
        public string FileId { get; set; }
        [DataMember(Name = "height")]
        public int Height { get; set; }
        [DataMember(Name = "width")]
        public int Weight { get; set; }
        [DataMember(Name="file_size")]
        public int FileSize { get; set; }
    }
}