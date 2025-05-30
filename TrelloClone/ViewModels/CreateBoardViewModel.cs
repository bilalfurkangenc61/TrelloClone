using System.ComponentModel.DataAnnotations;
using TrelloClone.Models;

namespace TrelloClone.ViewModels
{
    public class CreateBoardViewModel
    {
        [Required(ErrorMessage = "Pano adı gereklidir.")]
        [StringLength(200, ErrorMessage = "Pano adı en fazla 200 karakter olabilir.")]
        [Display(Name = "Pano Adı")]
        public string Name { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Açıklama en fazla 500 karakter olabilir.")]
        [Display(Name = "Açıklama")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Takım seçimi gereklidir.")]
        [Display(Name = "Takım")]
        public int? TeamId { get; set; }

        [Display(Name = "Arka Plan Rengi")]
        public string BackgroundColor { get; set; } = "#0079bf";

        // Dropdown için
        public List<Team> AvailableTeams { get; set; } = new List<Team>();
    }
}