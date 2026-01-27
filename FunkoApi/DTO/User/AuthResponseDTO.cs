namespace FunkoApi.DTO.User;

public record AuthResponseDTO(
    string Token,
    
    UserDTO User
);