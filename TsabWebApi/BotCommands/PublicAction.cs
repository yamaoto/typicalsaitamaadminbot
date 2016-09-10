using System;
using System.Collections.Generic;
using System.Linq;
using TsabSharedLib;
using TsabWebApi.Models;

namespace TsabWebApi.BotCommands
{
    [BotAction]
    internal class PublicAction : IBotAction
    {
        private BotActionContext _context;
        public void Start(BotActionContext context)
        {
            _context = context;
        }

        public string[] States { get; } = new [] { "choose-public", "wait-auth", "grant-auth" };
        public string CommandName { get; } = "/public";
        public string Description { get; } = "получение доступа к сообществу";

        private readonly DbService _dbService = BotService.GetDbService();

        public ISendItem Command(string command, MessageModel message, out MessageFlow flow)
        {
            var msg = $@"Ok, ты можешь получить доступ к этим сообществам:";
            var msg2 = new SendMessageModel(message.Chat.Id, "К какому сообществу тебя записать?");
            msg2.ReplyMarkup = new ReplyKeyboardMarkupModel()
            {
                OneTimeKeyboard = true,
            };
            var walls = _dbService.GetWalls();
            var btns = new List<KeyboardButtonModel>();
            var auths = _dbService.GetAuths(message.From.Id);
            foreach (var wall in walls.Where(wall => !auths.Any(a => a.WallId == wall.Id && a.Auth)))
            {
                msg = msg + $"\r\n    *) {wall.Name}";
                btns.Add(new KeyboardButtonModel() { Text = wall.Name });
            }
            if (btns.Count == 0)
            {
                flow = new MessageFlow() { new MessageFlowItem(message.Chat.Id, "BQADAgAD-SYAAktqAwABXyf8g9wohKUC", true, TimeSpan.FromSeconds(1)) };
                return new SendMessageModel(message.Chat.Id, "Так, слушай. У нас больше нет сообществ для тебя");
            }
            ((ReplyKeyboardMarkupModel)msg2.ReplyMarkup).Keyboard = btns.Select(s => new[] { s }).ToArray();
            _dbService.SetState(message.From.Id,message.Chat.Id, "choose-public");
            flow = new MessageFlow() { { new MessageFlowItem(msg2, TimeSpan.FromSeconds(2)) } };
            return new SendMessageModel(message.Chat.Id, msg);
        }

        public ISendItem Message(string state,string text, MessageModel message, out MessageFlow flow)
        {
            switch(state)
            {
                case "choose-public":
                    return _stateChoosePublic(text, message, out flow);
                case "wait-auth":
                    return _stateWaitAuth(text, message, out flow);
                case "grant-auth":
                    return _stateGrantAuth(text, message, out flow);
                default:
                    throw new InvalidOperationException();
            }
        }

        internal ISendItem _stateGrantAuth(string text, MessageModel message, out MessageFlow flow)
        {
            var state = _dbService.GetState(message.From.Id);
            var authId = Guid.Parse(state.StateParams);
            var auth = _dbService.GetAuth(authId);
            var admins = _dbService.GetWallAdmins(auth.WallId);

            if (auth.Solved)
            {
                flow = new MessageFlow()
                {
                    new MessageFlowItem(message.From.Id,"BQADBAADtwMAAqKYZgABJFsIZLA51N0C",true,TimeSpan.FromSeconds(1)),
                    new MessageFlowItem(message.From.Id,"Эту проблему уже решили",TimeSpan.FromSeconds(3))
                };
                return new SendMessageModel(message.Chat.Id, "Поздняк метаться");
            }

            var yes = new[] { "yes", "да", "конечно" };
            var no = new[] { "нет", "no", "исключено" };
            var messages = new MessageFlow();
            if (yes.Any(a => a.Equals(text, StringComparison.OrdinalIgnoreCase)))
            {
                _dbService.GrantAuth(authId, true);
                _dbService.SetState(auth.UserId, message.Chat.Id, "NoState");
                _dbService.SetState(message.From.Id, message.Chat.Id, "NoState");
                messages.Add(new MessageFlowItem(auth.UserChatId,
                    $"Отличные новости! {message.From.FirstName} {message.From.LastName} подтвердил твой зарос!"));
                messages.Add(new MessageFlowItem(auth.UserChatId, "BQADBAADgwMAAqKYZgABz5uEcP7gGQ0C", true, TimeSpan.FromSeconds(1)));
                messages.Add(new MessageFlowItem(auth.UserChatId, "Теперь смело можешь отправлять мне картинки на проверку", TimeSpan.FromSeconds(2)));
                foreach (var admin in admins)
                {
                    messages.Add(new MessageFlowItem(admin.UserChatId, $"{message.From.FirstName} {message.From.LastName} принял решение касательно запроса {auth.UserLastName} {auth.UserFirstName} - в доступе разрешить!"));
                    messages.Add(new MessageFlowItem(admin.UserChatId, $"Теперь нас {admins.Count() + 1}!", TimeSpan.FromSeconds(2)));
                    messages.Add(new MessageFlowItem(admin.UserChatId, "BQADBAADgwMAAqKYZgABz5uEcP7gGQ0C", true, TimeSpan.FromSeconds(2)));
                }
                flow = messages;
                return new SendStickerModel(message.Chat.Id, "BQADBAAD3gQAAqKYZgAB12DvgaPftTgC");
            }
            else if (no.Any(a => a.Equals(text, StringComparison.OrdinalIgnoreCase)))
            {
                _dbService.GrantAuth(authId, false);
                _dbService.SetState(auth.UserId, message.Chat.Id, "NoState");
                _dbService.SetState(message.From.Id, message.Chat.Id, "NoState");
                messages.Add(new MessageFlowItem(auth.UserChatId, "Плохие новости! СУК-А направило мне решение"));
                messages.Add(new MessageFlowItem(auth.UserChatId, "В нем указано что тебе запрещено в доступе к сообществу", TimeSpan.FromSeconds(1)));
                messages.Add(new MessageFlowItem(auth.UserChatId, "BQADBAADYgEAAqIkSQPtYd2YcYlnzQI", true, TimeSpan.FromSeconds(1)));
                foreach (var admin in admins)
                {
                    messages.Add(new MessageFlowItem(admin.UserChatId, $"{message.From.FirstName} {message.From.LastName} принял решение отказать {auth.UserLastName} {auth.UserFirstName}"));
                    messages.Add(new MessageFlowItem(admin.UserChatId, "BQADBAADaQEAAqIkSQNakH2j7oO-wgI", true, TimeSpan.FromSeconds(2)));
                }
                flow = messages;
                return new SendStickerModel(message.Chat.Id, "BQADBAADaQEAAqIkSQNakH2j7oO-wgI");
            }
            else
            {
                messages.Add(new MessageFlowItem(message.Chat.Id, "BQADBAADkgMAAqKYZgABPhmkbMd0GJAC", true, TimeSpan.FromMilliseconds(500)));
                messages.Add(new MessageFlowItem(message.Chat.Id, "Очень спешу, давай скорей", true, TimeSpan.FromMilliseconds(500)));
                flow = messages;
                return new SendMessageModel(message.Chat.Id, "Хорош разглагольствовать, пиши да или нет");
            }

        }

