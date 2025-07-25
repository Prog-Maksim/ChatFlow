using ChatFlow.Models.DB;

namespace ChatFlow.Models.Response;

public class MessagesPagination
{
    public required PaginationResult Pagination { get; set; }
    public required List<MessageData> Messages { get; set; }
}