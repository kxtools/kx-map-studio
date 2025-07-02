using CommunityToolkit.Mvvm.ComponentModel;

using Gw2Sharp;
using Gw2Sharp.Models;

using System.Windows;

namespace KXMapStudio.App.Services;

public partial class MumbleService : ObservableObject, IDisposable
{
    private readonly Gw2Client _client;
    private CancellationTokenSource? _cts;

    [ObservableProperty]
    private Coordinates3 _playerPosition;

    [ObservableProperty]
    private Coordinates3 _cameraPosition;

    [ObservableProperty]
    private uint _currentMapId;

    [ObservableProperty]
    private string _characterName = string.Empty;

    [ObservableProperty]
    private bool _isAvailable;

    public MumbleService()
    {
        var connection = new Connection();
        _client = new Gw2Client(connection);
    }

    public void Start()
    {
        if (_cts != null)
        {
            return;
        }

        _cts = new CancellationTokenSource();
        Task.Run(() => PollMumbleLink(_cts.Token));
    }

    public void Stop()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
    }

    private async Task PollMumbleLink(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            _client.Mumble.Update();
            bool currentIsAvailable = _client.Mumble.IsAvailable;

            // Marshal UI updates to the main thread.
            Application.Current.Dispatcher.Invoke(() =>
            {
                IsAvailable = currentIsAvailable;

                if (IsAvailable)
                {
                    PlayerPosition = _client.Mumble.AvatarPosition;
                    CameraPosition = _client.Mumble.CameraPosition;
                    CurrentMapId = (uint)_client.Mumble.MapId;
                    CharacterName = _client.Mumble.CharacterName;
                }
                else
                {
                    PlayerPosition = new Coordinates3();
                    CameraPosition = new Coordinates3();
                    CurrentMapId = 0;
                    CharacterName = "Not Available";
                }
            });

            try
            {
                await Task.Delay(100, cancellationToken);
            }
            catch (TaskCanceledException)
            {
                break;
            }
        }
    }

    public void Dispose()
    {
        Stop();
        _client.Dispose();
        GC.SuppressFinalize(this);
    }
}
