using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web;
using TsabWebApi.Controllers;
using TsabWebApi.Models;

namespace TsabWebApi.BotCommands
{
    [BotAction]
    internal class NextAction : IBotAction
    {
        private BotActionContext _context;
        public void Start(BotActionContext context)
        {
            _context = context;
        }

        public string[] States { get; } = new[] { "next-show", "next-choose-wall" };
        public string CommandName { get; } = "/next";
        public string Description { get; } = "показать следующую картинку";
        public ISendItem Command(string command, MessageModel message, out MessageFlow flow)
        {
            if (BotController.UserSearches.ContainsKey(message.From.Id))
            {
                var tag = BotController.UserSearches[message.From.Id];
                if (!BotController.SearchResult.ContainsKey(tag))
                {
                    _context.DbService.SetState(message.From.Id, message.Chat.Id, "NoState");
                    flow = null;
                    return new SendMessageModel(message.Chat.Id,
                        "А ты уверен что производил поиск? Если что, напиши /search для этого...");
                }
                var result = BotController.SearchResult[tag];
                var items = result.Items.ToArray();
                if (result.Position + 1 >= items.Length)
                {
                    _context.DbService.SetState(message.From.Id, message.Chat.Id, "NoState");
                    flow = null;
                    return new SendMessageModel(message.Chat.Id, "Это все! Действительно все. Больше нету картинок");
                }
                _context.DbService.SetState(message.From.Id, message.Chat.Id, "next-show");
                result.Position++;
                var item = items[result.Position];
                flow = new MessageFlow()
                {
                    new MessageFlowItem(message.From.Id,$"Найдено в '{item.Engine}', рейтинг {item.Score}",TimeSpan.FromMilliseconds(300))
                };
                var imageData = new WebClient().DownloadData(item.ImageUrl);
                byte[] jpegImageData = null;
                using (var stream = new MemoryStream(imageData))
                {
                    var image = Image.FromStream(stream);
                    using (var outStream = new MemoryStream())
                    {
                        image.Save(outStream, ImageFormat.Jpeg);
                        jpegImageData = outStream.GetBuffer();
                    }
                }
                return new SendPhotoModel(message.From.Id, jpegImageData);
            }
            else
            {
                _context.DbService.SetState(message.From.Id, message.Chat.Id, "NoState");
                flow = null;
                return new SendMessageModel(message.Chat.Id,
                    "А ты уверен что производил поиск? Если что, напиши /search для этого...");
            }
        }

        public ISendItem Message(string state, string text, MessageModel message, out MessageFlow flow)
        {
            switch (state)
            {
                case "next-show":
                    return _show(text, message, out flow);
                case "next-choose-wall":
                    return _chooseWall(text, message, out flow);
                default:
                    throw new ArgumentException(nameof(state));
            }
        }

        private ISendItem _chooseWall(string text, MessageModel message, out MessageFlow flow)
        {
            flow = new MessageFlow();
            var auths = _context.DbService.GetAuths(message.From.Id).Where(w=>w.Auth);
            var walls = _context.DbService.GetWalls().Where(w => auths.Any(a => a.WallId == w.Id));
            var wall = walls.FirstOrDefault(f => f.Name.Equals(text,StringComparison.CurrentCultureIgnoreCase));
            if (wall != null)
            {
                var tag = BotController.UserSearches[message.From.Id];
                var result = BotController.SearchResult[tag];
                var items = result.Items.ToArray();
                var item = items[result.Position];
                throw new NotImplementedException();
                _context.CompareService.Publish(item,wall.Id);
                flow = null;
                return null;
            }
            else
            {
                flow = null;
                return new SendMessageModel(message.Chat.Id, "Ээ, что-то я не могу найти такое сообщество.\r\nУточни-ка...");
            }
        }

        private ISendItem _show(string text, MessageModel message, out MessageFlow flow)
        {
            var show = new[] {"покажи","инфо","сведения","инфа", "показать инфу", "показать инфо","показать" };
            var post = new[] {"публикуй","действуй","в отложку","вк","в вк","паблик","в паблик", "опубликовать фото","опубликовать" };
            var next = new[] {"дальше","далее","еще","следующий","следующая", "показать следующее" };
            var tag = BotController.UserSearches[message.From.Id];
            var result = BotController.SearchResult[tag];
            var items = result.Items.ToArray();
            var item = items[result.Position];
            if (show.Any(a => a == text))
            {                
                flow = new MessageFlow()
                {
                    new MessageFlowItem(message.Chat.Id,item.ItemUrl)
                };
                return new SendMessageModel(message.Chat.Id,item.Description);
            } else if (post.Any(a => a == text))
            {
                _context.DbService.SetState(message.From.Id,message.Chat.Id, "next-choose-wall");
                flow = new MessageFlow();
                var auths = _context.DbService.GetAuths(message.From.Id).Where(w => w.Auth);
                var walls = _context.DbService.GetWalls().Where(w => auths.Any(a => a.WallId == w.Id));
                var keyboard = walls.Select(s => new KeyboardButtonModel(s.Name));
                return new SendMessageModel(message.Chat.Id, "В какое сообщество?")
                {
                    ReplyMarkup = new ReplyKeyboardMarkupModel(keyboard)
                };
            } else if (next.Any(a => a == text))
            {
                return Command(text,message,out flow);
            }
            flow = new MessageFlow()
            {
                new MessageFlowItem(message.Chat.Id,"Если ты скажешь что-то понятнее, я могу показать инфу, опубликовать фото или показать следующее..",TimeSpan.FromMilliseconds(300)),
            };
            return new SendMessageModel(message.Chat.Id,"Не понел что ты сказал...");
        }
    }
}