using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using TsabWebApi.Models;

namespace TsabWebApi.BotCommands
{
    [BotAction]
    internal class VkGroupAction : IBotAction
    {
        private BotActionContext _context;
        public void Start(BotActionContext context)
        {
            _context = context;
        }

        public string[] States { get; } = new[] { "vkgroup-auth", "vkgroup-choose-group" };
        public string CommandName { get; } = "/vkgroup";
        public string Description { get; } = "задать права доступа для сообщества Вк";

        public ISendItem Command(string command, MessageModel message, out MessageFlow flow)
        {
            var auths = _context.DbService.GetAuths(message.From.Id).Where(w => w.Auth).ToArray();
            var walls = _context.DbService.GetWalls().Where(w => auths.Any(a => a.WallId == w.Id)).ToArray();
            if (walls.Length == 1)
            {
                return _getAuth(walls.First().Id, message, out flow);
            } else if (walls.Length > 1)
            {
                _context.DbService.SetState(message.From.Id, message.Chat.Id, "vkgroup-choose-group");
                var keyboard = walls.Select(s => new KeyboardButtonModel(s.Name));
                flow = null;
                return new SendMessageModel(message.Chat.Id, "В какое сообщество?")
                {
                    ReplyMarkup = new ReplyKeyboardMarkupModel(keyboard)
                };
            }
            else
            {
                flow = new MessageFlow()
                {
                    new MessageFlowItem(message.Chat.Id, "Для этого введи /public", TimeSpan.FromMilliseconds(300))
                };
                return new SendMessageModel(message.Chat.Id, "Слушай, у тебя еще нет настроенного доступа для пабликов");
            }
        }

        public ISendItem Message(string state, string text, MessageModel message, out MessageFlow flow)
        {
            switch (state)
            {
                case "vkgroup-choose-group":
                    return _chooseGrpup(state, text, message, out flow);
                default:
                    flow = null;
                    return new SendMessageModel(message.Chat.Id,"o_O");
            }
        }

        public ISendItem _chooseGrpup(string state, string text, MessageModel message, out MessageFlow flow)
        {            
            var auths = _context.DbService.GetAuths(message.From.Id).Where(w => w.Auth).ToArray();
            var wall = _context.DbService.GetWalls().FirstOrDefault(w => auths.Any(a => a.WallId == w.Id) && w.Name==text);
            if (wall == null)
            {
                flow = null;
                return new SendMessageModel(message.Chat.Id, "Что-то не могу найти такого...");
            }
            else
            {
                return _getAuth(wall.Id, message, out flow);
            }
        }

        private ISendItem _getAuth(int wall,MessageModel message, out MessageFlow flow)
        {
            var msg = $"Так, сейчас тебе нужно авторизовать меня в сообществе во Вконтакте...";
            var id = Guid.NewGuid();
            _context.DbService.SetVkUser(id, message.From.Id,true,wall);
            var url = _context.CompareService.GetVkGroupAuth(id.ToString(), wall);
            flow = new MessageFlow()
            {
                { new MessageFlowItem(message.Chat.Id, "Для этого открой в браузере эту ссылку:", TimeSpan.FromMilliseconds(500)) },
                { new MessageFlowItem(message.Chat.Id, url, TimeSpan.FromMilliseconds(300)) }
            };
            return new SendMessageModel(message.Chat.Id, msg);
        }
    }
}