using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Asp.Versioning;
using CellPhoneContactsAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace CellPhoneContactsAPI.Controllers.v1;

[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
[ApiController]
public class AuthenticationController : ControllerBase
{
    private readonly AuthService _authService;

    public record AuthenticationData(string? UserName, string? Password);
    public record UserData(int UserId, string UserName, string Title, string EmployeeID);

    //IConfiguration _config;
    //public AuthenticationController(IConfiguration config)
    //{
    //    _config = config;
    //}

    public AuthenticationController(AuthService authService)
    {
        _authService = authService;
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        // Get user ID from JWT claims
        var userIdClaim = User.FindFirst("id")?.Value;
        if (userIdClaim == null)
            return Unauthorized();

        if (!int.TryParse(userIdClaim, out int userId))
            return Unauthorized();

        //NOTE: we may not wish to revoke all refresh tokens.
        //      in some cases they may be logged in with another app or site in which case we only want to revoke the session
        //      not all sessions. Same time we will want a feature where the user could log out of ALL sites like this would do.
        await _authService.RevokeAllRefreshTokensForUser(userId);

        return Ok(new { message = "Logged out successfully, all refresh tokens revoked." });
    }
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest model)
    {
        // TODO: get client IP and user agent from request headers if needed
        string? ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        string? userAgent = Request.Headers.UserAgent;

        var userId = await _authService.AuthenticateUser(model.Username, model.Password, ip, userAgent);
        if (userId == null)
            return Unauthorized("Invalid credentials or account locked.");

        var jwt = _authService.GenerateJwtToken(userId.Value, model.Username);
        var refreshToken = await _authService.GenerateRefreshToken(userId.Value, ip, userAgent);

        return Ok(new
        {
            access_token = jwt,
            refresh_token = refreshToken
        });
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest model)
    {
        var userId = await _authService.ValidateRefreshToken(model.RefreshToken);
        if (userId == null)
            return Unauthorized("Invalid or expired refresh token.");

        // Optionally revoke old refresh token (token rotation)
        await _authService.RevokeRefreshToken(model.RefreshToken);

        // Issue new JWT and refresh token
        // For username, query from DB or include in refresh token claims if you want
        // Here, assume we fetch username by userId
        string username = "user"; // Replace with actual fetch

        var jwt = _authService.GenerateJwtToken(userId.Value, username);
        var newRefreshToken = await _authService.GenerateRefreshToken(userId.Value);

        return Ok(new
        {
            access_token = jwt,
            refresh_token = newRefreshToken
        });
    }



    //// api/authentication/token
    //[HttpPost("token")]
    //[AllowAnonymous]
    //public ActionResult<string> Authenticate([FromBody] AuthenticationData data)
    //{
    //    var user = ValidateCredentials(data);

    //    if (user == null)
    //    {
    //        return Unauthorized();
    //    }
    //    var token = GenerateToken(user);
    //    return Ok(token);

    //}
    //private string GenerateToken(UserData user)
    //{
    //    // we need a secret key, issuer, and audience to make the token
    //    byte[] hash;

    //    SymmetricSecurityKey SecretKey;
    //    using (var sha = SHA256.Create())
    //    {
    //        hash = sha.ComputeHash(Encoding.ASCII.GetBytes(
    //            _config.GetValue<string>("Authentication:SecretKey")!));
    //        SecretKey = new SymmetricSecurityKey(hash);
    //    }



    //    var signingCredentials = new SigningCredentials(SecretKey, SecurityAlgorithms.HmacSha256);

    //    // Keep small for authentication/permissions
    //    List<Claim> claims = new List<Claim>();

    //    claims.Add(new(JwtRegisteredClaimNames.Sub, user.UserId.ToString()));
    //    claims.Add(new(JwtRegisteredClaimNames.UniqueName, user.UserName.ToString()));

    //    claims.Add(new("title", user.Title));
    //    claims.Add(new("employeeID", user.EmployeeID));


    //    // Generall should have a token short, but a refresh token to update it.
    //    var token = new JwtSecurityToken(_config.GetValue<string>("Authentication:Issuer"),
    //        _config.GetValue<string>("Authentication:Audience"),
    //        claims,
    //        DateTime.UtcNow, // when is valid
    //        DateTime.UtcNow.AddMinutes(1), // when token expires.
    //        signingCredentials
    //        );

    //    return new JwtSecurityTokenHandler().WriteToken(token);
    //}
    //private UserData? ValidateCredentials(AuthenticationData data)
    //{
    //    //TODO: NOT PRODUCTION CODE
    //    if (CompareValues(data.UserName, "mberryh")
    //        && CompareValues(data.Password, "pass123"))
    //    {
    //        return new UserData(1, data.UserName!, "Business Owner", "E001");
    //    }


    //    // We will have to take the user's password,
    //    // go to the data base, get the salt and apply it,
    //    // and apply the pepper from the secrets.json
    //    // then compare with the db hash. If we have a match it's a valid user.

    //    return null;
    //}
    //private bool CompareValues(string? actual, string expected)
    //{
    //    if (actual is not null)
    //    {
    //        if (actual.Equals(expected, StringComparison.InvariantCultureIgnoreCase))
    //        {
    //            return true;
    //        }
    //    }
    //    return false;
    //}
    // GET: api/<AuthenticationController>
    [HttpGet]
    public IEnumerable<string> Get()
    {
        return new string[] { "value1", "value2" };
    }

    // GET api/<AuthenticationController>/5
    [HttpGet("{id}")]
    public string Get(int id)
    {
        return "value";
    }

    // POST api/<AuthenticationController>
    [HttpPost]
    public void Post([FromBody] string value)
    {
    }

    // PUT api/<AuthenticationController>/5
    [HttpPut("{id}")]
    public void Put(int id, [FromBody] string value)
    {
    }

    // DELETE api/<AuthenticationController>/5
    [HttpDelete("{id}")]
    public void Delete(int id)
    {
    }
}

public class LoginRequest
{
    public string Username { get; set; } = default!;
    public string Password { get; set; } = default!;
}

public class RefreshRequest
{
    public string RefreshToken { get; set; } = default!;
}