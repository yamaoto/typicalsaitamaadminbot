using System;

namespace TsabSharedLib
{
    public class WallModel
    {
        public int Id { get; set; }
        public long? LastItemId { get; set; }
        public string Url { get; set; }
        public string Name { get; set; }
        public DateTime? LastUpdate { get; set; }
        public int? UploadAlbum { get; set; }
    }
}