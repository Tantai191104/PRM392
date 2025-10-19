using ChatService.Application.DTOs;
using ChatService.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace ChatService.Infrastructure.Hubs
{
    [Authorize]
    public class ChatHub : Hub
    {
        private readonly IChatAppService _chatService;
        private readonly ILogger<ChatHub> _logger;

        public ChatHub(IChatAppService chatService, ILogger<ChatHub> logger)
        {
            _chatService = chatService;
            _logger = logger;
        }

        public override async Task OnConnectedAsync()
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                         ?? Context.User?.FindFirst("sub")?.Value;

            if (!string.IsNullOrEmpty(userId))
            {
                // Add user to their personal group
                await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");
                _logger.LogInformation("User {UserId} connected to ChatHub with connection {ConnectionId}", 
                    userId, Context.ConnectionId);
            }

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                         ?? Context.User?.FindFirst("sub")?.Value;

            if (!string.IsNullOrEmpty(userId))
            {
                _logger.LogInformation("User {UserId} disconnected from ChatHub", userId);
            }

            await base.OnDisconnectedAsync(exception);
        }

        /// <summary>
        /// Join a specific chat room
        /// </summary>
        public async Task JoinChat(string chatId)
        {
            var userId = GetUserId();
            if (string.IsNullOrEmpty(userId))
            {
                throw new HubException("User not authenticated");
            }

            // Verify user is participant
            var chat = await _chatService.GetChatByIdAsync(chatId);
            if (chat == null)
            {
                throw new HubException("Chat not found");
            }

            if (userId != chat.BuyerId && userId != chat.SellerId)
            {
                throw new HubException("You are not a participant in this chat");
            }

            await Groups.AddToGroupAsync(Context.ConnectionId, $"chat_{chatId}");
            _logger.LogInformation("User {UserId} joined chat {ChatId}", userId, chatId);

            // Mark as read when joining
            await _chatService.MarkAsReadAsync(chatId, userId);
        }

        /// <summary>
        /// Leave a chat room
        /// </summary>
        public async Task LeaveChat(string chatId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"chat_{chatId}");
            _logger.LogInformation("User left chat {ChatId}", chatId);
        }

        /// <summary>
        /// Send message to chat room
        /// </summary>
        public async Task SendMessage(string chatId, SendMessageDto dto)
        {
            var userId = GetUserId();
            if (string.IsNullOrEmpty(userId))
            {
                throw new HubException("User not authenticated");
            }

            try
            {
                // Save message to database
                var message = await _chatService.SendMessageAsync(chatId, userId, dto);

                // Broadcast to all users in the chat room
                await Clients.Group($"chat_{chatId}").SendAsync("ReceiveMessage", message);

                // Also notify participants even if they're not in the room (for unread count)
                var chat = await _chatService.GetChatByIdAsync(chatId);
                if (chat != null)
                {
                    // Notify buyer
                    await Clients.Group($"user_{chat.BuyerId}").SendAsync("MessageNotification", new
                    {
                        chatId,
                        message.Id,
                        unreadCount = chat.BuyerUnreadCount
                    });

                    // Notify seller
                    await Clients.Group($"user_{chat.SellerId}").SendAsync("MessageNotification", new
                    {
                        chatId,
                        message.Id,
                        unreadCount = chat.SellerUnreadCount
                    });
                }

                _logger.LogInformation("Message {MessageId} sent in chat {ChatId}", message.Id, chatId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending message in chat {ChatId}", chatId);
                throw new HubException($"Failed to send message: {ex.Message}");
            }
        }

        /// <summary>
        /// Typing indicator
        /// </summary>
        public async Task Typing(string chatId, bool isTyping)
        {
            var userId = GetUserId();
            if (string.IsNullOrEmpty(userId))
            {
                return;
            }

            // Broadcast typing status to other participants (exclude sender)
            await Clients.GroupExcept($"chat_{chatId}", Context.ConnectionId)
                .SendAsync("UserTyping", new { userId, chatId, isTyping });
        }

        /// <summary>
        /// Mark messages as read
        /// </summary>
        public async Task MarkAsRead(string chatId)
        {
            var userId = GetUserId();
            if (string.IsNullOrEmpty(userId))
            {
                return;
            }

            await _chatService.MarkAsReadAsync(chatId, userId);

            // Notify other participants
            await Clients.GroupExcept($"chat_{chatId}", Context.ConnectionId)
                .SendAsync("MessagesRead", new { userId, chatId });
        }

        private string? GetUserId()
        {
            return Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                   ?? Context.User?.FindFirst("sub")?.Value;
        }
    }
}

