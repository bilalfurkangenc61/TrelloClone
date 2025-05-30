using System.ComponentModel.DataAnnotations;
using TrelloClone.Models;

namespace TrelloClone.ViewModels
{
    // Sprint oluşturma view model'i
    public class CreateSprintViewModel
    {
        [Required(ErrorMessage = "Sprint adı gereklidir.")]
        [StringLength(200, ErrorMessage = "Sprint adı en fazla 200 karakter olabilir.")]
        [Display(Name = "Sprint Adı")]
        public string Name { get; set; } = string.Empty;

        [StringLength(1000, ErrorMessage = "Açıklama en fazla 1000 karakter olabilir.")]
        [Display(Name = "Açıklama")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Başlangıç tarihi gereklidir.")]
        [Display(Name = "Başlangıç Tarihi")]
        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; } = DateTime.Today;

        [Required(ErrorMessage = "Bitiş tarihi gereklidir.")]
        [Display(Name = "Bitiş Tarihi")]
        [DataType(DataType.Date)]
        public DateTime EndDate { get; set; } = DateTime.Today.AddDays(14);

        // Hidden alanlar
        public int BoardId { get; set; }
        public string BoardName { get; set; } = string.Empty;

        // Özel validation
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (EndDate <= StartDate)
            {
                yield return new ValidationResult(
                    "Bitiş tarihi başlangıç tarihinden sonra olmalıdır.",
                    new[] { nameof(EndDate) });
            }

            if (StartDate < DateTime.Today)
            {
                yield return new ValidationResult(
                    "Başlangıç tarihi bugünden önce olamaz.",
                    new[] { nameof(StartDate) });
            }

            var duration = EndDate - StartDate;
            if (duration.TotalDays > 30)
            {
                yield return new ValidationResult(
                    "Sprint süresi 30 günden fazla olamaz.",
                    new[] { nameof(EndDate) });
            }
        }
    }

    // Sprint düzenleme view model'i
    public class EditSprintViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Sprint adı gereklidir.")]
        [StringLength(200, ErrorMessage = "Sprint adı en fazla 200 karakter olabilir.")]
        [Display(Name = "Sprint Adı")]
        public string Name { get; set; } = string.Empty;

        [StringLength(1000, ErrorMessage = "Açıklama en fazla 1000 karakter olabilir.")]
        [Display(Name = "Açıklama")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Başlangıç tarihi gereklidir.")]
        [Display(Name = "Başlangıç Tarihi")]
        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; }

        [Required(ErrorMessage = "Bitiş tarihi gereklidir.")]
        [Display(Name = "Bitiş Tarihi")]
        [DataType(DataType.Date)]
        public DateTime EndDate { get; set; }

        [Display(Name = "Sprint Durumu")]
        public SprintStatus Status { get; set; }

        // Hidden alanlar
        public int BoardId { get; set; }
        public string BoardName { get; set; } = string.Empty;
    }

    // Sprint özet bilgileri için view model
    public class SprintSummaryViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public SprintStatus Status { get; set; }
        public string CreatedByUserName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }

        // İstatistikler
        public int TotalCards { get; set; }
        public int CompletedCards { get; set; }
        public int InProgressCards { get; set; }
        public int TodoCards { get; set; }

        // Hesaplanan alanlar
        public int DaysRemaining
        {
            get
            {
                if (Status == SprintStatus.Completed || Status == SprintStatus.Cancelled)
                    return 0;

                var remaining = (EndDate - DateTime.Today).Days;
                return Math.Max(0, remaining);
            }
        }

        public int TotalDays
        {
            get { return (EndDate - StartDate).Days; }
        }

        public double CompletionPercentage
        {
            get
            {
                if (TotalCards == 0) return 0;
                return Math.Round((double)CompletedCards / TotalCards * 100, 1);
            }
        }

        public double ProgressPercentage
        {
            get
            {
                if (TotalDays == 0) return 0;
                var daysElapsed = Math.Max(0, (DateTime.Today - StartDate).Days);
                return Math.Round((double)daysElapsed / TotalDays * 100, 1);
            }
        }

        public string StatusDisplayName
        {
            get
            {
                return Status switch
                {
                    SprintStatus.Planning => "Planlama",
                    SprintStatus.Active => "Aktif",
                    SprintStatus.Completed => "Tamamlandı",
                    SprintStatus.Cancelled => "İptal Edildi",
                    _ => "Bilinmeyen"
                };
            }
        }

        public string StatusBadgeClass
        {
            get
            {
                return Status switch
                {
                    SprintStatus.Planning => "bg-warning",
                    SprintStatus.Active => "bg-success",
                    SprintStatus.Completed => "bg-primary",
                    SprintStatus.Cancelled => "bg-danger",
                    _ => "bg-secondary"
                };
            }
        }
    }

    // Sprint kart atama için view model
    public class SprintCardAssignmentViewModel
    {
        public int SprintId { get; set; }
        public string SprintName { get; set; } = string.Empty;
        public SprintStatus SprintStatus { get; set; }

        // Mevcut atanan kartlar
        public List<SprintCardViewModel> AssignedCards { get; set; } = new();

        // Atanabilir kartlar (sprint'e atanmamış kartlar)
        public List<SprintCardViewModel> AvailableCards { get; set; } = new();
    }

    // Sprint'te bulunan kart bilgileri
    public class SprintCardViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string ListName { get; set; } = string.Empty;
        public string ListColor { get; set; } = "#f4f5f7";
        public DateTime? DueDate { get; set; }
        public bool HasDescription { get; set; }
        public int CommentsCount { get; set; }
        public int ChecklistItemsCount { get; set; }
        public int CompletedChecklistItemsCount { get; set; }

        // Atanan kişiler
        public List<AssignedUserViewModel> AssignedUsers { get; set; } = new();

        // Hesaplanan alanlar
        public bool IsOverdue
        {
            get { return DueDate.HasValue && DueDate.Value < DateTime.Today; }
        }

        public string DueDateDisplayClass
        {
            get
            {
                if (!DueDate.HasValue) return "";
                if (IsOverdue) return "text-danger";
                if (DueDate.Value <= DateTime.Today.AddDays(3)) return "text-warning";
                return "text-muted";
            }
        }
    }

    // Atanan kullanıcı bilgileri
    public class AssignedUserViewModel
    {
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string UserInitial { get; set; } = string.Empty;
        public string? ProfilePicture { get; set; }
    }

    // Sprint burndown chart için veri
    public class SprintBurndownViewModel
    {
        public int SprintId { get; set; }
        public string SprintName { get; set; } = string.Empty;
        public List<BurndownDataPoint> DataPoints { get; set; } = new();
        public int TotalStoryPoints { get; set; }
        public int IdealBurndownSlope { get; set; }
    }

    public class BurndownDataPoint
    {
        public DateTime Date { get; set; }
        public int RemainingWork { get; set; }
        public int IdealRemaining { get; set; }
    }
}