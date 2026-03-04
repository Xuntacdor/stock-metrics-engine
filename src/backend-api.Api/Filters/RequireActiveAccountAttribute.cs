using System.Security.Claims;
using backend_api.Api.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace backend_api.Api.Filters;

/// <summary>
/// ActionFilter kiểm tra trạng thái tài khoản trước khi cho phép thực hiện giao dịch.
/// Áp dụng lên các endpoint nhạy cảm: đặt lệnh, nạp/rút tiền, v.v.
///
/// Luồng:
/// Request → [Authorize] (kiểm tra JWT hợp lệ) → [RequireActiveAccount] (kiểm tra AccountStatus = ACTIVE)
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public class RequireActiveAccountAttribute : Attribute, IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var httpContext = context.HttpContext;

        // Lấy userId từ JWT claim
        var userId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            context.Result = new UnauthorizedObjectResult(new
            {
                code    = "UNAUTHORIZED",
                message = "Bạn chưa đăng nhập."
            });
            return;
        }

        // Truy vấn thông tin user từ DB (không cache để đảm bảo real-time)
        var userRepo = httpContext.RequestServices.GetRequiredService<IUserRepository>();
        var user = await userRepo.GetByIdAsync(userId);

        if (user == null)
        {
            context.Result = new UnauthorizedObjectResult(new
            {
                code    = "USER_NOT_FOUND",
                message = "Không tìm thấy tài khoản."
            });
            return;
        }

        if (user.AccountStatus != "ACTIVE")
        {
            var message = user.AccountStatus switch
            {
                "INACTIVE"  => "Tài khoản chưa được kích hoạt. Vui lòng hoàn thành xác minh danh tính (KYC) trước khi giao dịch.",
                "SUSPENDED" => "Tài khoản đã bị tạm khóa. Vui lòng liên hệ hỗ trợ.",
                _           => "Tài khoản không hợp lệ. Vui lòng liên hệ hỗ trợ."
            };

            context.Result = new ObjectResult(new
            {
                code          = "ACCOUNT_NOT_ACTIVE",
                accountStatus = user.AccountStatus,
                kycStatus     = user.KycStatus,
                message
            })
            {
                StatusCode = StatusCodes.Status403Forbidden
            };
            return;
        }

        // Tài khoản ACTIVE → cho phép đi tiếp
        await next();
    }
}
