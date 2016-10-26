using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace TsabWebApi.Models
{
    [DataContract]
    public class ReplyKeyboardMarkupModel
    {
        public ReplyKeyboardMarkupModel()
        {

        }

        public ReplyKeyboardMarkupModel(KeyboardButtonModel[][] keyboard)
        {
            Keyboard = keyboard;
        }
        public ReplyKeyboardMarkupModel(IEnumerable<KeyboardButtonModel[]> keyboard)
        {
            Keyboard = keyboard.ToArray();
        }
        public ReplyKeyboardMarkupModel(IEnumerable<KeyboardButtonModel> keyboard)
        {
            Keyboard = keyboard.Select(s => new[] {s}).ToArray();
        }
        [DataMember(Name = "keyboard")]
        public KeyboardButtonModel[][] Keyboard { get; set; }
        [DataMember(Name="resize_keyboard")]
        public bool ResizeKeyboard { get; set; }

        [DataMember(Name = "one_time_keyboard")]
        public bool OneTimeKeyboard { get; set; } = true;
        [DataMember(Name = "selective")]
        public bool Selective { get; set; }
    }
}