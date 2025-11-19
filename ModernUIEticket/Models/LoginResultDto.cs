public class LoginResultDto
{
    public int Code { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? CompanyId { get; set; }
    public string? CompanyName { get; set; }
    public string? CreateDate { get; set; }
    public string? Email { get; set; }
    public string? FullAuthorization { get; set; }
    public string? LastLoginDate { get; set; }
    public string? PasswordHash { get; set; }
    public string? Telephone { get; set; }
    public string? Type { get; set; }
    public string? ULock { get; set; }
    public int UserId { get; set; }
    public string? UserName { get; set; }
    public string? UStatus { get; set; }
}
