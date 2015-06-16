using Microsoft.AspNet.Identity;

namespace Brade.AspNet.Identity
{
    public interface IQueryableRoleStore<TRole> : IQueryableRoleStore<TRole, int> where TRole : IRole<int>
    {

    }    
}
