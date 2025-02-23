using Sandbox;
using System.Text.RegularExpressions;
using static KOTH.MapVoteSystem;

namespace KOTH;

public struct FTextChatMessage
{
	public FTextChatMessage()
	{
	}

	public string Message { get; init; } = "";
	public Team Team { get; init; } = Team.Unassigned;
	public PlayerState AuthorPlayerState { get; init; } = null;
	public ulong AuthorSteamID { get; init; } = 0;
	public TimeSince TimeSinceAdd { get; init; } = 0;

	public string GetChatMessage()
	{
		var Prefix = "";

		if (Team != Team.Unassigned)
		{
			Prefix += $"[{Team}] ";
		}

		if (AuthorPlayerState.IsValid())
		{
			Prefix += $"{AuthorPlayerState.SteamName}";
		}

		return $"{Prefix} : {Message}";
	}
}

public sealed class TextChat : SingletonComponent<TextChat>
{
	public static string TextFilterRegex { get => @"[^\p{L}\p{N}\s.,!?\-']"; }
	public TextChatBox InputBox { get; set; }

	[Sync] public NetList<FTextChatMessage> Messages { get; private set; }

	protected override void OnStart()
	{
		base.OnStart();

		if (Networking.IsHost)
		{
			Messages = new();
		}
	}

	public bool WantsAllMessages { get => InputBox.HasFocus; }

	public List<FTextChatMessage> GetVisableMessages()
	{
		List<FTextChatMessage> VisableMessages = new();
		foreach (var NetMessage in Messages)
		{
			if (NetMessage.TimeSinceAdd < 5f || WantsAllMessages)
			{
				VisableMessages.Add(NetMessage);
			}
		}

		return VisableMessages;
	}

	[Rpc.Host]
	public void ServerRequestMessage(FTextChatMessage TextChatMessage)
	{
		Log.Info($"server got message {TextChatMessage.Message} from {TextChatMessage.AuthorSteamID}");

		if (Regex.IsMatch(TextChatMessage.Message, TextFilterRegex))
		{
			Log.Warning($"steamid {TextChatMessage.AuthorSteamID} is feeding us shit");
			return;
		}

		Log.Info(Messages);

		// i'm a paranoid man
		if (Messages.Count > 100)
		{
			Messages.Clear();
		}
		//

		if (Messages.Count > 50)
		{
			Messages.RemoveAt(50);
		}

		Messages.Add(TextChatMessage);
	}
}

public sealed class TextChatBox : TextEntry
{
	private bool IsTeamChat = false;

	public void SendMessage()
	{
		var RawInput = Text;
		Text = string.Empty;

		var FilteredMessage = Regex.Replace(RawInput, TextChat.TextFilterRegex, "");
		if (RawInput == string.Empty)
		{
			return;
		}

		FTextChatMessage ChatMessage = new()
		{
			Message = FilteredMessage,
			AuthorPlayerState = PlayerState.Local,
			AuthorSteamID = PlayerState.Local.SteamId,
			Team = IsTeamChat ? PlayerState.Local.Team : Team.Unassigned,
		};

		TextChat.Instance.ServerRequestMessage(ChatMessage);
	}

	public override void OnButtonTyped(ButtonEvent e)
	{
		e.StopPropagation = true;

		var button = e.Button;

		if (button == "tab")
		{
			IsTeamChat = !IsTeamChat;
		}

		base.OnButtonTyped(e);
	}
}

