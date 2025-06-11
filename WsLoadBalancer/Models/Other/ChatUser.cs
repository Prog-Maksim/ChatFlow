using WsLoadBalancer.Enums;

namespace WsLoadBalancer.Models.Other;

public class ChatUser
{
    /// <summary>
    /// Идентификатор пользователя
    /// </summary>
    public required string PersonId { get; set; }
    
    /// <summary>
    /// Роль пользователя
    /// </summary>
    public Roles Role { get; set; } = Roles.User;
}