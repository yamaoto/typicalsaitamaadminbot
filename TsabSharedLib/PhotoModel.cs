namespace TsabSharedLib
{
    public class PhotoModel
    {
        public PhotoModel()
        {

        }

        public PhotoModel(int wallId, long wallItemId, string url)
        {
            WallId = wallId;
            WallItemId = wallItemId;
            Url = url;
        }
        public long WallItemId { get; set; }
        public string Url { get; set; }
        public string Blob { get; set; }
        public bool Loaded { get; set; }
        public int WallId { get; set; }
    }
}