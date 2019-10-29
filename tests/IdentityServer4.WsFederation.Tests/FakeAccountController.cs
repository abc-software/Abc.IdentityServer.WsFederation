using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using IdentityModel;
using IdentityServer4.Test;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;

namespace IdentityServer4.WsFederation.Tests
{
    public class FakeAccountController : Controller
    {
        private readonly TestUserStore _userStore;

        public FakeAccountController(TestUserStore userStore)
        {
            _userStore = userStore;
            
        }

        [HttpGet]
        [Route("account/login")]
        public async Task<IActionResult> Login(string subjectId, string returnUrl)
        {
            var user = _userStore.FindBySubjectId(subjectId);
            var identity = new ClaimsIdentity(user.Claims.ToList(), "Fake IdP", JwtClaimTypes.Name, JwtClaimTypes.Role);
            var principal = new ClaimsPrincipal(identity);
            await HttpContext.SignInAsync("idsrv", principal);
            if(Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);
            return Ok();
        }
    }
}