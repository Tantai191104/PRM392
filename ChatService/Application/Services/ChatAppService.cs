using ChatService.Application.DTOs;
using ChatService.Domain.Entities;
using ChatService.Infrastructure.Repositories;

namespace ChatService.Application.Services
{
    public interface IChatAppService
    {
        Task<ChatResponseDto> GetOrCreateChatAsync(string buyerId, string listingId, string sellerId);
        Task<ChatResponseDto?> GetChatByIdAsync(string chatId);
        Task<List<ChatResponseDto>> GetUserChatsAsync(string userId);
        Task<ChatMessageDto> SendMessageAsync(string chatId, string senderId, SendMessageDto dto);
        Task<List<ChatMessageDto>> GetChatMessagesAsync(string chatId, int page = 1, int pageSize = 50);
        Task MarkAsReadAsync(string chatId, string userId);
    }

    public class ChatAppService : IChatAppService
    {
        private readonly IChatRepository _chatRepo;
        private readonly IMessageRepository _messageRepo;
        private readonly ILogger<ChatAppService> _logger;

        public ChatAppService(
            IChatRepository chatRepo,
            IMessageRepository messageRepo,
            ILogger<ChatAppService> logger)
        {
            _chatRepo = chatRepo;
            _messageRepo = messageRepo;
            _logger = logger;
        }

        public async Task<ChatResponseDto> GetOrCreateChatAsync(string buyerId, string listingId, string sellerId)
        {
            // Check if chat already exists
            var existingChat = await _chatRepo.GetByListingAndBuyerAsync(listingId, buyerId);
            if (existingChat != null)
            {
                return await MapToDtoAsync(existingChat);
            }

            // Create new chat
            var chat = new Chat
            {
                ListingId = listingId,
                BuyerId = buyerId,
                SellerId = sellerId
            };

            await _chatRepo.CreateAsync(chat);
            _logger.LogInformation("Created chat {ChatId} between buyer {BuyerId} and seller {SellerId}", 
                chat.Id, buyerId, sellerId);

            return await MapToDtoAsync(chat);
        }

        public async Task<ChatResponseDto?> GetChatByIdAsync(string chatId)
        {
            var chat = await _chatRepo.GetByIdAsync(chatId);
            return chat != null ? await MapToDtoAsync(chat) : null;
        }

        public async Task<List<ChatResponseDto>> GetUserChatsAsync(string userId)
        {
            var chats = await _chatRepo.GetByUserIdAsync(userId);
            var dtos = new List<ChatResponseDto>();

            foreach (var chat in chats)
            {
                dtos.Add(await MapToDtoAsync(chat));
            }

            return dtos;
        }

        public async Task<ChatMessageDto> SendMessageAsync(string chatId, string senderId, SendMessageDto dto)
        {
            var chat = await _chatRepo.GetByIdAsync(chatId);
            if (chat == null)
                throw new ArgumentException("Chat not found");

            // Verify sender is buyer or seller
            if (senderId != chat.BuyerId && senderId != chat.SellerId)
                throw new UnauthorizedAccessException("You are not a participant in this chat");

            // Create message
            var message = new ChatMessage
            {
                ChatId = chatId,
                SenderId = senderId,
                Content = dto.Content,
                Attachments = dto.Attachments ?? new List<MessageAttachment>(),
                ReadBy = new List<string> { senderId } // Sender has already read their own message
            };

            await _messageRepo.CreateAsync(message);

            // Update chat
            chat.LastMessageAt = DateTime.UtcNow;
            
            // Increment unread count for receiver
            if (senderId == chat.BuyerId)
            {
                chat.SellerUnreadCount++;
            }
            else
            {
                chat.BuyerUnreadCount++;
            }

            await _chatRepo.UpdateAsync(chat);

            _logger.LogInformation("Message sent in chat {ChatId} by {SenderId}", chatId, senderId);

            return MapMessageToDto(message);
        }

        public async Task<List<ChatMessageDto>> GetChatMessagesAsync(string chatId, int page = 1, int pageSize = 50)
        {
            var skip = (page - 1) * pageSize;
            var messages = await _messageRepo.GetByChatIdAsync(chatId, pageSize, skip);
            
            // Reverse to show oldest first
            messages.Reverse();
            
            return messages.Select(MapMessageToDto).ToList();
        }

        public async Task MarkAsReadAsync(string chatId, string userId)
        {
            var chat = await _chatRepo.GetByIdAsync(chatId);
            if (chat == null)
                throw new ArgumentException("Chat not found");

            // Mark messages as read
            await _messageRepo.MarkAsReadAsync(chatId, userId);

            // Reset unread count
            if (userId == chat.BuyerId)
            {
                chat.BuyerUnreadCount = 0;
            }
            else if (userId == chat.SellerId)
            {
                chat.SellerUnreadCount = 0;
            }

            await _chatRepo.UpdateAsync(chat);

            _logger.LogInformation("Marked messages as read in chat {ChatId} for user {UserId}", chatId, userId);
        }

        private async Task<ChatResponseDto> MapToDtoAsync(Chat chat)
        {
            // Get last message
            var lastMessages = await _messageRepo.GetByChatIdAsync(chat.Id, 1, 0);
            var lastMessage = lastMessages.FirstOrDefault();

            return new ChatResponseDto
            {
                Id = chat.Id,
                ListingId = chat.ListingId,
                BuyerId = chat.BuyerId,
                SellerId = chat.SellerId,
                OrderId = chat.OrderId,
                LastMessageAt = chat.LastMessageAt,
                BuyerUnreadCount = chat.BuyerUnreadCount,
                SellerUnreadCount = chat.SellerUnreadCount,
                CreatedAt = chat.CreatedAt,
                LastMessage = lastMessage != null ? MapMessageToDto(lastMessage) : null
            };
        }

        private ChatMessageDto MapMessageToDto(ChatMessage message)
        {
            return new ChatMessageDto
            {
                Id = message.Id,
                ChatId = message.ChatId,
                SenderId = message.SenderId,
                Content = message.Content,
                Attachments = message.Attachments,
                SystemFlag = message.SystemFlag,
                ReadBy = message.ReadBy,
                CreatedAt = message.CreatedAt
            };
        }
    }
}

