using System.Security.Claims;
using WebMessenger.Api.Infrastructure.Interfaces;

namespace WebMessenger.Api.Infrastructure
{
    public class CurrentUser(IHttpContextAccessor accessor) : ICurrentUser
    {
        private readonly IHttpContextAccessor _accessor = accessor;

        public bool IsAuthenticated =>
            _accessor.HttpContext?.User?.Identity?.IsAuthenticated == true;

        public Guid Id
        {
            get
            {
                if (!IsAuthenticated)
                    throw new InvalidOperationException("User is not authenticated");
                var claim = _accessor.HttpContext!.User.FindFirst(ClaimTypes.NameIdentifier)
                            ?? throw new InvalidOperationException("NameIdentifier claim not found");
                return Guid.Parse(claim.Value);
            }
        }

        public string? Username =>
            _accessor.HttpContext?.User?.FindFirst(ClaimTypes.Name)?.Value
            ?? _accessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    }

}
