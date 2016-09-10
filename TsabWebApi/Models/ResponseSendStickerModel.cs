using System.Runtime.Serialization;

namespace TsabWebApi.Models
{
    [DataContract]
    public class ResponseSendStickerModel
    {
        public ResponseSendStickerModel(SendStickerModel item)
        {
            ChatId = item.ChatId;
            Sticker = item.Sticker;
            ReplyToMessageId = item.ReplyToMessageId;
            ReplyMarkup = item.ReplyMarkup;
            DisableNotification = item.DisableNotification;
        }

        public ResponseSendStickerModel()
        {
            
        }
        [DataMember(Name = "chat_id")]
        public int ChatId { get; set; }
        [DataMember(Name = "sticker")]
        public string Sticker { get; set; }
        [DataMember(Name = "reply_to_message_id")]
        public int? ReplyToMessageId { get; set; }
        [DataMember(Name = "reply_markup")]
        public object ReplyMarkup { get; set; }
        [DataMember(Name = "disable_notification")]
        public bool DisableNotification { get; set; }

        [DataMember(Name = "method")]
        public string Method { get; set; } = "sendSticker";
    }
}