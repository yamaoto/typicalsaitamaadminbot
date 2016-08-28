using System;

namespace TsabSharedLib
{
    public class StateModel
    {
        public Guid Id { get; set; }
        public string State { get; set; }
        public string StateParams { get; set; }
        public int UserId { get; set; }
        public int UserChatId { get; set; }
        public string UserLastName { get; set; }
        public string UserFirstName { get; set; }
        public string Username { get; set; }
        public Guid AuthId { get; set; }
        public string Properties { get; set; }
    }
}