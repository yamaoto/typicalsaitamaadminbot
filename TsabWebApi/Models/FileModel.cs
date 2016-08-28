using System.Runtime.Serialization;

namespace TsabWebApi.Models
{
    [DataContract]
    public class FileModel
    {
        [DataMember(Name = "file_id")]
        public string FileId { get; set; }

        [DataMember(Name = "file_size")]
        public int FileSize { get; set; }

        [DataMember(Name = "file_path")]
        public string FilePath { get; set; }

    }
}