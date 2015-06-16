using System.Collections.Generic;
using Brade.AspNet.Identity.Dapper.Dapper.Stores;
using Brade.AspNet.Identity.Identity;
using Safi.AspNet.Identity.Common;

namespace Brade.AspNet.Identity.Dapper.Dapper
{
    public class IdentityDatabase : Database<IdentityDatabase>
    {
        public IdentityDatabase()
        {
            Roles = new Table<IdentityRole, int,List<string>>(this,"Roles",new List<string>{"Id"});
            Users = new Table<IdentityUser, int, List<string>>(this, "Users", new List<string> { "Id" });
            UserRoles = new Table<IdentityUserRole, int, List<string>>(this, "UserRoles", new List<string> { "Id" });
            UserLogins = new Table<IdentityUserLogin, int, List<string>>(this, "UserLogins", new List<string> { "Id" });
            UserClaims = new Table<IdentityUserClaim, int, List<string>>(this, "UserClaims", new List<string> { "Id" });
        }        
        
        public Table<IdentityRole, int, List<string>> Roles { get; set; }

        public Table<IdentityUser, int, List<string>> Users { get; set; }

        public Table<IdentityUserRole, int, List<string>> UserRoles { get; set; }

        public Table<IdentityUserLogin, int, List<string>> UserLogins { get; set; }

        public Table<IdentityUserClaim, int, List<string>> UserClaims { get; set; }

    }


}

