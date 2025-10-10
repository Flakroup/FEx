namespace FEx.Webx.Models;

public class ResponseResult
{
    /// <summary>
    ///     Gets a value indicating whether this instance is success.
    /// </summary>
    /// <value>
    ///     <c>true</c> if this instance is success; otherwise, <c>false</c>.
    /// </value>
    public bool IsSuccess { get; }

    /// <summary>
    ///     Gets the response body.
    /// </summary>
    /// <value>
    ///     The response body.
    /// </value>
    public string ResponseBody { get; }

    /// <summary>
    ///     Initializes a new instance of the <see cref="ResponseResult" /> class.
    /// </summary>
    /// <param name="isSuccess">if set to <c>true</c> [is success].</param>
    /// <param name="responseBody">The response body.</param>
    public ResponseResult(bool isSuccess, string responseBody)
    {
        IsSuccess = isSuccess;
        ResponseBody = responseBody;
    }
}