using WebApplication1.DataTransferObjects;

namespace WebApplication1.Interfaces
{
    public interface IAccountService
    {
        Task<AuthenticatedUserDto> RegisterAsync(RegisterDto dto);
        Task<AuthenticatedUserDto> LoginAsync(LoginDto dto);
    }
}
