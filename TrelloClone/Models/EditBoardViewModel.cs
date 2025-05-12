// ViewModels/BoardViewModels.cs
using System.ComponentModel.DataAnnotations;

namespace TrelloClone.ViewModels
{
    public class EditBoardViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Başlık alanı zorunludur.")]
        [StringLength(100, ErrorMessage = "Başlık en fazla 100 karakter olabilir.")]
        [Display(Name = "Başlık")]
        public string Title { get; set; }

        [StringLength(500, ErrorMessage = "Açıklama en fazla 500 karakter olabilir.")]
        [Display(Name = "Açıklama")]
        public string Description { get; set; }

        [Display(Name = "Arka Plan Rengi")]
        public string BackgroundColor { get; set; }

        [Display(Name = "Arka Plan Resmi")]
        public string BackgroundImage { get; set; }

        [Display(Name = "Herkese Açık")]
        public bool IsPublic { get; set; }
    }

    public class AddBoardMemberViewModel
    {
        [Required]
        public int BoardId { get; set; }

        [Required(ErrorMessage = "E-posta alanı zorunludur.")]
        [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi girin.")]
        public string Email { get; set; }
    }

    public class RemoveBoardMemberViewModel
    {
        [Required]
        public int BoardId { get; set; }

        [Required]
        public string UserId { get; set; }
    }

    public class AddBoardListViewModel
    {
        [Required]
        public int BoardId { get; set; }

        [Required(ErrorMessage = "Liste başlığı zorunludur.")]
        [StringLength(100, ErrorMessage = "Liste başlığı en fazla 100 karakter olabilir.")]
        public string Title { get; set; }
    }

    public class UpdateListPositionViewModel
    {
        [Required]
        public int BoardId { get; set; }

        [Required]
        public int ListId { get; set; }

        [Required]
        public int NewPosition { get; set; }
    }

    public class UpdateBoardListViewModel
    {
        [Required]
        public int ListId { get; set; }

        [Required(ErrorMessage = "Liste başlığı zorunludur.")]
        [StringLength(100, ErrorMessage = "Liste başlığı en fazla 100 karakter olabilir.")]
        public string Title { get; set; }
    }

    public class DeleteBoardListViewModel
    {
        [Required]
        public int ListId { get; set; }
    }
}