// ViewModels/CardViewModels.cs
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using TrelloClone.Models;

namespace TrelloClone.ViewModels
{
    public class CardDetailsViewModel
    {
        public Card Card { get; set; }
        public List<Label> BoardLabels { get; set; }
        public List<ApplicationUser> AvailableMembers { get; set; }
    }

    public class CreateCardViewModel
    {
        [Required]
        public int BoardListId { get; set; }

        [Required(ErrorMessage = "Kart başlığı zorunludur.")]
        [StringLength(200, ErrorMessage = "Kart başlığı en fazla 200 karakter olabilir.")]
        public string Title { get; set; }

        public string Description { get; set; }
    }

    public class UpdateCardViewModel
    {
        [Required]
        public int Id { get; set; }

        [Required(ErrorMessage = "Kart başlığı zorunludur.")]
        [StringLength(200, ErrorMessage = "Kart başlığı en fazla 200 karakter olabilir.")]
        public string Title { get; set; }

        public string Description { get; set; }

        public string? CoverColor { get; set; }

        public DateTime? DueDate { get; set; }
    }

    public class DeleteCardViewModel
    {
        [Required]
        public int CardId { get; set; }
    }

    public class MoveCardViewModel
    {
        [Required]
        public int CardId { get; set; }

        [Required]
        public int TargetListId { get; set; }

        [Required]
        public int NewPosition { get; set; }
    }

    public class AssignCardMemberViewModel
    {
        [Required]
        public int CardId { get; set; }

        [Required]
        public string UserId { get; set; }
    }

    public class RemoveCardMemberViewModel
    {
        [Required]
        public int CardId { get; set; }

        [Required]
        public string UserId { get; set; }
    }

    public class AddCardLabelViewModel
    {
        [Required]
        public int CardId { get; set; }

        [Required]
        public int LabelId { get; set; }
    }

    public class RemoveCardLabelViewModel
    {
        [Required]
        public int CardId { get; set; }

        [Required]
        public int LabelId { get; set; }
    }

    public class AddCardCommentViewModel
    {
        [Required]
        public int CardId { get; set; }

        [Required(ErrorMessage = "Yorum içeriği zorunludur.")]
        [StringLength(1000, ErrorMessage = "Yorum en fazla 1000 karakter olabilir.")]
        public string Content { get; set; }
    }

    public class DeleteCardCommentViewModel
    {
        [Required]
        public int CommentId { get; set; }
    }

    public class AddCardChecklistViewModel
    {
        [Required]
        public int CardId { get; set; }

        [Required(ErrorMessage = "Kontrol listesi başlığı zorunludur.")]
        [StringLength(100, ErrorMessage = "Kontrol listesi başlığı en fazla 100 karakter olabilir.")]
        public string Title { get; set; }
    }

    public class DeleteCardChecklistViewModel
    {
        [Required]
        public int ChecklistId { get; set; }
    }

    public class AddChecklistItemViewModel
    {
        [Required]
        public int ChecklistId { get; set; }

        [Required(ErrorMessage = "Madde içeriği zorunludur.")]
        [StringLength(200, ErrorMessage = "Madde içeriği en fazla 200 karakter olabilir.")]
        public string Content { get; set; }
    }

    public class UpdateChecklistItemViewModel
    {
        [Required]
        public int ItemId { get; set; }

        [Required(ErrorMessage = "Madde içeriği zorunludur.")]
        [StringLength(200, ErrorMessage = "Madde içeriği en fazla 200 karakter olabilir.")]
        public string Content { get; set; }

        public bool IsCompleted { get; set; }
    }

    public class DeleteChecklistItemViewModel
    {
        [Required]
        public int ItemId { get; set; }
    }
}