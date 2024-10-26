namespace Models;

public record class DeploymentOptions
{
    public string? WorkingDirectory { get; set; }
    public List<string>? FileList { get; set; }
    public string? ServerUrl { get; set; }
    public string? DBAuthenticationUser { get; set; } //user that will be user to authenticate. Could be current user if "Integrated Security"
    public string Logs { get; set; } = "";
}
