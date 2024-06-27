using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;
using cardscore_api.Data;
using Microsoft.EntityFrameworkCore;

namespace cardscore_api.Attributes
{
    public class IsAdminAttribute : TypeFilterAttribute
    {
        public IsAdminAttribute() : base(typeof(TestFilter))
        {
        }

        private class TestFilter : ActionFilterAttribute
        {
            private readonly DataContext _context;
            private readonly string _adminRoleName = "ADMIN";
            private readonly string _userIdHeaderName = "UserId";

            public TestFilter(DataContext context)
            {
                _context = context;
            }

            public override void OnActionExecuting(ActionExecutingContext context)
            {
                string userId = context.HttpContext.Request.Headers[_userIdHeaderName].FirstOrDefault();

                var user = _context.Users.Include(u => u.Role).SingleOrDefault(u => u.Id.ToString() == userId);

                if (user == null || user?.Role.Name != _adminRoleName)
                {
                    context.Result = new UnauthorizedResult();
                }
            }
        }


    }
}
