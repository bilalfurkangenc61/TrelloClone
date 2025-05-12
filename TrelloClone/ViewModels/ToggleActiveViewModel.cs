using System;
using System.ComponentModel.DataAnnotations;

namespace TrelloClone.ViewModels
{
    public class ToggleActiveViewModel
    {
        public bool IsActive { get; set; }
    }

    public class CreateSprintViewModel
    {
        public int BoardId { get; set; }

        [Required(ErrorMessage = "Sprint adı gereklidir.")]
        [StringLength(100, ErrorMessage = "Sprint adı en fazla 100 karakter olabilir.")]
        public string Name { get; set; }

        public string Description { get; set; }

        [Required(ErrorMessage = "Başlangıç tarihi gereklidir.")]
        [DataType(DataType.Date)]
        [Display(Name = "Başlangıç Tarihi")]
        public DateTime StartDate { get; set; }

        [Required(ErrorMessage = "Bitiş tarihi gereklidir.")]
        [DataType(DataType.Date)]
        [Display(Name = "Bitiş Tarihi")]
        public DateTime EndDate { get; set; }

        public string Goal { get; set; }

        [Display(Name = "Aktif Sprint")]
        public bool IsActive { get; set; }
    }
}