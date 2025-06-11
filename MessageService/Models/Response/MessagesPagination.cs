using MessageService.Models.DB;

namespace MessageService.Models.Response;

public class MessagesPagination
{
    public required PaginationResult Pagination { get; set; }
    public required List<MessageData> Messages { get; set; }
}