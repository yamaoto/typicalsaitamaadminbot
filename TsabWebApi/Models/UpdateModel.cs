using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;

namespace TsabWebApi.Models
{
    [DataContract]
    public class UpdateModel
    {
        [DataMember(Name="update_id")]
        public int UpdateId { get; set; }
        [DataMember(Name = "message")]
        public MessageModel Message { get; set; }
        [DataMember(Name="edited_message")]
        public MessageModel EditedMessage { get; set; }
        [DataMember(Name="inline_query")]
        public InlineQueryModel InlineQuery { get; set; }
        [DataMember(Name="chosen_inline_result")]
        public ChosenInlineResultModel ChosenInlineResult { get; set; }
        [DataMember(Name="callback_query")]
        public CallbackQueryModel CallbackQuery { get; set; }
    }
}