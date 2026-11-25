using System;
using System.Collections.Generic;

namespace shoes.Models;

public partial class Provider
{
    public int ProviderId { get; set; }

    public string Provider1 { get; set; } = null!;

    public virtual ICollection<Product> Products { get; set; } = new List<Product>();
}
