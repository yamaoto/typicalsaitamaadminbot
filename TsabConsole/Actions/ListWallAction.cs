using System;

namespace TsabConsole.Actions
{
    internal class ListWallAction : ITsabConsoleAction
    {
        public string ActionName { get; } = "list-wall";
        public string Descriptioin { get; } = "Вывести список стен";
        public string Syntax { get; } = "list-wall";
        public void Exec(string[] args)
        {
            var list = Program.Context.DbService.GetWalls();
            foreach (var wall in list)
            {
                Console.WriteLine("id: '{0}'; name: '{1}'", wall.Id, wall.Name);
            }
        }
    }
}