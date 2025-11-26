namespace FBMMultiMessenger.Models
{
    public class SweetAlertOptions
    {
        public string Title { get; set; } = "Error";
        public string Message { get; set; } = "Something went wrong.";
        public string Icon { get; set; } = "error"; // "success", "error", "warning", "info", "question"
        public string ConfirmButtonText { get; set; } = "Yes";
        public bool ShowCancelButton { get; set; } = false;
        public string CancelButtonText { get; set; } = "No";
        public SweetAlertFooter? Footer { get; set; }
    }

    public class SweetAlertFooter
    {
        public string? Text { get; set; }
        public string? Link { get; set; }
    }
}
