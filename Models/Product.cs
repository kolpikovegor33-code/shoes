using System;
using System.Collections.Generic;

namespace shoes.Models;

public partial class Product
{
    public string Article { get; set; } = null!;

    public string ProductName { get; set; } = null!;

    public string Unit { get; set; } = null!;

    public decimal Price { get; set; }

    public int ProviderId { get; set; }

    public int ManufacturerId { get; set; }

    public int ProductCategoryId { get; set; }

    public int CurrentDiscount { get; set; }

    public short StockQuantity { get; set; }

    public string Description { get; set; } = null!;

    public string? Photo { get; set; }

    public virtual Manufacturer Manufacturer { get; set; } = null!;

    public virtual ICollection<OrderProduct> OrderProducts { get; set; } = new List<OrderProduct>();

    public virtual ProductCategory ProductCategory { get; set; } = null!;

    public virtual Provider Provider { get; set; } = null!;
}
