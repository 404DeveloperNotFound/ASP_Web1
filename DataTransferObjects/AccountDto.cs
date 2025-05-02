namespace WebApplication1.DataTransferObjects
{
    public record RegisterDto(string Username, string Email, string Password, string Role);
    public record LoginDto(string Email, string Password);
    public record AuthenticatedUserDto(int Id, string Username, string Email, string Role);
}
