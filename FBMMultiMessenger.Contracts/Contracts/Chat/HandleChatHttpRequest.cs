namespace FBMMultiMessenger.Contracts.Contracts.Chat
{
    public class HandleChatHttpRequest
    {

        //[Required]
        public string? FbChatId { get; set; }

        //[Required]
        public string? FbAccountId { get; set; }

        //[Required]
        public string? FbListingId { get; set; }

        //[Required]
        public string? FbListingTitle { get; set; }

        //[Required]
        public string? FbListingLocation { get; set; }

        //[Required]
        public decimal? FbListingPrice { get; set; }

        // [Required]
        public List<string>? Messages { get; set; }
        public bool IsTextMessage { get; set; }
        public bool IsVideoMessage { get; set; }
        public bool IsImageMessage { get; set; }
        public bool IsAudioMessage { get; set; }

        public bool IsSent { get; set; } //this bit will determine whether the message is received to user or the user has sent it.

    }

    public class HandleChatHttpResponse
    {
        public int ChatId { get; set; }
        public int ChatMessageId { get; set; }
        public string FbUserId { get; set; } = null!;
        public string FbChatId { get; set; } = null!;
        public string FbListingId { get; set; } = null!;
        public string FbAccountId { get; set; } = null!;
        public string? FbMessageId { get; set; } = null!;
        public string? FbMessageReplyId { get; set; }
        public string? FbListingTitle { get; set; }
        public string? FbListingLocation { get; set; }
        public decimal? FbListingPrice { get; set; }
        public string? FbListingImage { get; set; }
        public string MessagPreview { get; set; } = string.Empty;
        public string MessagePreviewFrom { get; set; } = string.Empty;
        public string? UserProfileImage { get; set; }
        public string Message { get; set; } = null!;
        public string? MessageReply { get; set; }
        public string? MessageReplyTo { get; set; }
        public string? OfflineUniqueId { get; set; }
        public bool IsRead { get; set; }

        public bool IsTextMessage { get; set; }
        public bool IsVideoMessage { get; set; }
        public bool IsImageMessage { get; set; }
        public bool IsAudioMessage { get; set; }

        public bool IsReceived { get; set; }

        public DateTime StartedAt { get; set; }
    }
}
