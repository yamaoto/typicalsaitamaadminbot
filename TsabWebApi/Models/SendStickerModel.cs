using System.Runtime.Serialization;

namespace TsabWebApi.Models
{
    [DataContract]
    public class SendStickerModel : ISendItem
    {
        public SendStickerModel()
        {

        }

        public SendStickerModel(int chatId,string sticker)
        {
            ChatId = chatId;
            Sticker = sticker;
        }
        [DataMember(Name="chat_id")]
        public int ChatId { get; set; }
        [DataMember(Name = "sticker")]
        public string Sticker { get; set; }
        [DataMember(Name="reply_to_message_id")]
        public int? ReplyToMessageId { get; set; }
        [DataMember(Name="reply_markup")]
        public object ReplyMarkup { get; set; }
        [DataMember(Name="disable_notification")]
        public bool DisableNotification { get; set; }
    }
}