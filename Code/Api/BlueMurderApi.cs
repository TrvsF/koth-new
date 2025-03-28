using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace KOTH.Api;

public class BlueMurderApi : SingletonComponent<BlueMurderApi>
{
	private Dictionary<string, string> Headers = new();

	// TODO Don't know if there is a method in sandbox to see if we are running in the editor or live?
	private bool Dev = false;


	public bool Connected = false;

	private long SteamId
	{
		get
		{
			return Connection.Local.SteamId;
		}
	}


	private String Endpoint
	{
		get
		{
			return (Dev) ? "http://localhost:8080/api/v1/" : "https://blue-murder.com/api/v1/";
		}
	}

	private String AuthEndpoint
	{
		get
		{
			return (Dev) ? "http://localhost:8080/auth/" : "https://blue-murder.com/auth/";
		}
	}


	private record ClanTag(string clanTag, long steamId);

	public record Session
	{
		public string sessionId { get; set; }

		public string jwt { get; set; }
	};


	public Session CurrentSession;

	/// <summary>
	/// Registers a new player in the system.
	/// This method sends a request to the authentication endpoint to register a player
	/// using their Steam ID. If the registration is successful, it confirms the operation.
	/// </summary>
	/// <returns>
	/// A boolean value indicating whether the registration was successful or not.
	/// True if the player was registered successfully; otherwise, false.
	/// </returns>
	public async Task<bool> RegisterNewPlayer()
	{
		try
		{
			var response = await Http.RequestAsync(AuthEndpoint + "newPlayer", "POST",
				Http.CreateJsonContent(new { steamId = SteamId }));

			Connected = true;

			if (response.StatusCode == HttpStatusCode.Created)
			{
				Log.Info("Player registered successfully");
				return true;
			}

			Log.Error("Failed to register new player");

			return false;
		}
		catch (HttpRequestException e)
		{
			Connected = false;
		}

		return false;
	}


	/// Registers a new player session with the authentication service.
	/// This method attempts to register a new session for the player. If the player
	/// is not already registered, it will invoke the process to register the player
	/// first. Upon successful registration or if already authorized, it will establish
	/// a new session and update the current session details.
	/// <returns>
	/// A boolean value indicating whether the player session was successfully registered.
	/// Returns true if the session was registered successfully; otherwise, false.
	/// </returns>
	public async Task<bool> RegisterNewPlayerSession()
	{
		try
		{
			var response = await Http.RequestAsync(AuthEndpoint + $"newSession/{SteamId}");

			Connected = true;

			Log.Info("Registering new player session");


			if (response.StatusCode == HttpStatusCode.Unauthorized)
			{
				Log.Info("Registering new player:");
				bool register = await RegisterNewPlayer();

				if (!register)
				{
					Log.Error("Failed to register new player");
					return false;
				}

				return await RegisterNewPlayerSession();
			}

			if (response.StatusCode == HttpStatusCode.OK)
			{
				Log.Info("Player session registered successfully");
				var content = await response.Content.ReadAsStringAsync();
				CurrentSession = Json.Deserialize<Session>(content);
				Headers.Clear();
				Headers.Add("Authorization", $"Bearer {CurrentSession.jwt}");
				return true;
			}

			return false;
		}
		catch (HttpRequestException e)
		{
			Connected = false;
		}

		return false;
	}

	/// <summary>
	/// Removes the current player's session from the system.
	/// This operation communicates with the server to invalidate the session associated with the current player.
	/// </summary>
	/// <returns>
	/// Returns true if the session is removed successfully or if the session was not found.
	/// Returns false if the session removal fails due to an error.
	/// </returns>
	public async Task<bool> RemovePlayerSession()
	{
		if(!Connected) return false;
		try
		{
			var response = await Http.RequestAsync(AuthEndpoint + $"removeSession", "GET", null, Headers);

			if (response.StatusCode == HttpStatusCode.OK)
			{
				Log.Info("Player session removed successfully");
				return true;
			}

			if (response.StatusCode == HttpStatusCode.NotFound)
			{
				Log.Warning("Player session not found");
				return true;
			}

			Log.Error("Failed to remove player session");
			return false;
		}
		catch (HttpRequestException e)
		{
			Connected = false;
		}

		return false;
	}


	/// Retrieves the clan tag associated with a player's Steam ID.
	/// <param name="steamId">The Steam ID of the player for whom the clan tag is to be retrieved.</param>
	/// <returns>A string representing the player's clan tag if retrieved successfully; otherwise, an empty string.</returns>
	public async Task<string> GetPlayerClanTag(string steamId)
	{
		if(!Connected) return "";
		try
		{
			var response = await Http.RequestAsync(Endpoint + $"player/clanTag/{steamId}");

			if (response.StatusCode == HttpStatusCode.OK)
			{
				var content = await response.Content.ReadAsStringAsync();
				Log.Info($"Player clan tag retrieved successfully {content}");
				return content;
			}

			Log.Error($"Failed to get player clan tag for {steamId}");

			return "";
		}
		catch (HttpRequestException e)
		{
			Connected = false;
		}

		return "";
	}

	/// Sets the current player's clan tag.
	/// <param name="clanTag">The clan tag to assign to the current player. This should be a string adhering to any application-defined restrictions.</param>
	/// <returns>A task representing the asynchronous operation. The task's result is a boolean indicating whether the clan tag was successfully set.</returns>
	public async Task<bool> SetCurrentPlayerClanTag(string clanTag)
	{
		if(!Connected) return false;

		var model = new ClanTag(clanTag, SteamId);

		try
		{

			var response =
				await Http.RequestAsync(Endpoint + "player/clanTag", "POST", Http.CreateJsonContent(model), Headers);
			var responseString = await response.Content.ReadAsStringAsync();

			if (response.StatusCode == HttpStatusCode.Unauthorized)
			{
				Log.Warning("Player has no session");
				return false;
			}

			if (response.StatusCode == HttpStatusCode.BadRequest)
			{
				Log.Warning($"Player clan error: {responseString}");
				return false;
			}

			if (response.StatusCode == HttpStatusCode.Created)
			{
				Log.Info("Player clan tag set successfully");
				return true;
			}

			return false;
		}
		catch (HttpRequestException e)
		{
			Connected = false;
		}

		return false;
	}
}
