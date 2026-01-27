namespace FunkoApi.DTO.User;

public record UserDTO(
    long Id,
    
    string Username,
    
    string Email,
    
    string Role,
    
    DateTime CreatedAt
    );