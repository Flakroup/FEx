namespace FEx.Extensions.Base.Models;

public class ExceptionHandlerOptions
{
    public bool? InformUser { get; set; }
    public bool Wait { get; set; }
    public bool DoNotReport { get; set; }
    public (string, object)[] Custom { get; set; }
}