using System.Collections.Generic;

namespace TsabWebApi.Models
{
    internal class MessageFlow:List<MessageFlowItem>
    {
        public MessageFlow()
        {

        }

        public MessageFlow(IEnumerable<MessageFlowItem> items):base(items)
        {

        }
    }
}