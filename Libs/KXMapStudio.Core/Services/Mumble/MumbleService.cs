namespace KXMapStudio.Core.Services.Mumble;

public sealed record MumbleService : IMumbleService, IDisposable
{
	private const double MovementThreshold = 0.1; // Minimum distance to consider as movement
	private static readonly TimeSpan StaleTimeout = TimeSpan.FromSeconds(30); // 30s without movement = stale
	private static readonly TimeSpan DisconnectTimeout = TimeSpan.FromMinutes(2); // 2min stale = disconnect

	private readonly IGw2Client _gw2Client;
	private CancellationTokenSource? _cts;

	private volatile MumbleStateModel _current = new(
		MumbleConnectionState.Disconnected,
		false,
		new Position3D(),
		new Position3D(),
		0,
		"Not Available",
		DateTimeOffset.MinValue
	);

	public MumbleService(IGw2Client gw2Client)
	{
		_gw2Client = gw2Client;
	}

	public void Dispose()
	{
		Stop();
		_gw2Client.Dispose();
	}

	public MumbleStateModel Current => _current;
	public event EventHandler<MumbleStateModel>? MumbleUpdated;

	public void Start()
	{
		if (_cts != null) return;

		_cts = new CancellationTokenSource();
		Task.Run(() => PollLoop(_cts.Token));
	}

	public void Stop()
	{
		_cts?.Cancel();
		_cts?.Dispose();
		_cts = null;
	}

	private async Task PollLoop(CancellationToken ct)
	{
		var lastState = MumbleConnectionState.Disconnected;
		var lastPosition = new Position3D();
		var lastMovementTime = DateTimeOffset.MinValue;
		var lastAvailableTime = DateTimeOffset.MinValue;

		while (!ct.IsCancellationRequested)
		{
			try
			{
				_gw2Client.Mumble.Update();

				var available = _gw2Client.Mumble.IsAvailable;
				var mapId = (uint)_gw2Client.Mumble.MapId;
				var name = _gw2Client.Mumble.CharacterName ?? "";

				var avatar = _gw2Client.Mumble.AvatarPosition;
				var camera = _gw2Client.Mumble.CameraPosition;
				var playerPos = new Position3D(avatar.X, avatar.Y, avatar.Z);
				var cameraPos = new Position3D(camera.X, camera.Y, camera.Z);
				var now = DateTimeOffset.UtcNow;

				// Determine connection state
				MumbleConnectionState state;

				if (!available || mapId == 0 || string.IsNullOrWhiteSpace(name))
				{
					// Not available or invalid data
					state = MumbleConnectionState.Disconnected;
					lastMovementTime = DateTimeOffset.MinValue;
					lastAvailableTime = DateTimeOffset.MinValue;
				}
				else
				{
					// Available - update last available time
					if (lastAvailableTime == DateTimeOffset.MinValue)
						lastAvailableTime = now;

					// Check for movement
					var moved = HasMoved(lastPosition, playerPos);

					if (moved)
					{
						lastMovementTime = now;
						lastPosition = playerPos;
						state = MumbleConnectionState.Connected;
					}
					else
					{
						// No movement detected
						if (lastMovementTime == DateTimeOffset.MinValue)
						{
							// First time seeing this position, start stale timer
							lastMovementTime = now;
							state = MumbleConnectionState.Connected;
						}
						else
						{
							var timeSinceMovement = now - lastMovementTime;

							if (timeSinceMovement > DisconnectTimeout)
								// Too long without movement, consider disconnected
								state = MumbleConnectionState.Disconnected;
							else if (timeSinceMovement > StaleTimeout)
								// No movement for a while, mark as stale (AFK, menu, loading)
								state = MumbleConnectionState.Stale;
							else
								// Recently moved, still connected
								state = MumbleConnectionState.Connected;
						}
					}
				}

				// Create new state model
				var mumble = new MumbleStateModel(state, available, playerPos, cameraPos, mapId, name, now);

				// Track state changes for logging and event optimization
				var stateChanged = state != lastState;

				// Log state transitions only (avoid spam)
				if (stateChanged)
				{
					Console.WriteLine($"[Mumble] {lastState} → {state} | Map={mapId}, Name='{name}'");
					lastState = state;
				}

				// Always update current state
				_current = mumble;

				// Raise event based on state:
				// - Always raise for Connected/Stale (position updates needed)
				// - Only raise for Disconnected on state change (avoid redundant UI updates)
				if (state != MumbleConnectionState.Disconnected || stateChanged) MumbleUpdated?.Invoke(this, mumble);
			}
			catch (Exception ex)
			{
				Console.WriteLine($"[Mumble] Error: {ex.GetType().Name} - {ex.Message}");

				var mumble = new MumbleStateModel(
					MumbleConnectionState.Disconnected,
					false,
					new Position3D(),
					new Position3D(),
					0,
					"Not Available",
					DateTimeOffset.UtcNow
				);
				_current = mumble;
				MumbleUpdated?.Invoke(this, mumble);

				if (lastState != MumbleConnectionState.Disconnected)
				{
					Console.WriteLine("[Mumble] Disconnected due to error");
					lastState = MumbleConnectionState.Disconnected;
				}
			}

			try
			{
				await Task.Delay(100, ct);
			}
			catch
			{
				break;
			}
		}
	}

	private static bool HasMoved(Position3D oldPos, Position3D newPos)
	{
		var dx = newPos.X - oldPos.X;
		var dy = newPos.Y - oldPos.Y;
		var dz = newPos.Z - oldPos.Z;
		var distance = Math.Sqrt(dx * dx + dy * dy + dz * dz);
		return distance > MovementThreshold;
	}
}