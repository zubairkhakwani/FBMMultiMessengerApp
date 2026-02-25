using FBMMultiMessenger.Contracts.Contracts.Account;
using FBMMultiMessenger.Contracts.Contracts.Chat;
using FBMMultiMessenger.Database.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace FBMMultiMessenger.Database.Services
{
    public class MessageDbService
    {
        private readonly IDbContextFactory<MessengerDbContext> dbFactory;

        public MessageDbService(IDbContextFactory<MessengerDbContext> dbFactory)
        {
            this.dbFactory = dbFactory;
        }

        public async Task MarkChatAsRead(int chatId, int lastLocalMessageId, int currentUserId)
        {
            using var dbContext = dbFactory.CreateDbContext();

            var chat = await dbContext.Chats
                .FirstOrDefaultAsync(c => c.Id == chatId && c.UserId == currentUserId);

            if (chat == null)
            {
                return;
            }

            var messages = await dbContext.ChatMessages.Where(cm => cm.ChatId == chat.Id && !cm.IsRead && cm.Id <= lastLocalMessageId)
                .ToListAsync();

            foreach (var message in messages)
            {
                message.IsRead = true;
                message.UpdatedAt = DateTime.UtcNow;
            }

            if (!chat.IsRead || messages.Any())
            {
                chat.IsRead = true;
                chat.UpdatedAt = DateTime.UtcNow;
                await dbContext.SaveChangesAsync();
            }
        }

        public async Task<List<GeChatMessagesHttpResponse>> GetChatMessages(int chatId, int currentUserId)
        {
            using var db = dbFactory.CreateDbContext();

            var dbChatMessages = db.ChatMessages
                                               .AsNoTracking()
                                               .Include(cm => cm.Chat)
                                               .Where(cm => cm.ChatId == chatId)
                                               .OrderBy(x => x.FBTimestamp).ToList();

            var chatMessages = dbChatMessages.Select(x => new GeChatMessagesHttpResponse()
            {
                ChatMessageId = x.Id,
                ChatId = chatId,
                FbMessageId = x.FbMessageId,
                FbMessageReplyId = x.FbMessageReplyId,
                MessageReply = GetMessageReply(new MessageReplyRequest() { ChatMessages = dbChatMessages, FbMessageReplyId = x.FbMessageReplyId })?.Message,
                MessageReplyTo = GetMessageReply(new MessageReplyRequest() { ChatMessages = dbChatMessages, FbMessageReplyId = x.FbMessageReplyId })?.ReplyTo,
                IsReceived = x.IsReceived,
                IsRead = x.IsRead,
                Message = x.Message,
                FBTimestamp = x.FBTimestamp,
                IsTextMessage = x.IsTextMessage,
                IsImageMessage = x.IsImageMessage,
                IsVideoMessage = x.IsVideoMessage,
                IsAudioMessage = x.IsAudioMessage,
                IsSent = x.IsSent,
                CreatedAt = x.CreatedAt,
            }).ToList();

            return chatMessages;
        }

        public MessageReplyResult? GetMessageReply(MessageReplyRequest request)
        {
            var fbMessageReplyId = request.FbMessageReplyId;

            var chatMessages = request.ChatMessages;

            if (fbMessageReplyId is null) return null;

            var chatMessage = chatMessages.FirstOrDefault(cm => cm.FbMessageId == fbMessageReplyId);

            if (chatMessage is null) return null;

            var result = new MessageReplyResult();

            result.ReplyTo = chatMessage.IsReceived ? chatMessage.Chat.OtherUserName : "You";

            if (chatMessage.IsVideoMessage)
            {
                result.Message = "Video message";
            }
            else if (chatMessage.IsImageMessage)
            {
                result.Message = "Image message";
            }
            else if (chatMessage.IsAudioMessage)
            {
                result.Message = "Audio message";
            }
            result.Message = chatMessage.Message;

            return result;
        }

        public async Task<GetAllMyAccountsChatsHttpResponse> GetAllChats(int currentUserId)
        {
            using var db = dbFactory.CreateDbContext();
            var chats = await db.Chats
                .Include(a => a.Account)
                .Include(cm => cm.ChatMessages)
                .AsNoTracking()
                .Where(u => u.UserId == currentUserId)
                .OrderByDescending(x => x.ChatMessages.Max(cm => (long?)cm.FBTimestamp))
                .ToListAsync();

            var formattedChats = chats.Select(x =>
            {
                var lastMessage = x.ChatMessages
                                   .MaxBy(x => x.FBTimestamp)
                                    ??
                                    new ChatMessages() { Message = string.Empty };

                var account = x.Account;
                var isAccountConnected = false; //account is not null && account.AuthStatus == AccountAuthStatus.LoggedIn;
                var chatAccount = account is null ? null : new GetMyChatAccountHttpResponse()
                {
                    Id = account.Id,
                    Name = account.Name,
                    CreatedAt = account.CreatedAt
                };

                var messagePreview = GetMessagePreview(ToMessagePreviewRequest(lastMessage, x.OtherUserName ?? ""));

                return new GetMyChatsHttpResponse
                {
                    ChatId = x.Id,
                    FbChatId = x.FBChatId!,
                    FbListingTitle = x.FbListingTitle,
                    FbListingLocation = x.FbListingLocation,
                    FbListingPrice = x.FbListingPrice,
                    FbListingImage = x.FBListingImage,
                    UserProfileImage = x.UserProfileImage,
                    MessagePreview = messagePreview.MessagPreview,
                    SenderName = messagePreview.SenderName,
                    ChattingWithName = x.OtherUserName,
                    ChattingWithId = x.OtherUserId,
                    IsRead = x.IsRead,
                    UnReadCount = x.ChatMessages.Count(m => !m.IsRead),
                    StartedAt = x.StartedAt,
                    IsAccountConnected = isAccountConnected,
                    Account = chatAccount
                };

            }).ToList();

            return new GetAllMyAccountsChatsHttpResponse() { Chats = formattedChats };
        }

        public MessagePreviewRequest ToMessagePreviewRequest(ChatMessages source, string fbListingTitle)
        {
            List<string> messages;

            if (string.IsNullOrWhiteSpace(source.Message))
            {
                messages = new List<string>();
            }
            else
            {
                if (source.Message.TrimStart().StartsWith("["))
                {
                    messages = JsonSerializer.Deserialize<List<string>>(source.Message)
                               ?? new List<string> { source.Message };
                }
                else
                {
                    messages = new List<string> { source.Message };
                }
            }
            return new MessagePreviewRequest()
            {
                OtherUserName = fbListingTitle,
                IsImageMessage = source.IsImageMessage,
                IsVideoMessage = source.IsVideoMessage,
                IsAudioMessage = source.IsAudioMessage,
                IsReceived = source.IsReceived,
                Messages = messages
            };
        }

        public MessagePreviewResult GetMessagePreview(MessagePreviewRequest request)
        {
            var senderName = "You";

            if (request.IsReceived && !string.IsNullOrWhiteSpace(request.OtherUserName))
            {
                senderName = request.OtherUserName.Split(' ', StringSplitOptions.RemoveEmptyEntries)[0];
            }

            var messageCount = request.Messages.Count;

            var countText = $"{(messageCount > 1 ? messageCount : "a")}";
            var filesText = $"{(messageCount > 1 ? "s" : "")}";

            string messagePreview = request switch
            {
                { IsImageMessage: true } => $"sent {countText} photo{filesText}",
                { IsVideoMessage: true } => $"sent {countText} video{filesText}",
                { IsAudioMessage: true } => $"sent {countText} audio{filesText}",

                _ => request.Messages.FirstOrDefault()!
            };

            return new MessagePreviewResult() { MessagPreview = messagePreview, SenderName = senderName };
        }

        public class MessagePreviewRequest
        {
            public List<string> Messages { get; set; } = new();
            public bool IsReceived { get; set; }
            public bool IsImageMessage { get; set; }
            public bool IsVideoMessage { get; set; }
            public bool IsAudioMessage { get; set; }
            public string? OtherUserName { get; set; }
        }

        public class MessagePreviewResult
        {
            public string MessagPreview { get; set; } = string.Empty;
            public string SenderName { get; set; } = string.Empty;
        }

        public class MessageReplyRequest
        {
            public string? FbMessageReplyId { get; set; }
            public List<ChatMessages> ChatMessages = new();
        }

        public class MessageReplyResult
        {
            public string Message { get; set; } = string.Empty;
            public string ReplyTo { get; set; } = string.Empty;
        }
    }
}
