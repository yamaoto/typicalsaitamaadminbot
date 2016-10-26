using System;

namespace TsabConsole.Actions
{
    internal class LoadWallAction : ITsabConsoleAction
    {
        public string ActionName { get; } = "load-wall";
        public string Descriptioin { get; } = "�������� �����";
        public string Syntax { get; } = "load-wall <wallId>";
        public void Exec(string[] args)
        {
            int wallId;
            if (args.Length == 2 && int.TryParse(args[1], out wallId))
            {
                Program.Context.CompareService.LoadWall(wallId);
                Console.WriteLine("�������� ����� ��������.");
            }
            else
            {
                Console.WriteLine("�������� ������ �������.");
                Console.WriteLine("���������: {0}", Syntax);
            }
        }
    }
}