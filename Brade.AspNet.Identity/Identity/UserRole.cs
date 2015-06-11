namespace Brade.AspNet.Identity.Identity
{
    public class UserRole<TPermissionKey, TUserKey, TRoleKey>
    {

        public TPermissionKey Id { get; set; }

        public TUserKey UserId { get; set; }

        public TRoleKey RoleId { get; set; }
    }
}