namespace StockDataLibrary.Models
{
    using Microsoft.EntityFrameworkCore;
    using System;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    [Index(nameof(Symbol), IsUnique=true)]
    public class Stock
    {
        public int Id { get; set; }
        [Required]
        public string Symbol { get; set; }
        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public DateTime CreatedAt { get; set; }
    }
}
