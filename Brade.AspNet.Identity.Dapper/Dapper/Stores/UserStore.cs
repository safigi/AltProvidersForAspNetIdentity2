using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Claims;
using System.Threading.Tasks;
using Brade.AspNet.Identity.Identity;
using Dapper;
using Microsoft.AspNet.Identity;
using Safi.AspNet.Identity.Common;

namespace Brade.AspNet.Identity.Dapper.Dapper.Stores
{
    public class DapperUserStore :
        IUserLoginStore<IdentityUser, int>,
        IUserClaimStore<IdentityUser, int>,
        IUserRoleStore<IdentityUser, int>,
        IUserPasswordStore<IdentityUser, int>,
        IUserSecurityStampStore<IdentityUser, int>,
        IQueryableUserStore<IdentityUser, int>,
        IUserEmailStore<IdentityUser, int>,
        IUserPhoneNumberStore<IdentityUser, int>,
        IUserTwoFactorStore<IdentityUser, int>,
        IUserLockoutStore<IdentityUser, int>        
    {
        private readonly int _commandTimeout = 5;
        public DapperUserStore() { }

        public DapperUserStore(string connectionString)
        {
            ConnectionString = connectionString;
        }

        public string ConnectionString { get; set; }

        public void Dispose()
        {
        }        

        public async Task CreateAsync(IdentityUser user)
        {
            using (var conn = ConnectionHelper.CreateDbConnection(ConnectionString))
            {
                var db = IdentityDatabase.Init(conn, _commandTimeout);                

                var result = await FindByEmailAsync(user.Email);
                if (result == null)
                {  
                    await db.Users.InsertAsync(user);
                }
            }
        }

        public async Task UpdateAsync(IdentityUser user)
        {
            using (var conn =  ConnectionHelper.CreateDbConnection(ConnectionString))
            {
                var db = IdentityDatabase.Init(conn, _commandTimeout);

                await db.Users.UpdateAsync(user.Id, user);
            }
        }

        public async Task DeleteAsync(IdentityUser user)
        {
            using (var conn =  ConnectionHelper.CreateDbConnection(ConnectionString))
            {
                var db = IdentityDatabase.Init(conn, _commandTimeout);

                await db.Users.DeleteAsync(user.Id);
            }
        }
      

        public async Task<IdentityUser> FindByIdAsync(int userId)
        {
            using (var conn =  ConnectionHelper.CreateDbConnection(ConnectionString))
            {
                return (await conn.QueryAsync<IdentityUser>(@"select * from dbo.Users where Id=@Id", new { Id = userId })).SingleOrDefault();
            }
        }

        public async Task<IdentityUser> FindByNameAsync(string userName)
        {
            using (var conn =  ConnectionHelper.CreateDbConnection(ConnectionString))
            {
                return (await conn.QueryAsync<IdentityUser>(@"select * from dbo.Users where UserName=@Name", new { Name = userName })).SingleOrDefault();
            }
        }

        public async Task AddLoginAsync(IdentityUser user, UserLoginInfo login)
        {
            using (var conn =  ConnectionHelper.CreateDbConnection(ConnectionString))
            {
                var db = IdentityDatabase.Init(conn, _commandTimeout);

                var instance = new IdentityUserLogin
                {
                    UserId = user.Id,
                    LoginProvider = login.LoginProvider,
                    ProviderKey = login.ProviderKey,
                };

                await db.UserLogins.InsertAsync(instance);
            }
        }

        public async Task RemoveLoginAsync(IdentityUser user, UserLoginInfo login)
        {
            using (var conn =  ConnectionHelper.CreateDbConnection(ConnectionString))
            {
                await conn.ExecuteAsync("delete from dbo.UserLogins where LoginProvider=@LoginProvider and ProviderKey=@ProviderKey", new { login.LoginProvider, login.ProviderKey }).ConfigureAwait(false);
            }
        }

        public async Task<IList<UserLoginInfo>> GetLoginsAsync(IdentityUser user)
        {
            using (var conn =  ConnectionHelper.CreateDbConnection(ConnectionString))
            {
                return (await conn
                    .QueryAsync<IdentityUserLogin>(@"select * from dbo.UserLogins where UserId=@UserId", new { UserId = user.Id }))
                    .Select(x => new UserLoginInfo(x.LoginProvider, x.ProviderKey))
                    .ToList();
            }
        }

        public async Task<IdentityUser> FindAsync(UserLoginInfo login)
        {
            using (var conn =  ConnectionHelper.CreateDbConnection(ConnectionString))
            {
                return (await conn
                    .QueryAsync<IdentityUser>(@"select U.* from dbo.Users U inner join dbo.UserLogins UL on U.Id=UL.UserId where UL.LoginProvider=@LoginProvider and UL.ProviderKey=@ProviderKey", new { login.LoginProvider, login.ProviderKey })).SingleOrDefault();
            }
        }

        public async Task<IList<Claim>> GetClaimsAsync(IdentityUser user)
        {
            using (var conn =  ConnectionHelper.CreateDbConnection(ConnectionString))
            {
                return (await conn
                    .QueryAsync<IdentityUserClaim>(@"select * from dbo.UserClaims where UserId=@UserId", new { UserId = user.Id }))
                    .Select(x => new Claim(x.ClaimType, x.ClaimValue))
                    .ToList();
            }
        }

        public async Task AddClaimAsync(IdentityUser user, Claim claim)
        {
            using (var conn =  ConnectionHelper.CreateDbConnection(ConnectionString))
            {
                var db = IdentityDatabase.Init(conn, _commandTimeout);

                var instance = new IdentityUserClaim
                {
                    UserId = user.Id,
                    ClaimType = claim.Type,
                    ClaimValue = claim.Value,
                };

                await db.UserClaims.InsertAsync(instance);
            }
        }

        public async Task RemoveClaimAsync(IdentityUser user, Claim claim)
        {
            using (var conn =  ConnectionHelper.CreateDbConnection(ConnectionString))
            {
                await conn.ExecuteAsync("delete from dbo.UserClaims where UserId=@UserId and ClaimValue=@ClaimValue and ClaimType=@ClaimType", new { UserId = user.Id, ClaimValue = claim.Value, ClaimType = claim.Type }).ConfigureAwait(false);
            }
        }

        public async Task AddToRoleAsync(IdentityUser user, string roleName)
        {
            using (var conn =  ConnectionHelper.CreateDbConnection(ConnectionString))
            {
                var db = IdentityDatabase.Init(conn, _commandTimeout);

                var role = (await conn.QueryAsync<IdentityRole>(@"select * from dbo.Roles where Name=@RoleName", new { RoleName = roleName })).SingleOrDefault();
                if (role == null)
                {
                    throw new InvalidOperationException(string.Format("Role {0} not found.", roleName));
                }

                var instance = new IdentityUserRole
                {
                    RoleId = role.Id,
                    UserId = user.Id,
                };

                await db.UserRoles.InsertAsync(instance);
            }
        }

        public async Task RemoveFromRoleAsync(IdentityUser user, string roleName)
        {
            using (var conn =  ConnectionHelper.CreateDbConnection(ConnectionString))
            {
                IdentityRole role = (await conn.QueryAsync<IdentityRole>(@"select * from dbo.Roles where Name=@RoleName", new { RoleName = roleName })).SingleOrDefault();
                if (role == null)
                {
                    throw new InvalidOperationException(string.Format("Role {0} not found.", roleName));
                }

                await conn.ExecuteAsync("delete from dbo.UserRoles UR inner join dbo.Roles R on UR.RoleId=R.Id where UR.UserId=@UserId and R.RoleName=@RoleName", new { UserId = user.Id, RoleName = roleName }).ConfigureAwait(false);
            }
        }

        public async Task<IList<string>> GetRolesAsync(IdentityUser user)
        {
            using (var conn =  ConnectionHelper.CreateDbConnection(ConnectionString))
            {
                return (await conn
                    .QueryAsync<IdentityRole>(@"select R.* from dbo.UserRoles UR inner join dbo.Roles R on UR.RoleId=R.Id where UR.UserId=@UserId", new { UserId = user.Id }))
                    .Select(x => x.Name)
                    .ToList();
            }
        }

        public async Task<bool> IsInRoleAsync(IdentityUser user, string roleName)
        {
            using (var conn =  ConnectionHelper.CreateDbConnection(ConnectionString))
            {
                return (await conn
                    .QueryAsync<IdentityRole>(@"select R.* from dbo.UserRoles UR inner join dbo.Roles R on UR.RoleId=R.Id where UR.UserId=@UserId and R.Name=@RoleName", new { UserId = user.Id, RoleName = roleName }))
                    .Count() == 1;
            }
        }

        public async Task SetPasswordHashAsync(IdentityUser user, string passwordHash)
        {
            using (var conn =  ConnectionHelper.CreateDbConnection(ConnectionString))
            {
                var db = IdentityDatabase.Init(conn, _commandTimeout);

                var u =await db.Users.GetAsync(user.Id);

                if (u != null)
                {
                    u.PasswordHash = passwordHash;

                    await db.Users.UpdateAsync(u.Id, u);
                }
            }
        }

        public async Task<string> GetPasswordHashAsync(IdentityUser user)
        {
            using (var conn =  ConnectionHelper.CreateDbConnection(ConnectionString))
            {
                var db = IdentityDatabase.Init(conn, _commandTimeout);

                return (await db.Users.GetAsync(user.Id)).PasswordHash;
            }
        }

        public async Task<bool> HasPasswordAsync(IdentityUser user)
        {
            using (var conn =  ConnectionHelper.CreateDbConnection(ConnectionString))
            {
                var db = IdentityDatabase.Init(conn, _commandTimeout);

                var u =await db.Users.GetAsync(user.Id);

                return !string.IsNullOrEmpty(u.PasswordHash);
            }
        }

        public async Task SetSecurityStampAsync(IdentityUser user, string stamp)
        {
            using (var conn =  ConnectionHelper.CreateDbConnection(ConnectionString))
            {
                var db = IdentityDatabase.Init(conn, _commandTimeout);

                var u =await db.Users.GetAsync(user.Id);
                if (u == null)
                {
                    throw new InvalidOperationException("Cannot find a user to set the security stamp.");
                }

                u.SecurityStamp = stamp;

                await db.Users.UpdateAsync(u.Id, u);
            }
        }

        public async Task<string> GetSecurityStampAsync(IdentityUser user)
        {
            using (var conn =  ConnectionHelper.CreateDbConnection(ConnectionString))
            {
                var db = IdentityDatabase.Init(conn, _commandTimeout);

                return (await db.Users.GetAsync(user.Id)).SecurityStamp;
            }
        }

        public IQueryable<IdentityUser> Users
        {
            get
            {                
                using (var conn = ConnectionHelper.CreateDbConnection(ConnectionString))
                {
                    return conn.Query<IdentityUser>(@"select * from dbo.Users").AsQueryable();
                }
            
            }
        }

        public async Task SetEmailAsync(IdentityUser user, string email)
        {
            using (var conn =  ConnectionHelper.CreateDbConnection(ConnectionString))
            {
                var db = IdentityDatabase.Init(conn, _commandTimeout);

                var u =await db.Users.GetAsync(user.Id);

                if (u != null)
                {
                    u.Email = email;

                    await db.Users.UpdateAsync(u.Id, u);
                }
            }
        }

        public async Task<string> GetEmailAsync(IdentityUser user)
        {
            using (var conn =  ConnectionHelper.CreateDbConnection(ConnectionString))
            {
                var db = IdentityDatabase.Init(conn, _commandTimeout);

                return (await db.Users.GetAsync(user.Id)).Email;
            }
        }

        public async Task<bool> GetEmailConfirmedAsync(IdentityUser user)
        {
            using (var conn =  ConnectionHelper.CreateDbConnection(ConnectionString))
            {
                var db = IdentityDatabase.Init(conn, _commandTimeout);

                return (await db.Users.GetAsync(user.Id)).EmailConfirmed;
            }
        }

        public async Task SetEmailConfirmedAsync(IdentityUser user, bool confirmed)
        {
            using (var conn =  ConnectionHelper.CreateDbConnection(ConnectionString))
            {
                var db = IdentityDatabase.Init(conn, _commandTimeout);

                var u =await db.Users.GetAsync(user.Id);

                if (u != null)
                {
                    u.EmailConfirmed = confirmed;

                    await db.Users.UpdateAsync(u.Id, u);
                }
            }
        }

        public async Task<IdentityUser> FindByEmailAsync(string email)
        {
            using (var conn =  ConnectionHelper.CreateDbConnection(ConnectionString))
            {
                return (await conn.QueryAsync<IdentityUser>(@"select * from dbo.Users where Email=@Email", new { Email = email })).SingleOrDefault();
            }
        }

        public async Task SetPhoneNumberAsync(IdentityUser user, string phoneNumber)
        {
            using (var conn =  ConnectionHelper.CreateDbConnection(ConnectionString))
            {
                var db = IdentityDatabase.Init(conn, _commandTimeout);

                var u =await db.Users.GetAsync(user.Id);

                if (u != null)
                {
                    u.PhoneNumber = phoneNumber;

                    await db.Users.UpdateAsync(u.Id, u);
                }
            }
        }

        public async Task<string> GetPhoneNumberAsync(IdentityUser user)
        {
            using (var conn =  ConnectionHelper.CreateDbConnection(ConnectionString))
            {
                var db = IdentityDatabase.Init(conn, _commandTimeout);

                return (await db.Users.GetAsync(user.Id)).PhoneNumber;
            }
        }

        public async Task<bool> GetPhoneNumberConfirmedAsync(IdentityUser user)
        {
            using (var conn =  ConnectionHelper.CreateDbConnection(ConnectionString))
            {
                var db = IdentityDatabase.Init(conn, _commandTimeout);

                return (await db.Users.GetAsync(user.Id)).PhoneNumberConfirmed;
            }
        }

        public async Task SetPhoneNumberConfirmedAsync(IdentityUser user, bool confirmed)
        {
            using (var conn =  ConnectionHelper.CreateDbConnection(ConnectionString))
            {
                var db = IdentityDatabase.Init(conn, _commandTimeout);

                var u =await db.Users.GetAsync(user.Id);

                if (u != null)
                {
                    u.PhoneNumberConfirmed = confirmed;

                    await db.Users.UpdateAsync(u.Id, u);
                }
            }
        }

        public async Task SetTwoFactorEnabledAsync(IdentityUser user, bool enabled)
        {
            using (var conn =  ConnectionHelper.CreateDbConnection(ConnectionString))
            {
                var db = IdentityDatabase.Init(conn, _commandTimeout);

                var u =await db.Users.GetAsync(user.Id);

                if (u != null)
                {
                    u.TwoFactorEnabled = enabled;

                    await db.Users.UpdateAsync(u.Id, u);
                }
            }
        }

        public async Task<bool> GetTwoFactorEnabledAsync(IdentityUser user)
        {
            using (var conn =  ConnectionHelper.CreateDbConnection(ConnectionString))
            {
                var db = IdentityDatabase.Init(conn, _commandTimeout);

                return (await db.Users.GetAsync(user.Id)).TwoFactorEnabled;
            }
        }

        public async Task<DateTimeOffset> GetLockoutEndDateAsync(IdentityUser user)
        {
            return await GetUserField<DateTimeOffset>(user, "LockoutEndDateUtc");
        }

        public async Task SetLockoutEndDateAsync(IdentityUser user, DateTimeOffset lockoutEnd)
        {
            await SetUserField(user, "LockoutEndDateUtc", lockoutEnd);
        }

        public async Task<int> IncrementAccessFailedCountAsync(IdentityUser user)
        {          

            using (var conn =  ConnectionHelper.CreateDbConnection(ConnectionString))
            {
                var db = IdentityDatabase.Init(conn, _commandTimeout);

                var u =await db.Users.GetAsync(user.Id);
                if (u == null)
                {
                    throw new ArgumentException("Cannot find user.");
                }

                int result = u.AccessFailedCount++;

                await db.Users.UpdateAsync(u.Id, u);

                return result;
            }         
        }

        private async Task<T> GetUserField<T>(IdentityUser user, string fieldName)
        {
            T type = default(T);

            using (var conn =  ConnectionHelper.CreateDbConnection(ConnectionString))
            {
                var db = IdentityDatabase.Init(conn, _commandTimeout);

                var u =await db.Users.GetAsync(user.Id);
                if (u != null)
                {
                    var pi = user.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public).SingleOrDefault(x => x.Name == fieldName);
                    if (pi == null)
                    {
                        throw new ArgumentException(string.Format("Property name {0} does not exist for type IdentityUser.", fieldName));
                    }

                    type = (T)pi.GetValue(u);
                }
            }

            return type;
        }

