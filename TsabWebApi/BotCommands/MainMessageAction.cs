using System;
using TsabWebApi.Models;

namespace TsabWebApi.BotCommands
{
    [BotAction]
    internal class MainMessageAction : IBotAction
    {
        private BotActionContext _context;
        public void Start(BotActionContext context)
        {
            _context = context;
        }

        public string[] States { get; } = new [] { "NoState" };
        public string CommandName { get; } = null;
        public string Description { get; } = null;

        public ISendItem Command(string command, MessageModel message, out MessageFlow flow)
        {
            throw new InvalidOperationException();            
        }

        public ISendItem Message(string state, string text, MessageModel message, out MessageFlow flow)
        {
            flow = null;
            if (message.Sticker?.FileId != null)
            {
                return new SendStickerModel(message.Chat.Id, message.Sticker.FileId);
            }
            return new SendMessageModel(message.Chat.Id, ".-.");
        }
    }
}