//using System;
//using System.Security.Claims;
//using Microsoft.EntityFrameworkCore;
//using WebApplication1.Data;
//using WebApplication1.Models;

//public class UserService
//{
//    private readonly Web1Context _context;

//    public UserService(Web1Context context)
//    {
//        _context = context;
//    }

//    public async Task<Client> RegisterUser(string username, string email, string password, string role)
//    {
//        var user = new Client
//        {
//            Name = username,
//            Email = email,
//            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
//            Role = role
//        };

//        _context.Clients.Add(user);
//        await _context.SaveChangesAsync();
//        return user;
//    }

//    public async Task<User> AuthenticateUser(string username, string password)
//    {
//        var user = await _context.Clients.FirstOrDefaultAsync(u => u.Name == username);
//        if (user == null || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
//            return null;

//        return user;
//    }
//}