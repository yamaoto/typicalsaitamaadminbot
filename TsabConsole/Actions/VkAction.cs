using System;
using kasthack.vksharp;

namespace TsabConsole.Actions
{
    internal class VkAction : ITsabConsoleAction
    {
        public string ActionName { get; } = "vk";
        public string Descriptioin { get; } = "Авторизация приложения";
        public string Syntax { get; } = "vk";
        public void Exec(string[] args)
        {
            if (args.Length == 1)
            {
                var id = Guid.NewGuid().ToString("N");
                Console.WriteLine(Program.Context.CompareService.GetVkAuth(id));
            } else if (args.Length == 2)
            {
                var token = new Token(args[1]);
                Console.WriteLine($"value: {token.Value}\r\nsign: {token.Sign}\r\nuserId:{token.UserId}");
            }
        }
    }
}