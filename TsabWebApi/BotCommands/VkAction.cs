using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using TsabWebApi.Models;

namespace TsabWebApi.BotCommands
{
    [BotAction]
    internal class VkAction : IBotAction
    {
        private BotActionContext _context;
        public void Start(BotActionContext context)
        {
            _context = context;
        }

        public string[] States { get; } = new[] {"vk-auth"};
        public string CommandName { get; } = "/vk";
        public string Description { get; } = "задать права доступа для Вк";

        public ISendItem Command(string command, MessageModel message, out MessageFlow flow)
        {
            var msg = $"Так, сейчас тебе нужно авторизовать меня во Вконтакте...";
            var id = Guid.NewGuid();
            _context.DbService.SetVkUser(id,message.From.Id,false,null);
            var url = _context.CompareService.GetVkAuth(id.ToString());
            flow = new MessageFlow()
            {
                { new MessageFlowItem(message.Chat.Id, "Для этого открой в браузере эту ссылку:", TimeSpan.FromMilliseconds(500)) },
                { new MessageFlowItem(message.Chat.Id, url, TimeSpan.FromMilliseconds(300)) }
            };
            return new SendMessageModel(message.Chat.Id, msg);
        }

        public ISendItem Message(string sate, string text, MessageModel message, out MessageFlow flow)
        {
            throw new NotImplementedException();
        }
    }
}