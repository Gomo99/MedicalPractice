using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace MedicalPractice.Hubs
{
    [Authorize]
    public class NotificationHub : Hub
    {
        public override async Task OnConnectedAsync()
        {
            var employeeId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrEmpty(employeeId))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"employee-{employeeId}");
            }
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var employeeId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrEmpty(employeeId))
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"employee-{employeeId}");
            }
            await base.OnDisconnectedAsync(exception);
        }
    }
}