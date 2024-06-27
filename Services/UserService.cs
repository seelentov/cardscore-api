using cardscore_api.Data;
using cardscore_api.Models;
using cardscore_api.Models.Dtos;
using Microsoft.EntityFrameworkCore;

namespace cardscore_api.Services
{
    public class UserService
    {
        private DataContext _context;
        private readonly RoleService _roleService;
        private readonly BCryptService _bCryptService;

        public object JsRuntime { get; private set; }

        public UserService(DataContext context, RoleService roleService, BCryptService bCryptService)
        {
            _context = context;
            _roleService = roleService;
            _bCryptService = bCryptService;
        }

        public async Task<List<User>> GetAll()
        {
            var data = await _context.Users.ToListAsync();
            return data;
        }

        public async Task<User> GetByExpoToken(string expoToken)
        {
            var user = await _context.Users.FirstOrDefaultAsync();

            return user;
        }

        public async Task UpdateExpoNotification(int userId, string newToken)
        {
            var user = await GetById(userId);

            var userWithExpoToken = await GetByExpoToken(newToken);

            if(userWithExpoToken != null)
            {
                userWithExpoToken.ExpoToken = string.Empty;
            }

            if (user != null)
            {
                user.ExpoToken = newToken;
            }

            _context.SaveChanges();
        }

        public async Task<User> Update(int id, User newUserData, bool isPassChanged = false)
        {
            var user = await GetById(id);

            if (newUserData.Name != null)
            {
                user.Name = newUserData.Name;
            }

            if(newUserData.Email != null)
            {
                user.Email = newUserData.Email;
            }

            if (newUserData.Phone != null)
            {
                user.Phone = newUserData.Phone;
            }

            user.Active = newUserData.Active;

            if (newUserData.UniqueId != null)
            {
                user.UniqueId = newUserData.UniqueId;
            }

            if (newUserData.Role != null)
            {
                user.Role = newUserData.Role;
            }

            if (newUserData.SubData != null)
            {
                user.SubData = newUserData.SubData;
            }

            if (newUserData.SubStatus != null)
            {
                user.SubStatus = newUserData.SubStatus;
            }

            if(isPassChanged)
            {
                var newPassword = _bCryptService.Hash(newUserData.PasswordHash);
                user.PasswordHash = newPassword;
            }

            _context.SaveChanges();

            return await GetById(id);
        }
        public async Task<User> GetById(int id)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
            return user;
        }

        public async Task Remove(int id)
        {
            var user = await GetById(id);
            _context.Users.Remove(user);
            _context.SaveChanges();
        }

        public async Task<bool> IsUniqueByPhone(string uniqueId)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UniqueId == uniqueId);
            return user == null;
        }

        public async Task ActivateById(int id)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
            if(user != null)
            {
                user.Active = true;
                
            }
            await _context.SaveChangesAsync();
        }

        public async Task DeactivateById(int id)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
            if (user != null)
            {
                user.Active = false;

            }
            await _context.SaveChangesAsync();
        }

        public async Task<User> GetByName(string name)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Name == name);
            return user;
        }

        public async Task<User> GetByEmail(string email)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            return user;
        }


        public async Task<User> GetByPhone(string phone)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Phone == phone);
            return user;
        }


        public async Task<User> Create(CreateUserDto createUserDto)
        {
            var role = await _roleService.GetByName("USER");

            var passwordHash = _bCryptService.Hash(createUserDto.Password);

            var subData = DateTime.UtcNow.AddDays(14);

            var user = new User()
            {
                PasswordHash = passwordHash,
                Name = createUserDto.Name,
                Phone = createUserDto.Phone,
                Email = createUserDto.Email,
                Role = role,
                SubData = subData,
                SubStatus = (int)SubStatus.Test,
                Active = true,
                UniqueId = createUserDto.UniqueId
            };

            await _context.AddAsync(user);
            await _context.SaveChangesAsync();

            return await GetByName(createUserDto.Name);
        }

        public async Task UpdateSubStatus(int userId, int days)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            DateTime dateTime = DateTime.UtcNow.AddDays(days);
            user.SubData = dateTime;
            await _context.SaveChangesAsync();
        }

    }
}
