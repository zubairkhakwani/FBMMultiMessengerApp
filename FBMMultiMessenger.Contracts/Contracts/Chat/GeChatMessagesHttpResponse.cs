using FBMMultiMessenger.Contracts.Enums;
using Microsoft.AspNetCore.Components.Forms;

namespace FBMMultiMessenger.Contracts.Contracts.Chat
{
    public class GeChatMessagesHttpResponse
    {
        public int ChatMessageId { get; set; }
        public int ChatId { get; set; }
        public string? FbMessageId { get; set; }
        public string? FbMessageReplyId { get; set; }
        public string Message { get; set; } = string.Empty;

        //The actual message
        public bool IsReceived { get; set; }
        public bool IsTextMessage { get; set; }
        public bool IsVideoMessage { get; set; }
        public bool IsImageMessage { get; set; }
        public bool IsAudioMessage { get; set; }

        public bool IsSent { get; set; } // message status
        public bool IsRead { get; set; }
        public bool ScrollToBottom { get; set; } = true;
        public DateTime CreatedAt { get; set; }
        public long? FbTimeStamp { get; set; }
        public string OfflineUniqueId { get; set; } = string.Empty; // this is being used for 2 cases 
        public bool Sending { get; set; }
        public List<FileData> FileData { get; set; } = new();
        public MessageReplyHttpResponse? MessageReply { get; set; }
    }

    public class MessageReplyHttpResponse
    {
        public MessageReplyType Type { get; set; }
        public string? Reply { get; set; }
        public string ReplyTo { get; set; } = string.Empty;
        public List<MessageReplyFileHttpResponse>? Attachments { get; set; }
    }
    public class MessageReplyFileHttpResponse
    {
        public string Url { get; set; } = string.Empty;
    }

    public class FileData
    {
        public string Id { get; set; } = string.Empty;
        public string PreviewUrl { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public bool IsVideo { get; set; }
        public bool IsEmoji { get; set; }
        public bool IsSticker { get; set; }

        public IBrowserFile File { get; set; } = null!;
        public byte[] CompressedBytes { get; set; } = [];
    }
}
