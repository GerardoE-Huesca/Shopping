using Shooping.Common;
using Shopping.Data.Entities;

namespace Shopping.Models
{
    public class HomeViewModel
    {
        public PaginatedList<Product> Products { get; set; }
        public float Quantity { get; set; }
    }
}
