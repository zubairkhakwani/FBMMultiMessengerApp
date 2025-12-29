using Microsoft.AspNetCore.Components.Forms;

namespace FBMMultiMessenger.Contracts.Contracts.Chat
{
    public class GeChatMessagesHttpResponse
    {
        public string FBChatId { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public bool IsReceived { get; set; }
        public bool IsTextMessage { get; set; }
        public bool IsVideoMessage { get; set; }
        public bool IsImageMessage { get; set; }
        public bool IsAudioMessage { get; set; }
        public bool IsSent { get; set; } // message status
        public DateTime CreatedAt { get; set; }
        public string UniqueId { get; set; } = string.Empty;
        public bool Sending { get; set; }
        public List<FileData> FileData { get; set; } = new();
    }

    public class FileData
    {
        public int Index { get; set; }
        public string Id { get; set; } = string.Empty;
        public string PreviewUrl { get; set; } = string.Empty; 
        public string Name { get; set; } = string.Empty;
        public bool IsVideo { get; set; }
        public IBrowserFile File { get; set; } = null!;
    }
}
