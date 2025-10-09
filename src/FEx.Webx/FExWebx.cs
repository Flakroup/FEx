using System.Net;

namespace FEx.Webx;

public class FExWebx
{
    public static int DefaultConnectionLimit { get => ServicePointManager.DefaultConnectionLimit; set => ServicePointManager.DefaultConnectionLimit = value; }
}