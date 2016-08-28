using System.Runtime.Serialization;

namespace TsabSharedLib
{
    [DataContract]
    public class WallUpdateModel
    {
        public WallUpdateModel()
        {

        }

        public WallUpdateModel(int wallId)
        {
            WallId = wallId;
        }
        [DataMember(Name = "wall_id")]
        public int WallId { get; set; }
    }
}