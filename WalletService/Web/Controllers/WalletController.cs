using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WalletService.Application.DTOs;
using WalletService.Application.Services;
using WalletService.Domain.Entities;

namespace WalletService.Web.Controllers
{
    [ApiController]
    [Route("api/wallets")]
    public class WalletController : ControllerBase
    {
        private readonly WalletAppService _walletService;

        public WalletController(WalletAppService walletService)
        {
            _walletService = walletService;
        }

        /// <summary>
        /// Get wallet by user ID
        /// </summary>
        [HttpGet("users/{userId}")]
        [ProducesResponseType(typeof(Wallet), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetWalletByUserId(string userId)
        {
            var wallet = await _walletService.GetWalletByUserIdAsync(userId);
            if (wallet == null)
                return NotFound(new { message = $"Wallet not found for user {userId}" });

            return Ok(wallet);
        }

        /// <summary>
        /// Create a new wallet
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(Wallet), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateWallet([FromBody] WalletDTO walletDto)
        {
            if (walletDto == null || string.IsNullOrEmpty(walletDto.UserId))
                return BadRequest(new { message = "Invalid wallet data" });

            var wallet = new Wallet
            {
                UserId = walletDto.UserId,
                Balance = 0
            };

            await _walletService.CreateWalletAsync(wallet);
            return CreatedAtAction(nameof(GetWalletByUserId), new { userId = wallet.UserId }, wallet);
        }

        /// <summary>
        /// Update wallet balance
        /// </summary>
        [HttpPut("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateWallet(string id, [FromBody] WalletDTO walletDto)
        {
            if (walletDto == null || string.IsNullOrEmpty(walletDto.UserId))
                return BadRequest(new { message = "Invalid wallet data" });

            var wallet = new Wallet
            {
                Id = id,
                UserId = walletDto.UserId,
                Balance = walletDto.Balance
            };

            await _walletService.UpdateWalletAsync(wallet);
            return NoContent();
        }

        /// <summary>
        /// Transfer money from one user to another (for escrow release)
        /// </summary>
        [HttpPost("transfer")]
        public async Task<IActionResult> Transfer([FromBody] TransferRequestDto dto)
        {
            if (dto == null || string.IsNullOrEmpty(dto.FromUserId) ||
                string.IsNullOrEmpty(dto.ToUserId) || dto.Amount <= 0)
                return BadRequest(new { message = "Invalid transfer data" });

            var result = await _walletService.TransferAsync(dto.FromUserId, dto.ToUserId, dto.Amount);
            if (!result)
                return BadRequest(new { message = "Transfer failed" });

            return Ok(new { message = "Transfer successful" });
        }

        /// <summary>
        /// Release money to user (for escrow refund)
        /// </summary>
        [HttpPost("release")]
        public async Task<IActionResult> Release([FromBody] ReleaseRequestDto dto)
        {
            if (dto == null || string.IsNullOrEmpty(dto.UserId) || dto.Amount <= 0)
                return BadRequest(new { message = "Invalid release data" });

            var result = await _walletService.ReleaseAsync(dto.UserId, dto.Amount);
            if (!result)
                return BadRequest(new { message = "Release failed" });

            return Ok(new { message = "Release successful" });
        }
    }
}
