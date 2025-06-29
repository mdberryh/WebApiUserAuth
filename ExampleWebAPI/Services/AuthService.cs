namespace CellPhoneContactsAPI.Services;
using Npgsql;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Runtime.Intrinsics.Arm;

public class AuthService
{
    private readonly string _connectionString;

    //We never want to hold the plain text password, so always hold the hash instead.
    private readonly SymmetricSecurityKey _jwtKey;
    private readonly string _jwtIssuer;
    private readonly string _jwtAudience;
    private readonly int _jwtExpireMinutes;

    public AuthService(IConfiguration configuration)
    {
        _connectionString = configuration["ConnectionStrings:Default"];

        string appSalt = configuration["Authentication:AppSalt"];

        byte[] hash;
        SymmetricSecurityKey SecretKey;

        // use salt from the secrets.json to the plain text secret key before hashing!
        using (var sha = SHA256.Create())
        {
            hash = sha.ComputeHash(Encoding.ASCII.GetBytes(appSalt + configuration["Authentication:SecretKey"]!));
            SecretKey = new SymmetricSecurityKey(hash);
        }
        _jwtKey = SecretKey;

        _jwtIssuer = configuration["Authentication:Issuer"];
        _jwtAudience = configuration["Authentication:Audience"];
        _jwtExpireMinutes = int.Parse(configuration["Authentication:ExpireMinutes"]);
    }
    public async Task CleanupExpiredRefreshTokens()
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();

        await using var cmd = new NpgsqlCommand(
            "DELETE FROM refresh_tokens WHERE expires_at < NOW() OR revoked = true;", conn);

        await cmd.ExecuteNonQueryAsync();
    }
    public async Task RevokeAllRefreshTokensForUser(int userId)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();

        await using var cmd = new NpgsqlCommand("UPDATE refresh_tokens SET revoked = true WHERE user_id = @user_id AND revoked = false;", conn);
        cmd.Parameters.AddWithValue("user_id", userId);

        await cmd.ExecuteNonQueryAsync();
    }
    public async Task<int?> AuthenticateUser(string username, string password, string ip = null, string userAgent = null)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();

        await using var cmd = new NpgsqlCommand("SELECT authenticate_user(@username, @password, @ip, @user_agent);", conn);
        cmd.Parameters.AddWithValue("username", username);
        cmd.Parameters.AddWithValue("password", password);
        cmd.Parameters.AddWithValue("ip", (object)ip ?? DBNull.Value);
        cmd.Parameters.AddWithValue("user_agent", (object)userAgent ?? DBNull.Value);

        var success = await cmd.ExecuteScalarAsync();

        if (success != null && (bool)success)
        {
            // Get user ID
            await using var idCmd = new NpgsqlCommand("SELECT id FROM users WHERE username = @username;", conn);
            idCmd.Parameters.AddWithValue("username", username);
            var userIdObj = await idCmd.ExecuteScalarAsync();
            if (userIdObj != null)
                return Convert.ToInt32(userIdObj);
        }

        return null;
    }

    public string GenerateJwtToken(int userId, string username)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim("id", userId.ToString()),
                new Claim(ClaimTypes.Name, username),
                new(JwtRegisteredClaimNames.Sub, userId.ToString()),
                new(JwtRegisteredClaimNames.UniqueName, username.ToString()),

                new("title", "Business Owner"), // we can add some additional claims should we want
                new("employeeID", "1") //just a place holder 
            }),
            Expires = DateTime.UtcNow.AddMinutes(_jwtExpireMinutes),
            Issuer = _jwtIssuer,
            Audience = _jwtAudience,
            SigningCredentials = new SigningCredentials(_jwtKey, SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    public async Task<string> GenerateRefreshToken(int userId, string ip = null, string userAgent = null)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();

        await using var cmd = new NpgsqlCommand("SELECT generate_refresh_token(@user_id, @ip, @user_agent);", conn);
        cmd.Parameters.AddWithValue("user_id", userId);
        cmd.Parameters.AddWithValue("ip", (object)ip ?? DBNull.Value);
        cmd.Parameters.AddWithValue("user_agent", (object)userAgent ?? DBNull.Value);

        var tokenObj = await cmd.ExecuteScalarAsync();
        return tokenObj?.ToString();
    }

    public async Task<int?> ValidateRefreshToken(string refreshToken)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();

        await using var cmd = new NpgsqlCommand("SELECT validate_refresh_token(@token);", conn);
        cmd.Parameters.AddWithValue("token", refreshToken);

        var result = await cmd.ExecuteScalarAsync();
        if (result == DBNull.Value || result == null)
            return null;

        return Convert.ToInt32(result);
    }

    public async Task RevokeRefreshToken(string refreshToken)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();

        await using var cmd = new NpgsqlCommand("SELECT revoke_refresh_token(@token);", conn);
        cmd.Parameters.AddWithValue("token", refreshToken);

        await cmd.ExecuteNonQueryAsync();
    }
}
