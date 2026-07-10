using AkilliDepo.Api.Data;
using AkilliDepo.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace AkilliDepo.Api.Repositories;

public interface ICompanySettingsRepository
{
    Task<CompanySettings?> GetByCompanyIdAsync(string companyId);
    Task AddAsync(CompanySettings settings);
    Task UpdateAsync(CompanySettings settings);
    Task SaveChangesAsync();
}

public class CompanySettingsRepository : ICompanySettingsRepository
{
    private readonly AppDbContext _context;

    public CompanySettingsRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task<CompanySettings?> GetByCompanyIdAsync(string companyId) =>
        _context.CompanySettings.FirstOrDefaultAsync(s => s.CompanyId == companyId);

    public async Task AddAsync(CompanySettings settings)
    {
        await _context.CompanySettings.AddAsync(settings);
    }

    public Task UpdateAsync(CompanySettings settings)
    {
        _context.Entry(settings).State = EntityState.Modified;
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync() => _context.SaveChangesAsync();
}
