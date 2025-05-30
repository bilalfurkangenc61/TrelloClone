using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;

namespace TrelloClone.Hubs
{
    // Bu sınıf gerçek zamanlı güncellemeleri yönetir
    [Authorize] // Sadece giriş yapmış kullanıcılar kullanabilir
    public class BoardHub : Hub
    {
        // Kullanıcı bir panoya katılır
        public async Task JoinBoard(string boardId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"Board_{boardId}");
        }

        // Kullanıcı panodan ayrılır
        public async Task LeaveBoard(string boardId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Board_{boardId}");
        }

        // Kart taşındığında diğer kullanıcılара bildirir
        public async Task CardMoved(string boardId, object cardData)
        {
            await Clients.Group($"Board_{boardId}").SendAsync("CardMoved", cardData);
        }

        // Yeni kart eklendiğinde bildirir
        public async Task CardAdded(string boardId, object cardData)
        {
            await Clients.Group($"Board_{boardId}").SendAsync("CardAdded", cardData);
        }

        // Kart güncellendiğinde bildirir
        public async Task CardUpdated(string boardId, object cardData)
        {
            await Clients.Group($"Board_{boardId}").SendAsync("CardUpdated", cardData);
        }

        // Kart silindiğinde bildirir
        public async Task CardDeleted(string boardId, string cardId)
        {
            await Clients.Group($"Board_{boardId}").SendAsync("CardDeleted", cardId);
        }

        // Liste eklendiğinde bildirir
        public async Task ListAdded(string boardId, object listData)
        {
            await Clients.Group($"Board_{boardId}").SendAsync("ListAdded", listData);
        }

        // Liste güncellendiğinde bildirir
        public async Task ListUpdated(string boardId, object listData)
        {
            await Clients.Group($"Board_{boardId}").SendAsync("ListUpdated", listData);
        }
    }
}