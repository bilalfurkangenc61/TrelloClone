// ViewModels/CreateBoardViewModel.cs
using System.ComponentModel.DataAnnotations;

namespace TrelloClone.ViewModels
{
    public class CreateBoardViewModel
    {
        [Required(ErrorMessage = "Başlık alanı zorunludur.")]
        [StringLength(100, ErrorMessage = "Başlık en fazla 100 karakter olabilir.")]
        [Display(Name = "Başlık")]
        public string Title { get; set; }

        [StringLength(500, ErrorMessage = "Açıklama en fazla 500 karakter olabilir.")]
        [Display(Name = "Açıklama")]
        public string Description { get; set; }

        [Display(Name = "Arka Plan Rengi")]
        public string BackgroundColor { get; set; } = "#0079BF"; // Varsayılan mavi renk

        [Display(Name = "Arka Plan Resmi")]
        public string BackgroundImage { get; set; } = ""; // Boş varsayılan değer

        [Display(Name = "Herkese Açık")]
        public bool IsPublic { get; set; } = false; // Varsayılan olarak özel
    }
}