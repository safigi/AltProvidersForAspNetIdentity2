using System.Linq;
using System.Threading.Tasks;
using Brade.AspNet.Identity.Identity;
using Dapper;
using Safi.AspNet.Identity.Common;

namespace Brade.AspNet.Identity.Dapper.Dapper.Stores
{
    public class RoleStore : IQueryableRoleStore<IdentityRole>
    {
        public string ConnectionString { get; set; }

        public RoleStore(string connectionString)
        {
            ConnectionString = connectionString;
        }

        public void Dispose()
        {            
        }

        public async Task CreateAsync(IdentityRole role)
        {
            using (var conn = ConnectionHelper.CreateDbConnection(ConnectionString))
            {
                var db = IdentityDatabase.Init(conn, 2);
                
                await db.Roles.InsertAsync(role);
            }
        }

        public async Task UpdateAsync(IdentityRole role)
        {
            using (var conn = ConnectionHelper.CreateDbConnection(ConnectionString))
            {
                var db = IdentityDatabase.Init(conn, 2);
                
                await db.Roles.UpdateAsync(role.Id, role);
            }
        }

        public async Task DeleteAsync(IdentityRole role)
        {
            using (var conn = ConnectionHelper.CreateDbConnection(ConnectionString))
            {
                var db = IdentityDatabase.Init(conn, 2);

                await db.Roles.DeleteAsync(role.Id);
            }
        }

        public async Task<IdentityRole> FindByIdAsync(int roleId)
        {
            using (var conn = ConnectionHelper.CreateDbConnection(ConnectionString))
            {
                return (await conn.QueryAsync<IdentityRole>(@"select * from dbo.Roles where Id=@Id", new { Id = roleId })).SingleOrDefault();
            }
        }     

        public async Task<IdentityRole> FindByNameAsync(string roleName)
        {
            using (var conn = ConnectionHelper.CreateDbConnection(ConnectionString))
            {
                return (await conn.QueryAsync<IdentityRole>(@"select * from dbo.Roles where Name=@Name", new { Name = roleName })).SingleOrDefault();
            }
        }
      
        public IQueryable<IdentityRole> Roles
        {
            get
            {
                using (var conn = ConnectionHelper.CreateDbConnection(ConnectionString))
                {
                    return (conn.Query<IdentityRole>(@"select * from dbo.Roles").AsQueryable());
                }
            }
        }
    }
}