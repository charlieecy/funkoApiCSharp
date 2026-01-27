using System.Linq.Expressions;
using FunkoApi.DataBase;
using FunkoApi.Models;
using Microsoft.EntityFrameworkCore;

namespace FunkoApi.Repository.Users;

public class UserRepository(
    Context context,
    ILogger<UserRepository> logger
) : IUserRepository
{
 
    public async Task<User?> FindByIdAsync(long id)
    {
        return await context.Users.FindAsync(id);
    }

    /// <inheritdoc/>
    public async Task<User?> FindByUsernameAsync(string username)
    {
        return await context.Users.FirstOrDefaultAsync(u => u.Username == username);
    }

    /// <inheritdoc/>
    public async Task<User?> FindByEmailAsync(string email)
    {
        return await context.Users.FirstOrDefaultAsync(u => u.Email == email);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<User>> FindAllAsync()
    {
        return await context.Users.ToListAsync();
    }


    /// <inheritdoc/>
    public async Task<User> SaveAsync(User user)
    {
        context.Users.Add(user);
        await context.SaveChangesAsync();
        logger.LogInformation("Usuario creado con ID: {Id}", user.Id);
        return user;
    }

    /// <inheritdoc/>
    public async Task<User> UpdateAsync(User user)
    {
        context.Users.Update(user);
        await context.SaveChangesAsync();
        logger.LogInformation("Usuario actualizado con ID: {Id}", user.Id);
        return user;
    }

    /// <inheritdoc/>
    public async Task DeleteAsync(long id)
    {
        var user = await FindByIdAsync(id);
        if (user is not null)
        {
            user.IsDeleted = true;
            await context.SaveChangesAsync();
            logger.LogInformation("Usuario eliminado con ID: {Id}", id);
        }
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<User>> GetActiveUsersAsync()
    {
        logger.LogDebug("Obteniendo usuarios activos");
        return await context.Users
            .Where(u => !u.IsDeleted)
            .OrderBy(u => u.Email)
            .ToListAsync();
    }

    private static IQueryable<User> ApplySorting(IQueryable<User> query, string sortBy, string direction)
    {
        var isDescending = direction.Equals("desc", StringComparison.OrdinalIgnoreCase);
        Expression<Func<User, object>> keySelector = sortBy.ToLower() switch
        {
            "username" => u => u.Username,
            "email" => u => u.Email,
            "createdat" => u => u.CreatedAt,
            _ => u => u.Id
        };
        return isDescending ? query.OrderByDescending(keySelector) : query.OrderBy(keySelector);
    }
}