using ChatService.Application.DTOs;
using ChatService.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ChatService.Web.Controllers
{
    [ApiController]
    [Route("api/chats")]
    [Authorize]
    public class ChatController : ControllerBase
    {
        private readonly IChatAppService _chatService;
        private readonly ILogger<ChatController> _logger;

        public ChatController(IChatAppService chatService, ILogger<ChatController> logger)
        {
            _chatService = chatService;
            _logger = logger;
        }

        /// <summary>
        /// Get or create chat for a listing
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> GetOrCreateChat([FromBody] CreateChatDto dto)
        {
            try
            {
                var buyerId = GetUserId();
                if (string.IsNullOrEmpty(buyerId))
                    return Unauthorized();

                var chat = await _chatService.GetOrCreateChatAsync(buyerId, dto.ListingId, dto.SellerId);
                return Ok(new { success = true, data = chat });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating chat");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Get chat by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetChatById(string id)
        {
            try
            {
                var chat = await _chatService.GetChatByIdAsync(id);
                if (chat == null)
                    return NotFound(new { success = false, message = "Chat not found" });

                var userId = GetUserId();
                if (userId != chat.BuyerId && userId != chat.SellerId)
                    return Forbid();

                return Ok(new { success = true, data = chat });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting chat {ChatId}", id);
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Get user's chats
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetUserChats()
        {
            try
            {
                var userId = GetUserId();
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized();

                var chats = await _chatService.GetUserChatsAsync(userId);
                return Ok(new { success = true, data = chats, count = chats.Count });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user chats");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Get chat messages (with pagination)
        /// </summary>
        [HttpGet("{chatId}/messages")]
        public async Task<IActionResult> GetChatMessages(string chatId, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
        {
            try
            {
                var chat = await _chatService.GetChatByIdAsync(chatId);
                if (chat == null)
                    return NotFound(new { success = false, message = "Chat not found" });

                var userId = GetUserId();
                if (userId != chat.BuyerId && userId != chat.SellerId)
                    return Forbid();

                var messages = await _chatService.GetChatMessagesAsync(chatId, page, pageSize);
                return Ok(new { success = true, data = messages, page, pageSize, count = messages.Count });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting messages for chat {ChatId}", chatId);
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Send message (REST fallback for clients that don't support SignalR)
        /// </summary>
        [HttpPost("{chatId}/messages")]
        public async Task<IActionResult> SendMessage(string chatId, [FromBody] SendMessageDto dto)
        {
            try
            {
                var userId = GetUserId();
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized();

                var message = await _chatService.SendMessageAsync(chatId, userId, dto);
                return Ok(new { success = true, data = message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending message");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Mark messages as read
        /// </summary>
        [HttpPost("{chatId}/read")]
        public async Task<IActionResult> MarkAsRead(string chatId)
        {
            try
            {
                var userId = GetUserId();
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized();

                await _chatService.MarkAsReadAsync(chatId, userId);
                return Ok(new { success = true, message = "Messages marked as read" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking messages as read");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        private string? GetUserId()
        {
            return User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                   ?? User.FindFirst("sub")?.Value
                   ?? Request.Headers["X-User-Id"].FirstOrDefault();
        }
    }
}

