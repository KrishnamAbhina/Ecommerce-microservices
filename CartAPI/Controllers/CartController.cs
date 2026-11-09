using CartAPI.DBcontext;
using CartAPI.MODELS.DB_MODELS;
using CartAPI.MODELS.DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;

namespace CartAPI.Controllers
{
    [Authorize]
    [Route("api/CartAPI")]
    [ApiController]
    public class CartController : ControllerBase
    {
        private readonly CartDbContext _db;
        private readonly HttpClient _productsClient;

        public CartController(CartDbContext db, IHttpClientFactory httpClientFactory)
        {
            _db = db;
            _productsClient = httpClientFactory.CreateClient("ProductsAPI");
        }

        private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        [HttpGet]
        public async Task<ActionResult<IEnumerable<CartResponseDTO>>> Get()
        {
            var carts = await _db.Carts
                .AsNoTracking()
                .Where(c => c.UserId == CurrentUserId && c.IsActive)
                .ToListAsync();
            return Ok(carts.Select(ToResponse));
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<CartResponseDTO>> Get(int id)
        {
            var cart = await _db.Carts
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == id && c.UserId == CurrentUserId);
            if (cart == null)
            {
                return NotFound();
            }

            return Ok(ToResponse(cart));
        }

        [HttpGet("user/{userId}")]
        public async Task<ActionResult<IEnumerable<CartResponseDTO>>> GetByUser(string userId)
        {
            if (userId != CurrentUserId)
            {
                return Forbid();
            }

            var carts = await _db.Carts
                .AsNoTracking()
                .Where(c => c.UserId == userId && c.IsActive)
                .ToListAsync();
            return Ok(carts.Select(ToResponse));
        }

        [HttpPost("user/{userId}/checkout")]
        public async Task<IActionResult> Checkout(string userId, [FromBody] List<int> cartItemIds)
        {
            if (userId != CurrentUserId)
            {
                return Forbid();
            }

            var carts = await _db.Carts
                .Where(c => c.UserId == userId && c.IsActive && cartItemIds.Contains(c.Id))
                .ToListAsync();

            foreach (var cart in carts)
            {
                cart.IsActive = false;
                cart.UpdatedAt = DateTime.UtcNow;
            }

            await _db.SaveChangesAsync();

            return NoContent();
        }

        [HttpPost]
        public async Task<ActionResult<CartResponseDTO>> Post([FromBody] CartDTO cartDTO)
        {
            var price = await GetProductPriceAsync(cartDTO.ProductId);
            if (price == null)
            {
                return NotFound("Product not found.");
            }

            using var reserveResponse = await ReserveStockAsync(cartDTO.ProductId, cartDTO.Quantity);
            if (reserveResponse.StatusCode == HttpStatusCode.Conflict)
            {
                return Conflict("Insufficient stock.");
            }
            if (reserveResponse.StatusCode == HttpStatusCode.NotFound)
            {
                return NotFound("Product not found.");
            }
            reserveResponse.EnsureSuccessStatusCode();

            var existing = await _db.Carts.FirstOrDefaultAsync(c =>
                c.UserId == CurrentUserId && c.IsActive && c.ProductId == cartDTO.ProductId);

            if (existing != null)
            {
                existing.Quantity += cartDTO.Quantity;
                existing.TotalPrice = price.Value * existing.Quantity;
                existing.UpdatedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();

                return Ok(ToResponse(existing));
            }

            var cart = new CartModel
            {
                UserId = CurrentUserId,
                ProductId = cartDTO.ProductId,
                Quantity = cartDTO.Quantity,
                TotalPrice = price.Value * cartDTO.Quantity
            };

            _db.Carts.Add(cart);
            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(Get), new { id = cart.Id }, ToResponse(cart));
        }

        [HttpPut("{id:int}")]
        public async Task<ActionResult<CartResponseDTO>> Put(int id, [FromBody] CartDTO cartDTO)
        {
            var cart = await _db.Carts.FirstOrDefaultAsync(c => c.Id == id && c.UserId == CurrentUserId);
            if (cart == null)
            {
                return NotFound();
            }

            var price = await GetProductPriceAsync(cartDTO.ProductId);
            if (price == null)
            {
                return NotFound("Product not found.");
            }

            cart.ProductId = cartDTO.ProductId;
            cart.Quantity = cartDTO.Quantity;
            cart.TotalPrice = price.Value * cartDTO.Quantity;
            cart.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return Ok(ToResponse(cart));
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var cart = await _db.Carts.FirstOrDefaultAsync(c => c.Id == id && c.UserId == CurrentUserId);
            if (cart == null)
            {
                return NotFound();
            }

            _db.Carts.Remove(cart);
            await _db.SaveChangesAsync();

            return NoContent();
        }

        private async Task<decimal?> GetProductPriceAsync(int productId)
        {
            using var response = await _productsClient.GetAsync($"api/ProductsAPI/{productId}");
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }

            response.EnsureSuccessStatusCode();
            var product = await response.Content.ReadFromJsonAsync<ProductLookupDTO>();
            return product?.Price;
        }

        private Task<HttpResponseMessage> ReserveStockAsync(int productId, int quantity)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, $"api/ProductsAPI/{productId}/reserve")
            {
                Content = JsonContent.Create(quantity)
            };
            request.Headers.TryAddWithoutValidation("Authorization", Request.Headers.Authorization.ToString());

            return _productsClient.SendAsync(request);
        }

        private static CartResponseDTO ToResponse(CartModel cart) => new()
        {
            Id = cart.Id,
            UserId = cart.UserId,
            ProductId = cart.ProductId,
            Quantity = cart.Quantity,
            TotalPrice = cart.TotalPrice
        };
    }
}
