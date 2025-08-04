using ChatFlow.Models.DB.Other;

namespace ChatFlow.Models.Response;

public class MessagesPagination
{
    public required PaginationResult Pagination { get; set; }
    public required List<MessageData> Messages { get; set; }
}