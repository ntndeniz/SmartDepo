using AkilliDepo.Api.Data;
using AkilliDepo.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace AkilliDepo.Api.Repositories;

public interface IUserRepository
{
    Task<(List<User> Items, int TotalCount)> GetPagedAsync(string companyId, int page, int pageSize, string? search);
    Task<User?> GetByIdAsync(int id);
    Task<User?> GetByUsernameAsync(string companyId, string username);
    Task<List<string>> GetDistinctCompanyIdsAsync();
    Task<int> CountAdminsAsync(string companyId);
    Task AddAsync(User user);
    Task UpdateAsync(User user);
    Task SaveChangesAsync();
}

public class UserRepository : IUserRepository
{
    private readonly AppDbContext _context;

    public UserRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<(List<User> Items, int TotalCount)> GetPagedAsync(
        string companyId, int page, int pageSize, string? search)
    {
        var query = _context.Users.AsNoTracking().Where(u => u.CompanyId == companyId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(u => u.Username.Contains(search));
        }

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderBy(u => u.Username)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public Task<User?> GetByIdAsync(int id) =>
        _context.Users.FirstOrDefaultAsync(u => u.Id == id);

    public Task<User?> GetByUsernameAsync(string companyId, string username) =>
        _context.Users.FirstOrDefaultAsync(u => u.CompanyId == companyId && u.Username == username);

    public Task<List<string>> GetDistinctCompanyIdsAsync() =>
        _context.Users.AsNoTracking().Select(u => u.CompanyId).Distinct().ToListAsync();

    public Task<int> CountAdminsAsync(string companyId) =>
        _context.Users.AsNoTracking().CountAsync(u => u.CompanyId == companyId && u.Role == UserRole.Admin);

    public async Task AddAsync(User user)
    {
        await _context.Users.AddAsync(user);
    }

    public Task UpdateAsync(User user)
    {
        _context.Entry(user).State = EntityState.Modified;
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync() => _context.SaveChangesAsync();
}
