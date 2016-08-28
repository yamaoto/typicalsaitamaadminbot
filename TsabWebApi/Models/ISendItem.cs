namespace TsabWebApi.Models
{
    public interface ISendItem
    {
        int ChatId { get; set; }
        bool DisableNotification { get; set; }
    }
}