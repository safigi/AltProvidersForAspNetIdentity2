using System;
using Brade.AspNet.Identity.Dapper.Dapper;
using Brade.AspNet.Identity.Dapper.Dapper.Stores;
using Brade.AspNet.Identity.Identity;
using Microsoft.AspNet.Identity;

namespace Test
{
    class Program
    {
        static void Main(string[] args)
        {
            string pass = "pass";
            var hash = new PasswordHasher();
            var passhas = hash.HashPassword(pass);
            var userStore = new DapperUserStore(@"Data Source=(localdb)\ProjectsV12;Integrated Security=True;Pooling=False;Initial Catalog=TestDatabase_1");            

            DapperUserManager userManager = new DapperUserManager(userStore);
            var user = new IdentityUser{UserName = "akarki", 
                Email = "email@email.com",
                PasswordHash = passhas, 
                SecurityStamp = new Guid().ToString()};
           
            var res =userManager.Create(user);
            Console.WriteLine("success");
        }
    }
}
