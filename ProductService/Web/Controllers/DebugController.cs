using Microsoft.AspNetCore.Mvc;

namespace ProductService.Web.Controllers
{
    [ApiController]
    [Route("api/products/debug")]
    public class DebugController : ControllerBase
    {
        [HttpGet("headers")]
        public IActionResult Headers()
        {
            var dict = new Dictionary<string, string?>();
            foreach (var h in Request.Headers)
            {
                dict[h.Key] = h.Value.ToString();
            }
            return Ok(dict);
        }
    }
}
