using System.Collections.Generic;

namespace LibraryManagement.DTOs;

public class PagedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalCount { get; set; }
}


