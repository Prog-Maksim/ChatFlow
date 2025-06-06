using Microsoft.AspNetCore.Mvc;

namespace MessageService.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Produces("application/json")]
[Route("backend/v{version:apiVersion}/[controller]")]
public class MessageController: ControllerBase
{
    
}