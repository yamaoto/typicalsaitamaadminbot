using System;

namespace TsabConsole.Actions
{
    internal class ListAdminAction : ITsabConsoleAction
    {
        public string ActionName { get; } = "list-admin";
        public string Descriptioin { get; } = "����� ��������������� �����";
        public string Syntax { get; } = "list-admin <wallId>";

        public void Exec(string[] args)
        {
            int wallId;
            if (args.Length == 2 && int.TryParse(args[1], out wallId))
            {

                var list = Program.Context.DbService.GetWallAdmins(wallId);
                foreach (var wall in list)
                {
                    Console.WriteLine("id: '{0}'; name: '{1}'", wall.UserId, wall.UserFirstName + " " + wall.UserLastName);
                }
            }
            else
            {
                Console.WriteLine("�������� ������ �������.");
                Console.WriteLine("���������: {0}", Syntax);
            }
        }
    }
}