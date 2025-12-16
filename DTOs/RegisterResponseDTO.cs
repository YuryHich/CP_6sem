namespace LibraryManagement.DTOs;

public class RegisterResponseDTO
{
    public string Message { get; set; } = string.Empty;
    public bool RequiresEmailConfirmation { get; set; }
}

