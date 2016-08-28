namespace TsabSharedLib
{
    public class WallItemModel
    {
        public WallItemModel()
        {

        }

        public WallItemModel(int wallId, long id, string url)
        {
            WallId = wallId;
            Id = id;
            Url = url;
        }
        public long Id { get; set; }
        public string Url { get; set; }
        public bool Loaded { get; set; }
        public int WallId { get; set; }
    }
}