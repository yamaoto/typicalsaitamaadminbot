using System;
using TsabSharedLib;
using TsabWebApi.Models;

namespace TsabWebApi.BotCommands
{
    [BotAction]
    internal class CanselAction : IBotAction
    {
        private BotActionContext _context;
        public void Start(BotActionContext context)
        {
            _context = context;
        }

        public string[] States { get; } = new string[0];
        public string CommandName { get; } = "/cansel";
        public string Description { get; } = "отмена";
        private readonly DbService _dbService = BotService.GetDbService();

        public ISendItem Command(string command, MessageModel message, out MessageFlow flow)
        {
            _dbService.SetState(message.From.Id,message.Chat.Id, "NoState");
            var msg = $@"Ладно";
            var state = _context.DbService.GetState(message.From.Id);
            if (state.State == "choose-tag")
                flow = null;
            else
                flow = new MessageFlow() { new MessageFlowItem(message.Chat.Id, "BQADBAADMAMAAqKYZgABj5c3MVEUXD4C", true, TimeSpan.FromSeconds(2)) };
            return new SendMessageModel(message.Chat.Id, msg) {ReplyMarkup = new ReplyKeyboardHideModel() {HideKeyboard = true} };
        }

        public ISendItem Message(string state,string text, MessageModel message, out MessageFlow flow)
        {
            throw new InvalidOperationException();
        }
    }
}