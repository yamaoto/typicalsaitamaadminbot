using System;

namespace TsabConsole.Actions
{
    internal class UpdateWallAction : ITsabConsoleAction
    {
        public string ActionName { get; } = "update-wall";
        public string Descriptioin { get; } = "Обновление стены";
        public string Syntax { get; } = "update-wall <wallId>";
        public void Exec(string[] args)
        {
            int wallId;
            if (args.Length>=2&&int.TryParse(args[1], out wallId))
            {
                Program.Context.CompareService.UpdateWall(wallId);
                Console.WriteLine("Обновление стены завршено.");
            }
            else
            {
                Console.WriteLine("Неверный формат команды.");
                Console.WriteLine("Синтаксис: {0}", Syntax);
            }
        }
    }
}