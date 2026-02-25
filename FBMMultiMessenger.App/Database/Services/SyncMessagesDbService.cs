using FBMMultiMessenger.Contracts.Contracts.Chat;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FBMMultiMessenger.Database.Services
{
    public class SyncMessagesDbService
    {
        private readonly IDbContextFactory<MessengerDbContext> dbFactory;
        private SemaphoreSlim _syncLock = new SemaphoreSlim(1, 1);

        public SyncMessagesDbService(IDbContextFactory<MessengerDbContext> dbFactory)
        {
            this.dbFactory = dbFactory;
        }

        public async Task<DateTimeOffset?> GetLastSyncDateTime()
        {
            using var db = dbFactory.CreateDbContext();

            var syncMeta = await db.SyncMeta.FirstOrDefaultAsync();

            return syncMeta?.LastSyncedAt;
        }

        public async Task UpdateLastSyncDateTime(DateTimeOffset lastSyncedAt)
        {
            using var db = dbFactory.CreateDbContext();

            var syncMeta = await db.SyncMeta.FirstOrDefaultAsync();

            if(syncMeta != null)
            {
                syncMeta.LastSyncedAt = lastSyncedAt;
                await db.SaveChangesAsync();

                return;
            }

            db.SyncMeta.Add(new()
            {
                Key = "chatMessages",
                LastSyncedAt = lastSyncedAt,
            });

            await db.SaveChangesAsync();
        }

        public async Task<bool> UpdateDbMessagesFromAPI(GetUnSyncedMessagesHttpResponse data)
        {
            await _syncLock.WaitAsync();

            var chats = data.Chats;
            var accounts = data.Accounts;

            if(!chats.Any() && !accounts.Any())
            {
                _syncLock.Release();
                return false;
            }

            try
            {
                using var db = dbFactory.CreateDbContext();

                foreach (var account in accounts)
                {
                    var dbAccount = await db.Accounts.FirstOrDefaultAsync(a => a.Id == account.Id);
                    if (dbAccount != null)
                    {
                        dbAccount.Name = account.Name;
                        dbAccount.FbAccountId = account.FbAccountId;
                        dbAccount.CreatedAt = account.CreatedAt;
                        dbAccount.UpdatedAt = account.UpdatedAt;
                        dbAccount.IsActive = account.IsActive;
                    }
                    else
                    {
                        var newAccount = new Models.Account
                        {
                            Id = account.Id,
                            Name = account.Name,
                            FbAccountId = account.FbAccountId,
                            CreatedAt = account.CreatedAt,
                            UpdatedAt = account.UpdatedAt,
                            IsActive = account.IsActive
                        };

                        db.Accounts.Add(newAccount);
                    }
                }

                await db.SaveChangesAsync();
                db.ChangeTracker.Clear(); // clear tracker before adding chats

                foreach (var chat in chats)
                {
                    var dbChat = db.Chats.Include(c => c.ChatMessages).FirstOrDefault(c => c.Id == chat.Id);
                    if (dbChat is null)
                    {
                        var ch = new FBMMultiMessenger.Database.Models.Chat()
                        {
                            Id = chat.Id,
                            AccountId = chat.AccountId,
                            UserId = chat.UserId,
                            FbUserId = chat.FbUserId,
                            FbAccountId = chat.FbAccountId,
                            FBChatId = chat.FBChatId,
                            FbListingId = chat.FbListingId,
                            FbListingTitle = chat.FbListingTitle,
                            FbListingLocation = chat.FbListingLocation,
                            FBListingImage = chat.FBListingImage,
                            UserProfileImage = chat.UserProfileImage,
                            OtherUserName = chat.OtherUserName,
                            OtherUserId = chat.OtherUserId,
                            FbListingPrice = chat.FbListingPrice,
                            IsRead = chat.IsRead,
                            StartedAt = chat.StartedAt,
                            UpdatedAt = chat.UpdatedAt,
                            ChatMessages = chat.ChatMessages.Select(cm => new Models.ChatMessages
                            {
                                Id = cm.Id,
                                ChatId = cm.ChatId,
                                FbMessageId = cm.FbMessageId,
                                FbMessageReplyId = cm.FbMessageReplyId,
                                FBTimestamp = cm.FBTimestamp,
                                Message = cm.Message,
                                IsReceived = cm.IsReceived,
                                IsRead = cm.IsRead,
                                IsSent = cm.IsSent,
                                IsTextMessage = cm.IsTextMessage,
                                IsImageMessage = cm.IsImageMessage,
                                IsVideoMessage = cm.IsVideoMessage,
                                IsAudioMessage = cm.IsAudioMessage,
                                CreatedAt = cm.CreatedAt,
                            }).ToList(),
                        };

                        db.Chats.Add(ch);
                    }
                    else
                    {
                        dbChat.AccountId = chat.AccountId;
                        dbChat.UserId = chat.UserId;
                        dbChat.FbUserId = chat.FbUserId;
                        dbChat.FbAccountId = chat.FbAccountId;
                        dbChat.FBChatId = chat.FBChatId;
                        dbChat.FbListingId = chat.FbListingId;
                        dbChat.FbListingTitle = chat.FbListingTitle;
                        dbChat.FbListingLocation = chat.FbListingLocation;
                        dbChat.FBListingImage = chat.FBListingImage;
                        dbChat.UserProfileImage = chat.UserProfileImage;
                        dbChat.OtherUserName = chat.OtherUserName;
                        dbChat.OtherUserId = chat.OtherUserId;
                        dbChat.FbListingPrice = chat.FbListingPrice;
                        dbChat.IsRead = chat.IsRead;
                        dbChat.StartedAt = chat.StartedAt;
                        dbChat.UpdatedAt = chat.UpdatedAt;

                        var messagesToUpdate = chat.ChatMessages.Where(cm => dbChat.ChatMessages.Any(dcm => dcm.Id == cm.Id)).ToList();

                        var messagesToAdd = chat.ChatMessages.Where(cm => !dbChat.ChatMessages.Any(dcm => dcm.Id == cm.Id))
                            .Select(cm => new Models.ChatMessages
                            {
                                Id = cm.Id,
                                ChatId = cm.ChatId,
                                FbMessageId = cm.FbMessageId,
                                FbMessageReplyId = cm.FbMessageReplyId,
                                FBTimestamp = cm.FBTimestamp,
                                Message = cm.Message,
                                IsReceived = cm.IsReceived,
                                IsRead = cm.IsRead,
                                IsSent = cm.IsSent,
                                IsTextMessage = cm.IsTextMessage,
                                IsImageMessage = cm.IsImageMessage,
                                IsVideoMessage = cm.IsVideoMessage,
                                IsAudioMessage = cm.IsAudioMessage,
                                CreatedAt = cm.CreatedAt,
                            }).ToList();

                        db.ChatMessages.AddRange(messagesToAdd.ToList());

                        foreach (var msgToUpdate in messagesToUpdate)
                        {
                            var dbMessage = dbChat.ChatMessages.FirstOrDefault(cm => cm.Id == msgToUpdate.Id);

                            dbMessage.ChatId = msgToUpdate.ChatId;
                            dbMessage.FbMessageId = msgToUpdate.FbMessageId;
                            dbMessage.FbMessageReplyId = msgToUpdate.FbMessageReplyId;
                            dbMessage.FBTimestamp = msgToUpdate.FBTimestamp;
                            dbMessage.Message = msgToUpdate.Message;
                            dbMessage.IsReceived = msgToUpdate.IsReceived;
                            dbMessage.IsRead = msgToUpdate.IsRead;
                            dbMessage.IsSent = msgToUpdate.IsSent;
                            dbMessage.IsTextMessage = msgToUpdate.IsTextMessage;
                            dbMessage.IsImageMessage = msgToUpdate.IsImageMessage;
                            dbMessage.IsVideoMessage = msgToUpdate.IsVideoMessage;
                            dbMessage.IsAudioMessage = msgToUpdate.IsAudioMessage;
                            dbMessage.CreatedAt = msgToUpdate.CreatedAt;
                            dbMessage.UpdatedAt = msgToUpdate.CreatedAt;
                        }
                    }
                }

                await db.SaveChangesAsync();

                return true;
            }
            catch(Exception ex)
            {
                return false;
            }
            finally
            {
                _syncLock.Release();
            }
        }
    }
}
