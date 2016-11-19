using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TsabSharedLib
{
    public class VkUser
    {
        public Guid Id { get; set; }
        public string Code { get; set; }
        public string Token { get; set; }
        public DateTime Expires { get; set; }
        public long UserId { get; set; }
        public bool Group { get; set; }
        public int GroupId { get; set; }
    }
}
