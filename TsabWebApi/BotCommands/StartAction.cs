using System;
using TsabWebApi.Models;

namespace TsabWebApi.BotCommands
{
    [BotAction]
    internal class StartAction : IBotAction
    {
        private BotActionContext _context;
        public void Start(BotActionContext context)
        {
            _context = context;
        }

        public string[] States { get; } = new string[0];
        public string CommandName { get; } = "/start";
        public string Description { get; } = "начало работы с ботом";

        public ISendItem Command(string command, MessageModel message, out MessageFlow flow)
        {
            var msg = $@"Привет, {message.From.FirstName}!
Я @typical_saitama_adminBot. С моей помощью ты можешь проверять картинки на загрузку в сообщества, для этого просто введи /public или /help для получения всех подказок";
            var sticker = "BQADBAADtwMAAqKYZgABJFsIZLA51N0C";
            flow = new MessageFlow() { { new MessageFlowItem(message.Chat.Id, sticker, true, TimeSpan.FromSeconds(3)) } };
            return new SendMessageModel(message.Chat.Id, msg);
        }

        public ISendItem Message(string state, string text, MessageModel message, out MessageFlow flow)
        {
            throw new InvalidOperationException();
        }
    }
}