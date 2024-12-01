using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KOTH;

public sealed class LocalStats : SingletonComponent<LocalStats>
{
	//private struct Stat
	//{
	//	public Stat(string NameIn, Action<double> SetActionIn)
	//	{
	//		Name = NameIn;
	//		SetAction = SetActionIn;
	//	}

	//	public string Name { get; init; } = null;
	//	public Action<double> SetAction { get; init; } = null;
	//	public Action<double> GetAction { get; init; } = null;
	//}

	public Sandbox.Services.Stats.PlayerStats LocalStatsObject { get; private set; }
	readonly Dictionary<string, Action<double>> StatActions;

	// !! EVERY STAT /////////////
	//public static readonly List<string> Stats = new()
	//{
	//	"has-gold",
	//	"has-hat",
	//	"player-banner-colour",
	//	"clan-tag",
	//};
	//////////////////////////////

	public LocalStats()
	{
		StatActions = new Dictionary<string, Action<double>>
		{
			{ "has-gold", (Value) => SetHasGold(Value) },
			{ "has-hat", (Value) => SetHasHat(Value) },
			{ "player-banner-colour", (Value) => SetBannerColour(Value) },
			{ "clan-tag", (Value) => SetClanTag(Value) },
		};
	}

	public bool HasHat { get; private set; } = false;
	public bool HasGold { get; private set; } = false;
	public Color BannerColour { get; private set; } = Color.Black;
	public string ClanTag { get; private set; } = "";
	
	// set out data //////////////////////////////////////////////////

	private void SetHasHat(double Value)
	{
		HasHat = Value != 0;
	}

	private void SetHasGold(double Value)
	{
		HasGold = Value != 0;
	}

	private void SetBannerColour(double Value)
	{
		var ColourParsed = Color.FromRgb((uint) Value);

		Log.Info($"new colour '{ColourParsed}'");
		BannerColour = ColourParsed;
	}

	private void SetClanTag(double Value)
	{
		Log.Info($"data in from stats obj = {Value}");

		DecodeClantag((ulong)Value, out string ClantagOut);
		ClanTag = ClantagOut;
	}

	// externally set the web stats ///////////////////////////////////

	public void SetGoldStat(bool Gold)
	{
		if (!Network.IsOwner)
		{
			Log.Warning($"trying to set gold that aren't theirs");
			return;
		}

		Sandbox.Services.Stats.SetValue("has-gold", Gold ? 1 : 0);
		Sandbox.Services.Stats.Flush();
	}

	public void SetBannerColourStat(Color Colour)
	{
		if (!Network.IsOwner)
		{
			Log.Warning($"trying to set banner that aren't theirs");
			return;
		}

		Log.Info($"setting colour as {Colour.RgbInt}");
		Sandbox.Services.Stats.SetValue("player-banner-colour", (double) Colour.RgbInt);
		Sandbox.Services.Stats.Flush();
	}

	public void SetClanTagStat(string ClanTag)
	{
		if (!Network.IsOwner)
		{
			Log.Warning($"trying to set clan tag that aren't theirs");
			return;
		}

		if (!EncodeClantag(ClanTag, out ulong ClantagData))
		{
			Log.Warning($"clantag not valid '{ClanTag}'");
			return;
		}

		ClantagData = 100;
		Log.Info($"CLANTAG DATA = {ClantagData}");
		Sandbox.Services.Stats.SetValue("clan-tag", 100);
		Sandbox.Services.Stats.Flush();
	}

	public void SetLocalStatsObject(Sandbox.Services.Stats.PlayerStats LocalStatsObjectIn)
	{
		if (!Network.IsOwner)
		{
			Log.Warning($"trying to set stats that aren't theirs");
			return;
		}

		SetBannerColourStat(Color.Red);
		SetGoldStat(true);
		SetClanTagStat("hellwrld");

		LocalStatsObject = LocalStatsObjectIn;
		RefreshStats();
	}

	///////////////////////////////////////////////////////////////////////////////

	public void RefreshStats()
	{
		foreach (var LocalStat in LocalStatsObject)
		{
			var StatName = LocalStat.Name;
			if (StatActions.TryGetValue(StatName, out Action<double> SetStat))
			{
				SetStat(LocalStat.LastValue);
			}
		}
	}

	// TODO : move me /////////////////////////////////////////////////////////////

	const ulong DataError = 0;

	static bool EncodeClantag(string Clantag, out ulong EncodedClantag)
	{
		Log.Info($"CLANTAG IN = {Clantag}");

		EncodedClantag = DataError;
		byte[] ClantagAsciiBytes = Encoding.ASCII.GetBytes(Clantag);
		ulong Convert = BitConverter.ToUInt64(ClantagAsciiBytes);

		if (ClantagAsciiBytes.Length < 0 || ClantagAsciiBytes.Length > 8)
		{
			return false;
		}

		for (int ClantagByte = 0; ClantagByte < ClantagAsciiBytes.Length; ClantagByte++)
		{
			EncodedClantag |= ((ulong)ClantagAsciiBytes[ClantagByte] << (8 * ClantagByte));
		}

		Log.Info($"CLANTAG CAAAAA = {Convert}");
		Log.Info($"CLANTAG ENCODE = {EncodedClantag}");

		return !(EncodedClantag == DataError);
	}

	static bool DecodeClantag(ulong EncodedClantag, out string Clantag)
	{
		Log.Info($"Data In From Decode = {EncodedClantag}");

		char[] OutBytes = new char[8];
		for (int i = 0; i < 8; i++)
		{
			byte EncodedByte = (byte)(EncodedClantag >> (8 * i) & 0xFF);
			OutBytes[i] = (char)EncodedByte;
		}

		Clantag = new string(OutBytes);

		Log.Info($"CLANTAG Out Bytes = {OutBytes}");
		Log.Info($"CLANTAG DECODE = {Clantag}");

		return true;
	}
}
