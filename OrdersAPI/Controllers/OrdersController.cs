using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrdersAPI.DBcontext;
using OrdersAPI.Models;
using OrdersAPI.Models.DTO;
using System.Net;
using System.Net.Http.Json;

namespace OrdersAPI.Controllers
{
    [Route("api/OrdersAPI")]
    [ApiController]
    public class OrdersController : ControllerBase
    {
        private readonly OrdersDbContext _db;
        private readonly HttpClient _cartClient;

        public OrdersController(OrdersDbContext db, IHttpClientFactory httpClientFactory)
        {
            _db = db;
            _cartClient = httpClientFactory.CreateClient("CartAPI");
        }

        [HttpPost]
        public async Task<ActionResult<OrderResponseDTO>> Checkout([FromBody] OrderRequestDTO request)
        {
            using var cartResponse = await GetUserCartResponseAsync(request.UserId);
            if (cartResponse.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            {
                return StatusCode((int)cartResponse.StatusCode);
            }

            cartResponse.EnsureSuccessStatusCode();
            var cartItems = await cartResponse.Content.ReadFromJsonAsync<List<CartItemDTO>>() ?? [];
            if (cartItems.Count == 0)
            {
                return BadRequest("Cart is empty.");
            }

            using var clearResponse = await ClearCartItemsAsync(request.UserId, cartItems.Select(c => c.Id).ToList());
            if (clearResponse.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            {
                return StatusCode((int)clearResponse.StatusCode);
            }

            clearResponse.EnsureSuccessStatusCode();

            var order = new OrderModel
            {
                UserId = request.UserId,
                Address = request.Address,
                Description = request.Description,
                PaymentMode = request.PaymentMode!.Value,
                NumberOfItems = cartItems.Sum(c => c.Quantity),
                TotalPrice = cartItems.Sum(c => c.TotalPrice),
                Items = cartItems.Select(c => new OrderItemModel
                {
                    CartId = c.Id,
                    ProductId = c.ProductId,
                    Quantity = c.Quantity,
                    TotalPrice = c.TotalPrice
                }).ToList()
            };

            _db.Orders.Add(order);
            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = order.Id }, ToResponse(order));
        }

        [HttpGet("user/{userId}")]
        public async Task<ActionResult<IEnumerable<OrderResponseDTO>>> GetByUser(string userId)
        {
            var orders = await _db.Orders
                .AsNoTracking()
                .Include(o => o.Items)
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            return Ok(orders.Select(ToResponse));
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<OrderResponseDTO>> GetById(int id)
        {
            var order = await _db.Orders
                .AsNoTracking()
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == id);
            if (order == null)
            {
                return NotFound();
            }

            return Ok(ToResponse(order));
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var order = await _db.Orders.FirstOrDefaultAsync(o => o.Id == id);
            if (order == null)
            {
                return NotFound();
            }

            _db.Orders.Remove(order);
            await _db.SaveChangesAsync();

            return NoContent();
        }

        private async Task<HttpResponseMessage> GetUserCartResponseAsync(string userId)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"api/CartAPI/user/{Uri.EscapeDataString(userId)}");
            ForwardAuthorization(request);
            return await _cartClient.SendAsync(request);
        }

        private async Task<HttpResponseMessage> ClearCartItemsAsync(string userId, List<int> cartItemIds)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, $"api/CartAPI/user/{Uri.EscapeDataString(userId)}/checkout")
            {
                Content = JsonContent.Create(cartItemIds)
            };
            ForwardAuthorization(request);
            return await _cartClient.SendAsync(request);
        }

        private void ForwardAuthorization(HttpRequestMessage request)
        {
            if (Request.Headers.Authorization.Count > 0)
            {
                request.Headers.TryAddWithoutValidation("Authorization", Request.Headers.Authorization.ToString());
            }
        }

        private static OrderResponseDTO ToResponse(OrderModel order) => new()
        {
            Id = order.Id,
            UserId = order.UserId,
            Address = order.Address,
            Description = order.Description,
            PaymentMode = order.PaymentMode,
            NumberOfItems = order.NumberOfItems,
            TotalPrice = order.TotalPrice,
            CreatedAt = order.CreatedAt,
            Items = order.Items.Select(i => new OrderItemResponseDTO
            {
                Id = i.Id,
                CartId = i.CartId,
                ProductId = i.ProductId,
                Quantity = i.Quantity,
                TotalPrice = i.TotalPrice
            }).ToList()
        };
    }
}
