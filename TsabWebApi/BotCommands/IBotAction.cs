using System.Web;
using TsabWebApi.Models;

namespace TsabWebApi.BotCommands
{
    internal interface IBotAction
    {
        void Start(BotActionContext context);
        string[] States { get; }
        string CommandName { get; }
        string Description { get; }
        ISendItem Command(string command, MessageModel message, out MessageFlow flow);
        ISendItem Message(string sate,string text, MessageModel message, out MessageFlow flow);
    }
}