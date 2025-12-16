namespace LibraryManagement.DTOs;

public class GlobalStatisticsDTO
{
    public int TotalBooks { get; set; }
    public int TotalLoans { get; set; }
    public int ActiveLoans { get; set; }
    public int OverdueLoans { get; set; }
    public int TotalUsers { get; set; }
    public int TotalAuthors { get; set; }
}

public class UserStatisticsDTO
{
    public int TotalLoans { get; set; }
    public int ActiveLoans { get; set; }
    public int OverdueLoans { get; set; }
    public List<GenreCountDTO> FavoriteGenres { get; set; } = new();
}

public class AuthorStatisticsDTO
{
    public string AuthorName { get; set; } = string.Empty;
    public int BookCount { get; set; }
    public int LoanCount { get; set; }
}

public class GenreStatisticsDTO
{
    public string GenreName { get; set; } = string.Empty;
    public int BookCount { get; set; }
    public int LoanCount { get; set; }
}

public class LanguageStatisticsDTO
{
    public string LanguageName { get; set; } = string.Empty;
    public int BookCount { get; set; }
    public int LoanCount { get; set; }
}

public class GenreCountDTO
{
    public string GenreName { get; set; } = string.Empty;
    public int Count { get; set; }
}

