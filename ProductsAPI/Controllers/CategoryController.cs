using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProductsAPI.DBcontext;
using ProductsAPI.Models.Catalog;
using ProductsAPI.Models.DTO;

namespace ProductsAPI.Controllers
{
    [Route("api/CategoryAPI")]
    [ApiController]
    public class CategoryController : ControllerBase
    {
        private readonly CatalogDbContext _db;
        private readonly IMapper _mapper;

        public CategoryController(CatalogDbContext db, IMapper mapper)
        {
            _db = db;
            _mapper = mapper;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<CategoryResponseDTO>>> Get()
        {
            var categories = await _db.Categories.Where(c => c.IsActive).ToListAsync();
            return Ok(_mapper.Map<IEnumerable<CategoryResponseDTO>>(categories));
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<CategoryResponseDTO>> Get(int id)
        {
            var category = await _db.Categories.FirstOrDefaultAsync(c => c.Id == id && c.IsActive);
            if (category == null)
            {
                return NotFound();
            }

            return Ok(_mapper.Map<CategoryResponseDTO>(category));
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<CategoryResponseDTO>> Post([FromBody] CategoryDTO categoryDTO)
        {
            var category = _mapper.Map<Category>(categoryDTO);
            category.IsActive = true;
            category.CreatedAt = DateTime.UtcNow;
            category.UpdatedAt = category.CreatedAt;

            _db.Categories.Add(category);
            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(Get), new { id = category.Id },
                _mapper.Map<CategoryResponseDTO>(category));
        }

        [HttpPut("{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<CategoryResponseDTO>> Put(int id, [FromBody] CategoryDTO categoryDTO)
        {
            var category = await _db.Categories.FirstOrDefaultAsync(c => c.Id == id && c.IsActive);
            if (category == null)
            {
                return NotFound();
            }

            _mapper.Map(categoryDTO, category);
            category.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return Ok(_mapper.Map<CategoryResponseDTO>(category));
        }

        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var category = await _db.Categories.FirstOrDefaultAsync(c => c.Id == id);
            if (category == null)
            {
                return NotFound();
            }

            if (await _db.Products.AnyAsync(p => p.CategoryId == id))
            {
                return Conflict("Category has products and cannot be deleted.");
            }

            _db.Categories.Remove(category);
            await _db.SaveChangesAsync();

            return NoContent();
        }
    }
}
