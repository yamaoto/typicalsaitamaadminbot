using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web;
using TsabSharedLib;
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
                var commands = new[] { "/next","Публикуй", "/cansel" };
                var reply = new ReplyKeyboardMarkupModel()
                {
                    Keyboard = commands.Select(s => new[] {new KeyboardButtonModel() {Text = s}}).ToArray()
                };
                flow = new MessageFlow()
                {
                    new MessageFlowItem(
                        new SendMessageModel(message.From.Id,$"Найдено в '{item.Engine}', рейтинг {item.Score}",TimeSpan.FromMilliseconds(300))
                        {
                            ReplyMarkup = reply
                        }
                        )
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
            var tag = BotController.UserSearches[message.From.Id];
            var result = BotController.SearchResult[tag];
            var items = result.Items.ToArray();
            var item = items[result.Position];
            flow = new MessageFlow();
            var auths = _context.DbService.GetAuths(message.From.Id).Where(w => w.Auth);
            var walls = _context.DbService.GetWalls().Where(w => auths.Any(a => a.WallId == w.Id));
            var wall = walls.FirstOrDefault(f => f.Name.Equals(text, StringComparison.CurrentCultureIgnoreCase));
            if (wall != null)
            {
                if (!wall.UploadAlbum.HasValue)
                {
                    return new SendMessageModel(message.Chat.Id, "Администратор паблика не указал для него альбом для загруки, увы пока не могу загрузить фото...");
                }
                _context.DbService.SetState(message.From.Id, message.Chat.Id,"NoState");
                return _publishPhoto(item,wall.Id,wall.UploadAlbum.Value, message, out flow);
            }
            else
            {
                return new SendMessageModel(message.Chat.Id, "Ээ, что-то я не могу найти такое сообщество.\r\nУточни-ка...");
            }
        }
        private static List<Task> Tasks = new List<Task>();
        private ISendItem _publishPhoto(ISearchResultItem item,int wallId,int albumId, MessageModel message, out MessageFlow flow)
        {
            var task = _context.CompareService.Publish(message.From.Id, message.MessageId, item, message.From.Id, wallId,
                albumId);
            task.ContinueWith(async tsk =>
            {
                if (tsk.Exception != null) throw new Exception(tsk.Exception.Message);
                await _context.BotMethods.BotMethod("sendMessage",
                    new SendMessageModel(message.Chat.Id, "Готово!"));
            });
            Tasks.Add(task);
            flow = null;
            return new SendStickerModel(message.From.Id, "BQADAgADWQADq3KnAj5VX6KhUianAg");
        }
        private ISendItem _publish(string text, MessageModel message, out MessageFlow flow)
        {
            var tag = BotController.UserSearches[message.From.Id];
            var result = BotController.SearchResult[tag];
            var items = result.Items.ToArray();
            var item = items[result.Position];
            flow = new MessageFlow();
            var auths = _context.DbService.GetAuths(message.From.Id).Where(w => w.Auth).ToArray();
            var walls = _context.DbService.GetWalls().Where(w => auths.Any(a => a.WallId == w.Id)).ToArray();
            if (walls.Length > 1)
            {
                _context.DbService.SetState(message.From.Id, message.Chat.Id, "next-choose-wall");
                var keyboard = walls.Select(s => new KeyboardButtonModel(s.Name));
                return new SendMessageModel(message.Chat.Id, "В какое сообщество?")
                {
                    ReplyMarkup = new ReplyKeyboardMarkupModel(keyboard)
                };
            }
            else if (walls.Length == 1)
            {
                var wall = walls.First();
                if (!wall.UploadAlbum.HasValue)
                {
                    return new SendMessageModel(message.Chat.Id, "Администратор паблика не указал для него альбом для загруки, увы пока не могу загрузить фото...");
                }
                _context.DbService.SetState(message.From.Id, message.Chat.Id, "NoState");

                return _publishPhoto(item,wall.Id, wall.UploadAlbum.Value, message, out flow);
            }
            else
            {
                flow.Add(new MessageFlowItem(message.Chat.Id, "Для этого введи /public", TimeSpan.FromMilliseconds(300)));
                return new SendMessageModel(message.Chat.Id, "Слушай, у тебя еще нет настроенного доступа для пабликов");
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
            } else if (post.Any(a => a.Equals(text,StringComparison.CurrentCultureIgnoreCase)))
            {
                return _publish(text, message, out flow);
            } else if (next.Any(a =>a.Equals(text, StringComparison.CurrentCultureIgnoreCase)))
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