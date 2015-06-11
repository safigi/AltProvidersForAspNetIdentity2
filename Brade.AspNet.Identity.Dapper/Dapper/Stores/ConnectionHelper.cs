
using System.Configuration;
using System.Data;
using System.Data.Common;
using System.Linq;

namespace Brade.AspNet.Identity.Dapper.Dapper.Stores
{
    public static class ConnectionHelper
    {
        //public static DbConnection CreateDbConnection(string connectionString)
        //{
            
        //    var fact = DbProviderFactories.GetFactory(connectionString);
        //    return fact.CreateConnection();
        //}

        public static DbConnection CreateDbConnection(string connStr, string providerName = "System.Data.SqlClient")
        {
            
            var csb = new DbConnectionStringBuilder { ConnectionString = connStr };

            if (csb.ContainsKey("provider"))
            {
                providerName = csb["provider"].ToString();
            }
            else
            {
                var css = ConfigurationManager
                                  .ConnectionStrings
                                  .Cast<ConnectionStringSettings>()
                                  .FirstOrDefault(x => x.ConnectionString == connStr);
                if (css != null) providerName = css.ProviderName;
            }

            if (providerName != null)
            {
                var providerExists = DbProviderFactories
                                            .GetFactoryClasses()
                                            .Rows.Cast<DataRow>()
                                            .Any(r => r[2].Equals(providerName));
                if (providerExists)
                {
                    var factory = DbProviderFactories.GetFactory(providerName);
                    var dbConnection = factory.CreateConnection();

                    dbConnection.ConnectionString = connStr;
                    return dbConnection;
                }
            }

            return null;
        }
    }
}