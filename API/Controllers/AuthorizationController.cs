using System.Security.Claims;
using API.Models.DB;
using API.Models.Requests;
using API.Models.Responce;
using API.Scripts;
using API.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthorizationController(ApplicationContext context): ControllerBase
{
    private readonly PasswordHasher<Person> _passwordHasher = new();
    
    [HttpPost("registration")]
    public async Task<IActionResult> RegistrationUser(RegisteredUser registeredUser)
    {
        string personId = UserIdentifierGenerator.GenerateUid();
        
        var userIpAddress = HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault() ?? HttpContext.Connection.RemoteIpAddress?.ToString();
        var validationResponse = await UserValidator.ValidateUser(HttpContext, context, "registration", registeredUser.Name);
        
        if (validationResponse != null)
            return validationResponse;

        string tag = "@" + registeredUser.Name.Replace(" ", "");
        
        var person = new Person
        {
            PersonId = personId,
            Name = registeredUser.Name,
            Tag = tag,
            RegistrationTime = DateTime.Now,
            Country = await DeterminingIPAddress.GetPositionUser(userIpAddress!)
        };
        person.Password = _passwordHasher.HashPassword(person, registeredUser.Password);
        
        await context.person.AddAsync(person);
        await context.SaveChangesAsync();
        
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

        IActionResult? validationResponse = await UserValidator.ValidateUser(HttpContext, context, "authorization", authorizationUser.Name);
        if (validationResponse != null)
            return validationResponse;
        
        var person = context.person.FirstOrDefault(p => p.Name == authorizationUser.Name);
        
        if (person == null)
        {
            var responce = new RegistrationRequests
            {
                Success = false,
                message = "Данный пользователь не найден",
                ErrorCode = 404.ToString(),
                Error = "Not Found"
            };
            return StatusCode(404, responce);
        }
        
        var result = _passwordHasher.VerifyHashedPassword(person, person.Password, authorizationUser.Password);
        if (result != PasswordVerificationResult.Success)
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

        var refreshToken = RefreshTokenService.GenerateRefreshToken();

        var security = new Session
        {
            IpAdress = userIpAddress!,
            PersonId = person.PersonId,
            LoginCountry = await DeterminingIPAddress.GetPositionUser(userIpAddress!),
            LoginDevice = HttpContext.Request.Headers["User-Agent"]!,
            LoginTime = DateTime.Now,
            SessionId = UserIdentifierGenerator.GenerateUid(),
            SessionToken = refreshToken
        };
        
        var claims = new List<Claim> { new(ClaimTypes.Authentication, person.PersonId) };
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

        await context.session.AddAsync(security);
        await context.SaveChangesAsync();
        
        return StatusCode(200, responceResult);
    }
    
    [HttpPost("refresh-token")]
    public async Task<IActionResult> RefreshToken(RefreshData data)
    {
        var validationResponse = await UserValidator.ValidateUser(HttpContext, context);
        
        if (validationResponse != null)
            return validationResponse;
        
        try
        {
            ClaimsPrincipal principal = JwtController.GetPrincipalFromExpiredToken(data.access_token);
            Claim? authClaim = principal.FindFirst(ClaimTypes.Authentication);
            
            if (authClaim == null)
                throw new SecurityTokenException("Invalid token");
            
            if (!await RefreshTokenService.ValidateRefreshToken(authClaim.Value, data.refresh_token, context))
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