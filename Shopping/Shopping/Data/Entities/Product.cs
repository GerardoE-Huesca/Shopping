using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Shopping.Data.Entities
{
	public class Product
    {
        public int Id { get; set; }

        [Display(Name = "Nombre")]
        [MaxLength(50, ErrorMessage = "El campo {0} debe tener máximo {1} caractéres.")]
        [Required(ErrorMessage = "El campo {0} es obligatorio.")]
        public string Name { get; set; }

        [DataType(DataType.MultilineText)]
        [Display(Name = "Descripción")]
        [MaxLength(500, ErrorMessage = "El campo {0} debe tener máximo {1} caractéres.")]
        public string Description { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [DisplayFormat(DataFormatString = "{0:C2}")]
        [Display(Name = "Precio")]
        [Required(ErrorMessage = "El campo {0} es obligatorio.")]
        public decimal Price { get; set; }

        [DisplayFormat(DataFormatString = "{0:N2}")]
        [Display(Name = "Inventario")]
        [Required(ErrorMessage = "El campo {0} es obligatorio.")]
        public float Stock { get; set; }

        public ICollection<ProductCategory> ProductCategories { get; set; } = new List<ProductCategory>();

        [Display(Name = "Categorías")]
        public int CategoriesNumber => ProductCategories?.Count ?? 0;

        public ICollection<ProductImage> ProductImages { get; set; } = new List<ProductImage>();

        [Display(Name = "Fotos")]
        public int ImagesNumber => ProductImages?.Count ?? 0;

        [Display(Name = "Foto")]
        public string ImageFullPath => ProductImages == null || !ProductImages.Any()
            ? "https://localhost:7016/images/noimage.png"
            : ProductImages.FirstOrDefault().ImageFullPath;
    }
}
