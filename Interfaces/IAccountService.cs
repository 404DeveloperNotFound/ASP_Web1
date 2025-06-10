using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using WebApplication1.DataTransferObjects;

namespace WebApplication1.Interfaces
{
    public interface IAccountService
    {
        Task<AuthenticatedUserDto> RegisterAsync(RegisterDto dto);
        Task<AuthenticatedUserDto> LoginAsync(LoginDto dto);
        List<Claim> BuildClaims(AuthenticatedUserDto user);
        Task SignInAsync(List<Claim> claims);
    }
}