        internal ISendItem _stateWaitAuth(string text, MessageModel message, out MessageFlow flow)
        {
            flow = null;
            return new SendMessageModel(message.Chat.Id, ".-.");
        }

        internal ISendItem _stateChoosePublic(string text, MessageModel message, out MessageFlow flow)
        {
            var walls = _dbService.GetWalls();
            var auths = _dbService.GetAuths(message.From.Id);
            var wall = walls.FirstOrDefault(f => f.Name == text);
            if (wall == null)
            {
                flow = null;
                return new SendMessageModel(message.Chat.Id, "Ээ, что-то я не могу найти такое сообщество.\r\nУточни-ка...");
            }
            var check = auths.FirstOrDefault(f => f.WallId == wall.Id);
            if (check != null)
            {
                _dbService.SetState(message.From.Id, message.Chat.Id, "NoState");
                if (check.Auth)
                {
                    flow = new MessageFlow() { new MessageFlowItem(message.Chat.Id, "BQADBAADWwMAAqKYZgAB3mljwfZMlVkC", true, TimeSpan.FromSeconds(1)) };
                    return new SendMessageModel(message.Chat.Id, "Ok, но вообщето я уже раньше тебя записывал в это сообщество, не помнишь?") { ReplyMarkup = new ReplyKeyboardHideModel() { HideKeyboard = true } };
                }
                else
                {
                    flow = new MessageFlow() { new MessageFlowItem(message.Chat.Id, "BQADBAADMAMAAqKYZgABj5c3MVEUXD4C", true, TimeSpan.FromSeconds(1)) };
                    return new SendMessageModel(message.Chat.Id, "Кстати да, ты уже раньше отправлял запрос на доступ к сообществу " + wall.Name + ".\r\nПросто запасись терпением, наши администраторы ну очень занятые..");
                }

            }

            _dbService.SetState(message.From.Id, message.Chat.Id, "wait-auth", wall.Id.ToString());
            _dbService.InsertAuthQuery(message.From.Id, message.Chat.Id, message.From.LastName, message.From.FirstName, message.From.Username, wall.Id);
            var auth = _dbService.GetAuth(message.From.Id, wall.Id);
            var admins = _dbService.GetWallAdmins(wall.Id);
            flow = new MessageFlow()
            {
                new MessageFlowItem(message.Chat.Id, "Я написал заявление в Структурное Управление Кандидатов-Администраторов для разрешения дотсупа к твоему сообщесву", TimeSpan.FromSeconds(2)),
                new MessageFlowItem(message.Chat.Id,"Будем ждать их реакции...",TimeSpan.FromMilliseconds(600)),
                new MessageFlowItem(message.Chat.Id,"BQADBAADkgMAAqKYZgABPhmkbMd0GJAC",true,TimeSpan.FromMilliseconds(500))
            };
            foreach (var admin in admins)
            {
                _dbService.SetState(admin.UserId, admin.UserChatId, "grant-auth", auth.Id.ToString());
                flow.Add(new MessageFlowItem(admin.UserChatId, $"Привет еще раз {admin.UserFirstName}!\r\n('{message.From.FirstName} {message.From.LastName}' запрашивает доступ к сообществу {wall.Name}.\r\n\r\nРазрешить ему?"));
            }

            return new SendMessageModel(message.Chat.Id, "Отлично, теперь осталась небольшая бюрократическая процедура...") { ReplyMarkup = new ReplyKeyboardHideModel() { HideKeyboard = true } };
        }
    }
}