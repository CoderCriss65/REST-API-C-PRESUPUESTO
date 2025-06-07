using BackendREST.Models;

namespace BackendREST.Services
{
    public interface IAuthService
    {
        string GenerateJwtToken(Usuario usuario);
        string HashPassword(string password);
    }
}
