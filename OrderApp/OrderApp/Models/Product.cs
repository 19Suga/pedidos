using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OrderApp.Models
{
    public class Product
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre es requerido")]
        [StringLength(100, ErrorMessage = "El nombre no puede exceder 100 caracteres")]
        [Display(Name = "Nombre del Producto")]
        public string Name { get; set; }

        [Display(Name = "Descripción")]
        [StringLength(500, ErrorMessage = "La descripción no puede exceder 500 caracteres")]
        public string Description { get; set; }

        [Required(ErrorMessage = "El precio es requerido")]
        [Range(0.01, double.MaxValue, ErrorMessage = "El precio debe ser mayor a 0")]
        [DataType(DataType.Currency)]
        [Display(Name = "Precio")]
        public decimal Price { get; set; }

        [Required(ErrorMessage = "El stock es requerido")]
        [Range(0, int.MaxValue, ErrorMessage = "El stock no puede ser negativo")]
        [Display(Name = "Stock Disponible")]
        public int Stock { get; set; }

        [Display(Name = "Categoría")]
        [StringLength(50, ErrorMessage = "La categoría no puede exceder 50 caracteres")]
        public string Category { get; set; }

        [Display(Name = "Fecha de Creación")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Display(Name = "Última Actualización")]
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        [Display(Name = "¿Está activo?")]
        public bool IsActive { get; set; } = true;

        // Relación con OrderItems (una producto puede estar en muchos items de pedido)
        //public ICollection<OrderItem> OrderItems { get; set; }
    }
}