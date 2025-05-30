using System.ComponentModel.DataAnnotations;

namespace TrelloClone.ViewModels
{
    public class EditBoardViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Pano adı gereklidir.")]
        [StringLength(200, ErrorMessage = "Pano adı en fazla 200 karakter olabilir.")]
        [Display(Name = "Pano Adı")]
        public string Name { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Açıklama en fazla 500 karakter olabilir.")]
        [Display(Name = "Açıklama")]
        public string? Description { get; set; }

        [Display(Name = "Arka Plan Rengi")]
        public string BackgroundColor { get; set; } = "#0079bf";

        // Sadece gösterim için
        public string TeamName { get; set; } = string.Empty;
    }
}