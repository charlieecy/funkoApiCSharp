namespace FunkoApi.Repository.Users;

public interface IUserRepository
{
    Task<Models.User?> FindByIdAsync(long id);

    Task<Models.User?> FindByUsernameAsync(string username);

    Task<Models.User?> FindByEmailAsync(string email);
    
    Task<IEnumerable<Models.User>> FindAllAsync();
    
    Task<Models.User> SaveAsync(Models.User user);
    
    Task<Models.User> UpdateAsync(Models.User user);
    
    Task DeleteAsync(long id);
    
    Task<IEnumerable<Models.User>> GetActiveUsersAsync();
}