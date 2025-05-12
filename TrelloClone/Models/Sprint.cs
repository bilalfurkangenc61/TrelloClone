// Models/Sprint.cs
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
namespace TrelloClone.Models
{
    public class Sprint
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        [Display(Name = "Sprint Adı")]
        public string Name { get; set; }

        [Display(Name = "Açıklama")]
        public string Description { get; set; }

        [Required]
        [Display(Name = "Başlangıç Tarihi")]
        public DateTime StartDate { get; set; }

        [Required]
        [Display(Name = "Bitiş Tarihi")]
        public DateTime EndDate { get; set; }

        [Display(Name = "Aktif Sprint")]
        public bool IsActive { get; set; }

        [Display(Name = "Tamamlandı")]
        public bool IsCompleted { get; set; }

        // BoardId'yi nullable yap
        public int? BoardId { get; set; }
        public virtual Board Board { get; set; }

        // Bir sprint birden fazla karta sahip olabilir
        public virtual ICollection<Card> Cards { get; set; } = new List<Card>();

        [Display(Name = "Sprint Hedefi")]
        public string Goal { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }
    }
}