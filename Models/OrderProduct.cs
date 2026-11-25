using System;
using System.Collections.Generic;

namespace shoes.Models;

public partial class OrderProduct
{
    public int OrderArticleId { get; set; }

    public int OrderId { get; set; }

    public string Article { get; set; } = null!;

    public int Count { get; set; }

    public virtual Product ArticleNavigation { get; set; } = null!;

    public virtual Order Order { get; set; } = null!;
}
