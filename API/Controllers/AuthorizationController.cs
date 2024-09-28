using System.Security.Claims;
using API.Models.DB;
using API.Models.Requests;
using API.Models.Responce;
using API.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace API.Controllers;


[ApiController]
[Route("api/auth")]
public class AuthorizationController: ControllerBase
{
    private static List<Person> _clients = new();
    private static Dictionary<string, List<Models.DB.Security>> _sessions = new();

    [HttpPost("registration")]
    public async Task<IActionResult> RegistrationUser(RegisteredUser registeredUser)
    {
        Random rnd = new Random();
        int personId = rnd.Next(1111111, 9999999);
        var userIpAddress = HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault() ?? HttpContext.Connection.RemoteIpAddress?.ToString();
        
        var validationResponse = await UserValidator.ValidateUser(HttpContext, _clients, "registration", registeredUser.Name);
        
        if (validationResponse != null)
            return validationResponse;

        var security = new Models.DB.Security
        {
            UserAgent = HttpContext.Request.Headers["User-Agent"]!,
            LoginTime = DateTime.Now,
            PersonIpAdress = userIpAddress,
            LoginCountry = await DeterminingIPAddress.GetPositionUser(userIpAddress!),
            LoginDevice = HttpContext.Request.Headers["User-Agent"]!
        };
        var person = new Person
        {
            Id = personId.ToString(),
            Name = registeredUser.Name,
            Password = registeredUser.Password,
            Security = security
        };
        _clients.Add(person);
        
        var responceResult = new RegistrationRequests
        {
            Success = true,
            message = "Вы успешно создали аккаунт!",
        };

        return StatusCode(200, responceResult);
    }

    [HttpPost("authorization")]
    public async Task<IActionResult> Authorization(RegisteredUser authorizationUser)
    {
        var userIpAddress = HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault() ?? HttpContext.Connection.RemoteIpAddress?.ToString();

        IActionResult? validationResponse = await UserValidator.ValidateUser(HttpContext, _clients, "authorization", authorizationUser.Name);
        if (validationResponse != null)
            return validationResponse;

        var person = _clients.FirstOrDefault(p => p.Name == authorizationUser.Name && p.Password == authorizationUser.Password);

        if (person == null)
        {
            var responce = new RegistrationRequests
            {
                Success = false,
                message = "Пароль не верен",
                ErrorCode = 403.ToString(),
                Error = "Forbidden"
            };
            return StatusCode(403, responce);
        }

        var refreshToken = RefreshTokenService.GenerateRefreshToken(person.Id);
        
        var security = new Models.DB.Security
        {
            SessionId = new Random().Next(1111111, 9999999).ToString(),
            UserAgent = HttpContext.Request.Headers["User-Agent"]!,
            LoginTime = DateTime.Now,
            PersonIpAdress = userIpAddress,
            LoginCountry = await DeterminingIPAddress.GetPositionUser(userIpAddress!),
            LoginDevice = HttpContext.Request.Headers["User-Agent"]!,
            Token = refreshToken
        };
        
        var claims = new List<Claim> { new(ClaimTypes.Authentication, person.Id) };
        var identity = new ClaimsIdentity(claims);
        var principal = new ClaimsPrincipal(identity);
        var encodedJwt = JwtController.GenerateNewToken(principal);
        
        var responceResult = new RegistrationRequests
        {
            Success = true,
            message = "Вы успешно авторизовались",
            token_expires = JwtController.DurationExpires,
            access_token = encodedJwt,
            refresh_token = refreshToken
        };

        try
        {
            _sessions[person.Id].Add(security);
        }
        catch
        {
            _sessions[person.Id] = [ security ];
        }
        
        return StatusCode(200, responceResult);
    }
    
    [HttpPost("refresh-token")]
    public async Task<IActionResult> RefreshToken(RefreshData data)
    {
        var validationResponse = await UserValidator.ValidateUser(HttpContext, _clients);
        
        if (validationResponse != null)
            return validationResponse;
        
        try
        {
            ClaimsPrincipal principal = JwtController.GetPrincipalFromExpiredToken(data.access_token);
            Claim? authClaim = principal.FindFirst(ClaimTypes.Authentication);
            
            if (authClaim == null)
                throw new SecurityTokenException("Invalid token");
            
            if (!RefreshTokenService.ValidateRefreshToken(authClaim.Value, data.refresh_token))
                throw new SecurityTokenException("Invalid token");
            
            var newJwtToken = JwtController.GenerateNewToken(principal);
            
            var responceResult = new RegistrationRequests
            {
                Success = true,
                message = "токен успешно обновлен",
                access_token = newJwtToken
            };
            return StatusCode(200, responceResult);
        }
        catch
        {
            var responce = new RegistrationRequests
            {
                Success = false,
                message = "Invalid token",
                ErrorCode = 400.ToString(),
                Error = "Bad Request"
            };
            return StatusCode(400, responce);
        }
    }
}