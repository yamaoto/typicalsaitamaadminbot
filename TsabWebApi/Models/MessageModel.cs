using System.Runtime.Serialization;

namespace TsabWebApi.Models
{
    [DataContract]
    public class MessageModel
    {
        [DataMember(Name="message_id")]
        public int MessageId { get; set; }
        [DataMember(Name = "from")]
        public UserModel From { get; set; }
        [DataMember(Name = "date")]
        public int Date { get; set; }
        [DataMember(Name = "chat")]
        public ChatModel Chat { get; set; }
        [DataMember(Name="forward_from")]
        public UserModel ForwardFrom { get; set; }
        [DataMember(Name="forward_from_chat")]
        public ChatModel ForwardFromChat { get; set; }
        [DataMember(Name="forward_date")]
        public int ForwardDate { get; set; }
        [DataMember(Name="reply_to_message")]
        public MessageModel ReplyToMessage { get; set; }
        [DataMember(Name="edit_date")]
        public int EditDate { get; set; }
        [DataMember(Name = "text")]
        public string Text { get; set; }
        [DataMember(Name = "entities")]
        public MessageEntryModel[] Entities { get; set; }
        [DataMember(Name = "audio")]
        public AudioModel Audio { get; set; }
        [DataMember(Name = "document")]
        public DocumentModel Document { get; set; }
        [DataMember(Name = "photo")]
        public PhotoSizeModel[] Photo { get; set; }
        [DataMember(Name = "sticker")]
        public StickerModel Sticker { get; set; }
        [DataMember(Name = "video")]
        public VideoModel Video { get; set; }
        [DataMember(Name = "voice")]
        public VoiceModel Voice { get; set; }
        [DataMember(Name = "caption")]
        public string Caption { get; set; }
        [DataMember(Name = "contact")]
        public ContactModel Contact { get; set; }
        [DataMember(Name = "location")]
        public LocationModel Location { get; set; }
        [DataMember(Name = "venue")]
        public VenueModel Venue { get; set; }
        [DataMember(Name="new_chat_user")]
        public UserModel NewChatUser { get; set; }
        [DataMember(Name="left_chat_user")]
        public UserModel LeftChatUser { get; set; }
        [DataMember(Name="new_chat_title")]
        public string NewChatTitle { get; set; }
        [DataMember(Name="new_chat_photo")]
        public PhotoSizeModel[] NewChatPhoto { get; set; }
        [DataMember(Name="delete_chat_photo")]
        public bool DeleteChatPhoto { get; set; }
        [DataMember(Name="group_chat_created")]
        public bool GroupChatCreated { get; set; }
        [DataMember(Name="supergroup_chat_created")]
        public bool SupergroupChatCreated { get; set; }
        [DataMember(Name="channel_chat_created")]
        public bool ChannelChatCreated { get; set; }
        [DataMember(Name="migrate_to_chat_id")]
        public int MigrateToChatId { get; set; }
        [DataMember(Name="migrate_from_chat_id")]
        public int MigrateFromChatId { get; set; }
        [DataMember(Name="pinned_message")]
        public MessageModel PinnedMessage { get; set; }
    }
}