        private async Task SetUserField(IdentityUser user, string fieldName, object value)
        {
            var pi = user.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public).SingleOrDefault(x => x.Name == fieldName);
            if (pi == null)
            {
                throw new ArgumentException(string.Format("Property name {0} does not exist for type IdentityUser.", fieldName));
            }

            using (var conn =  ConnectionHelper.CreateDbConnection(ConnectionString))
            {
                var db = IdentityDatabase.Init(conn, _commandTimeout);

                var u =await db.Users.GetAsync(user.Id);
                if (u != null)
                {
                    pi.SetValue(u, value);

                    await db.Users.UpdateAsync(u.Id, u);
                }
            }
        }

        public async Task ResetAccessFailedCountAsync(IdentityUser user)
        {
            await SetUserField(user, "AccessFailedCount", 0);
        }

        public async Task<int> GetAccessFailedCountAsync(IdentityUser user)
        {
            return await GetUserField<int>(user, "AccessFailedCount");
        }

        public async Task<bool> GetLockoutEnabledAsync(IdentityUser user)
        {
            return await GetUserField<bool>(user, "LockoutEnabled");
        }

        public async Task SetLockoutEnabledAsync(IdentityUser user, bool enabled)
        {
            await SetUserField(user, "LockoutEnabled", enabled);
        }

        
    }
}