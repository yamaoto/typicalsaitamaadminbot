using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TsabSharedLib
{
    public class AuthModel
    {
        public Guid Id { get; set; }
        public int UserId { get; set; }
        public int UserChatId { get; set; }
        public string UserLastName { get; set; }
        public string UserFirstName { get; set; }
        public DateTime ExecDate { get; set; }
        public bool Auth { get; set; }
        public int WallId { get; set; }
        public bool Solved { get; set; }
    }
}
