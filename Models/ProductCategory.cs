using System;
using System.Collections.Generic;

namespace shoes.Models;

public partial class ProductCategory
{
    public int ProductCategoryId { get; set; }

    public string ProductCategory1 { get; set; } = null!;

    public virtual ICollection<Product> Products { get; set; } = new List<Product>();
}
