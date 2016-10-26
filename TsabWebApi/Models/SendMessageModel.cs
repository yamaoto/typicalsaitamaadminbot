using System.Runtime.Serialization;

namespace TsabWebApi.Models
{
    [DataContract]
    public class SendMessageModel : ISendItem
    {
        public SendMessageModel(int chatId,string text, object replyMarkup=null)
        {
            ChatId = chatId;
            Text = text;
            ReplyMarkup = replyMarkup;
        }
        [DataMember(Name="chat_id")]
        public int ChatId { get; set; }
        [DataMember(Name = "text")]
        public string Text { get; set; }
        [DataMember(Name="parse_mode")]
        public string ParseMode { get; set; }
        [DataMember(Name="disable_web_page_preview")]
        public bool DisableWebPagePreview { get; set; }
        [DataMember(Name="disable_notification")]
        public bool DisableNotification { get; set; }
        [DataMember(Name="reply_to_message_id")]
        public int? ReplyToMessageId { get; set; }
        /// <summary>
        /// <see cref="InlineKeyboardButtonModel"/>, <see cref="ReplyKeyboardMarkupModel"/>, <see cref="ReplyKeyboardHideModel"/>, <see cref="ForceReplyModel"/>
        /// </summary>
        [DataMember(Name="reply_markup")]
        public object ReplyMarkup { get; set; }
    }

    [DataContract]
    public class SendPhotoModel : ISendItem
    {
        public SendPhotoModel(int chatId, byte[] bytes)
        {
            ChatId = chatId;
            Photo = bytes;
        }
        [DataMember(Name = "chat_id")]
        public int ChatId { get; set; }
        [DataMember(Name = "photo")]
        public byte[] Photo { get; set; }
        [DataMember(Name = "caption")]
        public string Caption { get; set; }
        [DataMember(Name = "disable_notification")]
        public bool DisableNotification { get; set; }
        [DataMember(Name = "reply_to_message_id")]
        public int? ReplyToMessageId { get; set; }
        /// <summary>
        /// <see cref="InlineKeyboardButtonModel"/>, <see cref="ReplyKeyboardMarkupModel"/>, <see cref="ReplyKeyboardHideModel"/>, <see cref="ForceReplyModel"/>
        /// </summary>
        [DataMember(Name = "reply_markup")]
        public object ReplyMarkup { get; set; }
    }
}