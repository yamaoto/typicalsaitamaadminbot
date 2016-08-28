using System;

namespace TsabWebApi.Models
{
    internal class MessageFlowItem
    {
        public MessageFlowItem (ISendItem message, TimeSpan? span=null)
        {
            Message = message;
            Span = span;
        }

        public MessageFlowItem(int chatId, string text, TimeSpan? span = null) : this(new SendMessageModel(chatId, text), span)
        {

        }

        public MessageFlowItem(int chatId, string sticker,bool isSticker, TimeSpan? span = null) : this(new SendStickerModel(chatId,sticker), span)
        {

        }
        public ISendItem Message { get; set; }
        public TimeSpan? Span { get; set; }
    }
}