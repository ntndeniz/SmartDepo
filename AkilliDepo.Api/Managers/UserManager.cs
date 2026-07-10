using AkilliDepo.Api.DTOs;
using AkilliDepo.Api.Entities;
using AkilliDepo.Api.Repositories;

namespace AkilliDepo.Api.Managers;

public interface IUserManager
{
    Task<ServiceResult<PagedResponse<UserDto>>> GetPagedAsync(PagedRequest request);
    Task<ServiceResult<UserDto>> CreateAsync(CreateUserRequest request);
    Task<ServiceResult<bool>> DeleteAsync(DeleteRequest request, int requestingUserId);
    Task<ServiceResult<LoginResultDto>> LoginAsync(LoginRequest request);
}

public class UserManager : IUserManager
{
    private readonly IUserRepository _repository;
    private readonly IJwtTokenService _tokenService;

    public UserManager(IUserRepository repository, IJwtTokenService tokenService)
    {
        _repository = repository;
        _tokenService = tokenService;
    }

    private static UserDto ToDto(User u) => new()
    {
        Id = u.Id,
        CompanyId = u.CompanyId,
        Username = u.Username,
        Role = u.Role,
        CreatedAt = u.CreatedAt
    };

    public async Task<ServiceResult<PagedResponse<UserDto>>> GetPagedAsync(PagedRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.CompanyId))
            return ServiceResult<PagedResponse<UserDto>>.BadRequest("CompanyId zorunludur.");

        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize < 1 ? 25 : request.PageSize;

        var (items, totalCount) = await _repository.GetPagedAsync(
            request.CompanyId, page, pageSize, request.Search);

        return ServiceResult<PagedResponse<UserDto>>.Ok(new PagedResponse<UserDto>
        {
            Data = items.Select(ToDto).ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        });
    }

    public async Task<ServiceResult<UserDto>> CreateAsync(CreateUserRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.CompanyId))
            return ServiceResult<UserDto>.BadRequest("CompanyId zorunludur.");
        if (string.IsNullOrWhiteSpace(request.Username))
            return ServiceResult<UserDto>.BadRequest("Kullanıcı adı zorunludur.");
        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 4)
            return ServiceResult<UserDto>.BadRequest("Şifre en az 4 karakter olmalıdır.");

        var existing = await _repository.GetByUsernameAsync(request.CompanyId, request.Username);
        if (existing is not null)
            return ServiceResult<UserDto>.BadRequest("Bu kullanıcı adı zaten kullanılıyor.");

        var role = request.Role == UserRole.Admin ? UserRole.Admin : UserRole.Staff;

        var (hash, salt) = PasswordHasher.Hash(request.Password);

        var user = new User
        {
            CompanyId = request.CompanyId,
            Username = request.Username,
            PasswordHash = hash,
            PasswordSalt = salt,
            Role = role,
            CreatedAt = DateTime.UtcNow
        };

        await _repository.AddAsync(user);
        await _repository.SaveChangesAsync();

        return ServiceResult<UserDto>.Ok(ToDto(user), "Kullanıcı oluşturuldu.");
    }

    public async Task<ServiceResult<bool>> DeleteAsync(DeleteRequest request, int requestingUserId)
    {
        if (string.IsNullOrWhiteSpace(request.CompanyId))
            return ServiceResult<bool>.BadRequest("CompanyId zorunludur.");

        var user = await _repository.GetByIdAsync(request.Id);
        if (user is null)
            return ServiceResult<bool>.NotFound("Kullanıcı bulunamadı.");
        if (user.CompanyId != request.CompanyId)
            return ServiceResult<bool>.Forbidden("Bu kullanıcıya erişim yetkiniz yok.");
        if (user.Id == requestingUserId)
            return ServiceResult<bool>.BadRequest("Kendi hesabınızı silemezsiniz.");
        if (user.Role == UserRole.Admin)
        {
            var adminCount = await _repository.CountAdminsAsync(request.CompanyId);
            if (adminCount <= 1)
                return ServiceResult<bool>.BadRequest("Şirketin son yöneticisini silemezsiniz.");
        }

        user.IsDeleted = true;
        await _repository.UpdateAsync(user);
        await _repository.SaveChangesAsync();

        return ServiceResult<bool>.Ok(true, "Kullanıcı silindi.");
    }

    public async Task<ServiceResult<LoginResultDto>> LoginAsync(LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.CompanyId))
            return ServiceResult<LoginResultDto>.BadRequest("Şirket adı zorunludur.");
        if (string.IsNullOrWhiteSpace(request.Username))
            return ServiceResult<LoginResultDto>.BadRequest("Kullanıcı adı zorunludur.");
        if (string.IsNullOrWhiteSpace(request.Password))
            return ServiceResult<LoginResultDto>.BadRequest("Şifre zorunludur.");

        var user = await _repository.GetByUsernameAsync(request.CompanyId, request.Username);
        if (user is null || !PasswordHasher.Verify(request.Password, user.PasswordHash, user.PasswordSalt))
            return ServiceResult<LoginResultDto>.BadRequest("Şirket adı, kullanıcı adı veya şifre hatalı.");

        var token = _tokenService.GenerateToken(user);
        return ServiceResult<LoginResultDto>.Ok(new LoginResultDto { Token = token, User = ToDto(user) }, "Giriş başarılı.");
    }
}
