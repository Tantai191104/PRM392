using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using ProductService.Application.Services;

namespace ProductService.Web.Controllers
{
    [ApiController]
    [Route("api/price-suggestion")]
    public class PriceSuggestionController : ControllerBase
    {
        private readonly IPriceSuggestionService _priceSuggestionService;

        public PriceSuggestionController(IPriceSuggestionService priceSuggestionService)
        {
            _priceSuggestionService = priceSuggestionService;
        }

        // POST: api/price-suggestion
        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> GetSuggestion([FromBody] ProductService.Application.DTOs.PriceSuggestionRequest request)
        {
            if (request == null)
                return BadRequest(new { success = false, message = "Missing body" });

            var apiKey = Environment.GetEnvironmentVariable("AI_API_KEY");
            try
            {
                var result = await _priceSuggestionService.GetPriceSuggestionAsync(request);
                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }
    }
}
