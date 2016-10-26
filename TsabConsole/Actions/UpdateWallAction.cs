using System;

namespace TsabConsole.Actions
{
    internal class UpdateWallAction : ITsabConsoleAction
    {
        public string ActionName { get; } = "update-wall";
        public string Descriptioin { get; } = "���������� �����";
        public string Syntax { get; } = "update-wall <wallId>";
        public void Exec(string[] args)
        {
            int wallId;
            if (args.Length>=2&&int.TryParse(args[1], out wallId))
            {
                Program.Context.CompareService.UpdateWall(wallId);
                Console.WriteLine("���������� ����� ��������.");
            }
            else
            {
                Console.WriteLine("�������� ������ �������.");
                Console.WriteLine("���������: {0}", Syntax);
            }
        }
    }
}