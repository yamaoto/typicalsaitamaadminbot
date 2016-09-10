using System;
using System.Linq;
using TsabWebApi.Models;

namespace TsabWebApi.BotCommands
{
    [BotAction]
    internal class HelpAction : IBotAction
    {
        private BotActionContext _context;
        public void Start(BotActionContext context)
        {
            _context = context;
        }

        public string[] States { get; } = new string[0];
        public string CommandName { get; } = "/help";
        public string Description { get; } = "показать команды";

        public ISendItem Command(string command, MessageModel message, out MessageFlow flow)
        {
            var msg = $"Итак, {message.From.FirstName}!\r\nВот команды на которые меня обучил @yamaoto:\r\n";
            var actions = BotService.GetActions().Where(w=>w.CommandName!=null).OrderBy(o=>o.CommandName);
            msg = actions.Aggregate(msg, (current, action) => current + $"{action.CommandName} - {action.Description}\r\n");
            flow = null;
            return new SendMessageModel(message.Chat.Id, msg);
        }

        public ISendItem Message(string state,string text, MessageModel message, out MessageFlow flow)
        {
            throw new InvalidOperationException();
        }
    }
}