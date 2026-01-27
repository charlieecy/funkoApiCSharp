using Microsoft.AspNetCore.SignalR;

namespace FunkoApi.SignalR;

public class FunkoHub(ILogger<FunkoHub> log) : Hub
{
    // Context.ConnectionId - ID unico de la conexion
    // Context.User - Usuario autenticado (si aplica)
    // Groups - Gestion de grupos
    // Clients - Referencia a todos los clientes conectados

    public override async Task OnConnectedAsync()
    {
        await base.OnConnectedAsync();
        log.LogInformation("Cliente conectado: {ConnectionId}", Context.ConnectionId);
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await base.OnDisconnectedAsync(exception);
        log.LogInformation("Cliente desconectado: {ConnectionId}", Context.ConnectionId);
    }

    // Metodo que puede ser llamado por el cliente
    public async Task SendMessage(string user, string message)
    {
        // Enviar a todos los clientes
        await Clients.All.SendAsync("ReceiveMessage", user, message);
    }

    // Enviar a un usuario especifico (usando grupos)
    public async Task SendToUser(string targetUser, string message)
    {
        await Clients.Group($"user-{targetUser}").SendAsync("PrivateMessage", message);
    }

    // Unirse a un grupo
    public async Task JoinGroup(string groupName)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        await Clients.Group(groupName).SendAsync("UserJoined", Context.ConnectionId, groupName);
    }
}