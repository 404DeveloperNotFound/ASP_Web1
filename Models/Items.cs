﻿using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using WebApplication1.Interfaces;

namespace WebApplication1.Models
{
    public class Items : IProduct
    {
        public int Id { get; set; }
        public string Name {get;set;}
        public double Price {get;set;}
        public string ImageUrl {get;set;}

        [Required] 
        public string SerialNumber { get; set; }

        public int Quantity { get; set; }
        
        [Timestamp]
        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        [ConcurrencyCheck] 
        public byte[] RowVersion { get; set; } // enables optimistic concurrency
        public int? CategoryId { get; set; }
        [ForeignKey("CategoryId")]
        public Category? Category { get; set; }

        public List<Client>? Clients { get; set; }
    }
}
