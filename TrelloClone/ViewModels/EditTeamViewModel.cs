using System.ComponentModel.DataAnnotations;

namespace TrelloClone.ViewModels
{
    public class EditTeamViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Takım adı gereklidir.")]
        [StringLength(200, ErrorMessage = "Takım adı en fazla 200 karakter olabilir.")]
        [Display(Name = "Takım Adı")]
        public string Name { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Açıklama en fazla 500 karakter olabilir.")]
        [Display(Name = "Açıklama")]
        public string? Description { get; set; }
    }
}