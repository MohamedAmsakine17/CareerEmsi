namespace CareerEMSI.Models;

using CareerEMSI.Models.Enums;


// ConnectionDto.cs
public class ConnectionDto
{
    public int ReceiverId { get; set; }
}

public class ConnectionResponseDto
{
    public int ConnectionId { get; set; }
    public bool Accept { get; set; }
}

public class ConnectionResultDto
{
    public int ConnectionId { get; set; }
    public int RequesterId { get; set; }
    public int ReceiverId { get; set; }
    public ConnectionStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public BasicUserDto Requester { get; set; }
    public BasicUserDto Receiver { get; set; }
}

public class BasicUserDto
{
    public int Id { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string ProfilePictureUrl { get; set; }
    public string Email { get; set; }
    public string ProfileBackgroundUrl { get; set; }
}