using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using TsabWebApi.Controllers;
using TsabWebApi.Models;

namespace TsabWebApi.BotCommands
{
    [BotAction]
    internal class SearchAction :IBotAction
    {
        private BotActionContext _context;
        public void Start(BotActionContext context)
        {
            _context = context;
        }

        public string[] States { get; } = new[] {"search-choose-tag"};
        public string CommandName { get; } = "/search";
        public string Description { get; } = "поиск картинок";

        public ISendItem Command(string command, MessageModel message, out MessageFlow flow)
        {
            var tags = new[] { "#opm", "#one_punch_man", "#onepunchman", "#saitama", "#genos" };
            flow = null;
            var keyboad = tags.Select(tag => new KeyboardButtonModel() { Text = tag });
            var reply = new ReplyKeyboardMarkupModel(keyboad);
            var msg = new SendMessageModel(message.Chat.Id, "Давай поищем, только скажи по какому тегу?", reply);
            _context.DbService.SetState(message.From.Id,message.From.Id, "search-choose-tag");
            return msg;
        }

        public ISendItem Message(string state, string text, MessageModel message, out MessageFlow flow)
        {
            switch (state)
            {
                case "search-choose-tag":
                    return _chooseTag(text,message, out flow);
                default:
                    throw new ArgumentException(nameof(state));
            }
        }

        private ISendItem _chooseTag(string tag, MessageModel message, out MessageFlow flow)
        {
            if (tag.StartsWith("#"))
                tag = tag.Substring(1);
            if (tag.Contains(" "))
            {
                flow = null;
                return new SendMessageModel(message.Chat.Id, "Что-то не похоже на тег...");
            }
            _context.DbService.SetState(message.From.Id, message.From.Id, "NoState");
            _search(message, tag);
            flow = new MessageFlow()
            {
                new MessageFlowItem(message.Chat.Id, "BQADBAADTAUAAqKYZgABfaNgr6BIuFIC", true,
                    TimeSpan.FromMilliseconds(300))
            };
            return new SendMessageModel(message.Chat.Id, "Подожди некоторое время...")
            {
                ReplyMarkup = new ReplyKeyboardHideModel() { HideKeyboard =true}
            };
        }

        private Task _search(MessageModel message, string tag)
        {            
            var task = Task.Run(() =>
            {
                var searchResult = _context.SearchService.Search(tag, 20).OrderByDescending(o => o.Score);
                BotController.UserSearches[message.From.Id] = tag;
                BotController.SearchResult[tag] = new SearchResultModel()
                {
                    Items = searchResult.Select(s=>new SearchResultItemModel(s)),
                    Tag = tag,
                    UserId = message.From.Id,
                    Position = 0
                };
                _context.BotMethods.BotMethod("sendMessage",
                   new SendMessageModel(message.Chat.Id,
                       "Итак, вот что мне удалось найти по запросу #" + tag)).Wait();
                Thread.Sleep(300);
                _context.BotMethods.BotMethod("sendMessage",
                    new SendMessageModel(message.Chat.Id,
                        "http://typical-saitama-admin-bot.azurewebsites.net/search?tag=" + tag)).Wait();
                var commands = new[] {"/next", "/cansel"};
                _context.BotMethods.BotMethod("sendMessage",
                   new SendMessageModel(message.Chat.Id,
                       "Или напиши /next чтобы я отправил первый результат" + tag)
                   {
                       ReplyMarkup = new ReplyKeyboardMarkupModel()
                       {
                           Keyboard = commands.Select(s=>new[] {new KeyboardButtonModel() {Text = s} }).ToArray()
                       }
                   }).Wait();
                Thread.Sleep(300);
            });
            return task;
        }

    }
}