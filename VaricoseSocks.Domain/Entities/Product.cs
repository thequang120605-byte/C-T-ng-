namespace VaricoseSocks.Domain.Entities;

public class Product
{
    public int Id { get; set; }
    public int CategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public decimal? DiscountPrice { get; set; }
    public int StockQuantity { get; set; }
    public string? ImageUrl { get; set; }
    public string? Size { get; set; }             // S, M, L, XL
    public string? Color { get; set; }            // Đen, Da, Xám
    public string? CompressionLevel { get; set; }  // 15-20 mmHg, 20-30 mmHg
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation property
    public Category? Category { get; set; }
}