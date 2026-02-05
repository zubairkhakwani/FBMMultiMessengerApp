using Microsoft.AspNetCore.Components.Forms;

namespace FBMMultiMessenger.Contracts.Contracts.Chat
{
    public class GeChatMessagesHttpResponse
    {
        public int ChatMessageId { get; set; }
        public int ChatId { get; set; }
        public string? FbMessageId { get; set; } // Facebook Message id
        public string? FbMessageReplyId { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? MessageReply { get; set; } // Created on the client side
        public bool IsReceived { get; set; }
        public bool IsTextMessage { get; set; }
        public bool IsVideoMessage { get; set; }
        public bool IsImageMessage { get; set; }
        public bool IsAudioMessage { get; set; }
        public bool IsSent { get; set; } // message status
        public bool ScrollToBottom { get; set; } = true;
        public DateTime CreatedAt { get; set; }
        public string OfflineUniqueId { get; set; } = string.Empty; // this is being used for 2 cases 
        public bool Sending { get; set; }
        public List<FileData> FileData { get; set; } = new();
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
