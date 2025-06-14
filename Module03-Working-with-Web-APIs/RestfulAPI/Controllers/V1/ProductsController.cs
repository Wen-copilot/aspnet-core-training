using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestfulAPI.Data;
using RestfulAPI.DTOs;
using RestfulAPI.Models;
using RestfulAPI.Models.DTOs;
using Asp.Versioning;

namespace RestfulAPI.Controllers.V1
{
    [ApiController]
    [Route("api/v{version:apiVersion}/[controller]")]
    [Produces("application/json")]
    [ApiVersion("1.0")]
    [Authorize]
    public class ProductsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ProductsController> _logger;

        public ProductsController(ApplicationDbContext context, ILogger<ProductsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Get all products with optional filtering
        /// </summary>
        /// <param name="category">Filter by category name</param>
        /// <param name="name">Filter by product name</param>
        /// <param name="minPrice">Minimum price filter</param>
        /// <param name="maxPrice">Maximum price filter</param>
        /// <returns>List of products</returns>
        [HttpGet]
        [AllowAnonymous] // Override controller-level authorization
        [ProducesResponseType(typeof(IEnumerable<BookDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<ProductDto>>> GetProducts(
            [FromQuery] string? category = null,
            [FromQuery] string? name = null,
            [FromQuery] decimal? minPrice = null,
            [FromQuery] decimal? maxPrice = null)
        {
            _logger.LogInformation("Getting products with filters - Category: {Category}, Name: {Name}, MinPrice: {MinPrice}, MaxPrice: {MaxPrice}",
                category, name, minPrice, maxPrice);

            var query = _context.Products.AsQueryable();

            if (!string.IsNullOrWhiteSpace(category))
            {
                query = query.Where(p => p.Category.Contains(category));
            }

            if (!string.IsNullOrWhiteSpace(name))
            {
                query = query.Where(p => p.Name.Contains(name));
            }

            if (minPrice.HasValue)
            {
                query = query.Where(p => p.Price >= minPrice.Value);
            }

            if (maxPrice.HasValue)
            {
                query = query.Where(p => p.Price <= maxPrice.Value);
            }

            var products = await query
                .Select(p => new ProductDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Description = p.Description,
                    Price = p.Price,
                    Category = p.Category,
                    StockQuantity = p.StockQuantity,
                    Sku = p.Sku,
                    IsActive = p.IsActive,
                    IsAvailable = p.IsAvailable,
                    CreatedAt = p.CreatedAt,
                    UpdatedAt = p.UpdatedAt
                })
                .ToListAsync();

            return Ok(products);
        }

        /// <summary>
        /// Get a specific product by ID
        /// </summary>
        /// <param name="id">Product ID</param>
        /// <returns>Product details</returns>
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ProductDto>> GetProduct(int id)
        {
            _logger.LogInformation("Getting product with ID: {ProductId}", id);

            var product = await _context.Products
                .Where(p => p.Id == id)
                .Select(p => new ProductDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Description = p.Description,
                    Price = p.Price,
                    Category = p.Category,
                    StockQuantity = p.StockQuantity,
                    Sku = p.Sku,
                    IsActive = p.IsActive,
                    IsAvailable = p.IsAvailable,
                    CreatedAt = p.CreatedAt,
                    UpdatedAt = p.UpdatedAt
                })
                .FirstOrDefaultAsync();

            if (product == null)
            {
                _logger.LogWarning("Product with ID {ProductId} not found", id);
                return NotFound(new { message = $"Product with ID {id} not found" });
            }

            return Ok(product);
        }

        /// <summary>
        /// Create a new product
        /// </summary>
        /// <param name="createProductDto">Product creation data</param>
        /// <returns>Created product</returns>
        [HttpPost]
        [Authorize(Policy = "RequireLibrarianRole")]
        [ProducesResponseType(typeof(BookDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<ProductDto>> CreateProduct([FromBody] CreateProductDto createProductDto)
        {
            _logger.LogInformation("Creating new product: {ProductName}", createProductDto.Name);

            // Check for duplicate SKU
            var skuExists = await _context.Products.AnyAsync(p => p.Sku == createProductDto.Sku);
            if (skuExists)
            {
                return BadRequest(new { message = $"Product with SKU {createProductDto.Sku} already exists" });
            }

            var product = new Product
            {
                Name = createProductDto.Name,
                Description = createProductDto.Description,
                Price = createProductDto.Price,
                Category = createProductDto.Category,
                StockQuantity = createProductDto.StockQuantity,
                Sku = createProductDto.Sku,
                IsActive = createProductDto.IsActive ?? true,
                IsAvailable = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            var productDto = new ProductDto
            {
                Id = product.Id,
                Name = product.Name,
                Description = product.Description,
                Price = product.Price,
                Category = product.Category,
                StockQuantity = product.StockQuantity,
                Sku = product.Sku,
                IsActive = product.IsActive,
                IsAvailable = product.IsAvailable,
                CreatedAt = product.CreatedAt,
                UpdatedAt = product.UpdatedAt
            };

            return CreatedAtAction(
                nameof(GetProduct),
                new { id = product.Id },
                productDto);
        }

        /// <summary>
        /// Update an existing product
        /// </summary>
        /// <param name="id">Product ID</param>
        /// <param name="updateProductDto">Updated product data</param>
        /// <returns>No content</returns>
        [HttpPut("{id:int}")]
        [Authorize(Policy = "RequireLibrarianRole")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> UpdateProduct(int id, [FromBody] UpdateProductDto updateProductDto)
        {
            _logger.LogInformation("Updating product with ID: {ProductId}", id);

            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound(new { message = $"Product with ID {id} not found" });
            }

            // Check for duplicate SKU (if changed)
            if (!string.IsNullOrEmpty(updateProductDto.Sku) && product.Sku != updateProductDto.Sku)
            {
                var skuExists = await _context.Products.AnyAsync(p => p.Sku == updateProductDto.Sku && p.Id != id);
                if (skuExists)
                {
                    return BadRequest(new { message = $"Product with SKU {updateProductDto.Sku} already exists" });
                }
            }

            product.Name = updateProductDto.Name;
            product.Description = updateProductDto.Description;
            product.Price = updateProductDto.Price;
            product.Category = updateProductDto.Category;
            product.StockQuantity = updateProductDto.StockQuantity;

            if (!string.IsNullOrEmpty(updateProductDto.Sku))
            {
                product.Sku = updateProductDto.Sku;
            }

            if (updateProductDto.IsActive.HasValue)
            {
                product.IsActive = updateProductDto.IsActive.Value;
            }

            if (updateProductDto.IsAvailable.HasValue)
            {
                product.IsAvailable = updateProductDto.IsAvailable.Value;
            }

            product.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        /// <summary>
        /// Delete a product
        /// </summary>
        /// <param name="id">Product ID</param>
        /// <returns>No content</returns>
        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            _logger.LogInformation("Deleting product with ID: {ProductId}", id);

            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound(new { message = $"Product with ID {id} not found" });
            }

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        /// <summary>
        /// Get products by category
        /// </summary>
        /// <param name="category">Category name</param>
        /// <returns>List of products in the category</returns>
        [HttpGet("by-category/{category}")]
        [ProducesResponseType(typeof(IEnumerable<ProductDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<ProductDto>>> GetProductsByCategory(string category)
        {
            var products = await _context.Products
                .Where(p => p.Category.Contains(category))
                .Select(p => new ProductDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Description = p.Description,
                    Price = p.Price,
                    Category = p.Category,
                    StockQuantity = p.StockQuantity,
                    Sku = p.Sku,
                    IsActive = p.IsActive,
                    IsAvailable = p.IsAvailable,
                    CreatedAt = p.CreatedAt,
                    UpdatedAt = p.UpdatedAt
                })
                .ToListAsync();

            return Ok(products);
        }
    }
}