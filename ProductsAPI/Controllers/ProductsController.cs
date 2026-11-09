using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProductsAPI.DBcontext;
using ProductsAPI.Models.Catalog;
using ProductsAPI.Models.DTO;

namespace ProductsAPI.Controllers
{
    [Route("api/ProductsAPI")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private static readonly string[] AllowedImageExtensions = [".jpg", ".jpeg", ".png", ".webp", ".gif"];

        private readonly CatalogDbContext _db;
        private readonly IMapper _mapper;
        private readonly IWebHostEnvironment _env;

        public ProductsController(CatalogDbContext db, IMapper mapper, IWebHostEnvironment env)
        {
            _db = db;
            _mapper = mapper;
            _env = env;
        }

        [HttpPost("upload")]
        [Authorize(Roles = "Admin")]
        [RequestSizeLimit(5 * 1024 * 1024)]
        public async Task<IActionResult> Upload(IFormFile file)
        {
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (file.Length == 0 || !AllowedImageExtensions.Contains(extension))
            {
                return BadRequest("Upload a non-empty .jpg, .jpeg, .png, .webp or .gif file.");
            }

            var imagesDir = Path.Combine(_env.WebRootPath, "images");
            Directory.CreateDirectory(imagesDir);

            var fileName = $"{Guid.NewGuid()}{extension}";
            using (var stream = new FileStream(Path.Combine(imagesDir, fileName), FileMode.CreateNew))
            {
                await file.CopyToAsync(stream);
            }

            return Ok(new { imagePath = $"/images/{fileName}" });
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ProductResponseDTO>>> Get()
        {
            var products = await _db.Products
                .Include(p => p.Category)
                .Where(p => p.IsActive)
                .ToListAsync();
            return Ok(_mapper.Map<IEnumerable<ProductResponseDTO>>(products));
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<ProductResponseDTO>> Get(int id)
        {
            var product = await _db.Products
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.Id == id && p.IsActive);
            if (product == null)
            {
                return NotFound();
            }

            return Ok(_mapper.Map<ProductResponseDTO>(product));
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ProductResponseDTO>> Post([FromBody] ProductDTO productDTO)
        {
            var product = _mapper.Map<Product>(productDTO);
            product.IsActive = true;
            product.CreatedAt = DateTime.UtcNow;
            product.UpdatedAt = product.CreatedAt;

            _db.Products.Add(product);
            await _db.SaveChangesAsync();

            var saved = await _db.Products
                .AsNoTracking()
                .Include(p => p.Category)
                .FirstAsync(p => p.Id == product.Id);

            return CreatedAtAction(nameof(Get), new { id = saved.Id },
                _mapper.Map<ProductResponseDTO>(saved));
        }

        [HttpPut("{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ProductResponseDTO>> Put(int id, [FromBody] ProductDTO productDTO)
        {
            var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == id && p.IsActive);
            if (product == null)
            {
                return NotFound();
            }

            _mapper.Map(productDTO, product);
            product.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            var updated = await _db.Products
                .AsNoTracking()
                .Include(p => p.Category)
                .FirstAsync(p => p.Id == id);

            return Ok(_mapper.Map<ProductResponseDTO>(updated));
        }

        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == id && p.IsActive);
            if (product == null)
            {
                return NotFound();
            }

            product.IsActive = false;
            product.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return NoContent();
        }

        [HttpPost("{id:int}/reserve")]
        [Authorize]
        public async Task<IActionResult> Reserve(int id, [FromBody] int quantity)
        {
            if (quantity < 1)
            {
                return BadRequest("Quantity must be at least 1.");
            }

            var updated = await _db.Products
                .Where(p => p.Id == id && p.IsActive && p.Quantity >= quantity)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(p => p.Quantity, p => p.Quantity - quantity)
                    .SetProperty(p => p.UpdatedAt, DateTime.UtcNow));

            if (updated == 0)
            {
                var exists = await _db.Products.AnyAsync(p => p.Id == id && p.IsActive);
                return exists ? Conflict("Insufficient stock.") : NotFound();
            }

            return NoContent();
        }
    }
}
