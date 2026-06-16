using FEx.Agnostics.Abstractions.Extensions;
using FEx.Agnostics.BaseObjects;
using FEx.AzureDevOpsx.Extensions;
using FEx.AzureDevOpsx.Services;
using FEx.Core.Abstractions.Extensions;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;
using System.Windows.Media;

namespace FEx.AzureDevOpsx.Entities;

public class Owner : NotifyPropertyChanged
{
    private TfsEnvironment _tfsEnvironment;

    private string _id;
    private string _displayName;
    private string _uniqueName;
    private Uri _url;
    private Uri _imageUrl;
    private ImageSource _image;

    [JsonIgnore]
    public string EnvironmentId { get; internal set; }

    [JsonProperty("id")]
    public string Id
    {
        get => _id;
        set => SetProperty(ref _id, value);
    }

    [JsonProperty("displayName")]
    public string DisplayName
    {
        get => _displayName;
        set => SetProperty(ref _displayName, value);
    }

    [JsonProperty("uniqueName")]
    public string UniqueName
    {
        get => _uniqueName;
        set => SetProperty(ref _uniqueName, value);
    }

    [JsonProperty("url")]
    public Uri Url
    {
        get => _url;
        set => SetProperty(ref _url, value);
    }

    /// <summary>
    /// Gets or sets the owner image URL.
    /// </summary>
    /// <value>
    /// The image URL.
    /// </value>
    [JsonProperty("imageUrl")]
    public Uri ImageUrl
    {
        get => _imageUrl;
        set => SetProperty(ref _imageUrl, value);
    }

    [JsonIgnore]
    public ImageSource Image
    {
        get
        {
            if (_image == null
                && ImageUrl != null)
                _ = Task.Run(RefreshImage);

            return _image;
        }
        private set => SetProperty(ref _image, value);
    }

    [JsonIgnore]
    private TfsEnvironment TfsEnvironment
    {
        get
        {
            if (_tfsEnvironment == null
                || _tfsEnvironment.EnvironmentId != EnvironmentId)
                _tfsEnvironment =
                    TfsService.Instance.TfsEnvironments.FindInEnumerable(x => x.EnvironmentId == EnvironmentId);

            return _tfsEnvironment;
        }
    }

    public async void RefreshImage()
    {
        try
        {
            await RefreshImageAsync(TfsEnvironment);
        }
        catch (Exception ex)
        {
            ex.HandleException();
        }
    }

    public async Task RefreshImageAsync(TfsEnvironment env)
    {
        if (env != null)
            Image = await TfsExtensions.LoadImageAsync(ImageUrl, env.Server.Credentials);
    }
}