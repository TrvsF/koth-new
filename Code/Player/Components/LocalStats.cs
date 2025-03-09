using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KOTH;

public sealed class LocalStats : Component
{
	public Sandbox.Services.Stats.PlayerStats LocalStatsObject { get; private set; }
	public (string Name, double Value) KillsStat = ("kills", 0);

	protected override void OnAwake()
	{
		base.OnAwake();

		LocalStatsObject = Sandbox.Services.Stats.LocalPlayer;

		KillsStat.Value = LocalStatsObject.Get(KillsStat.Name).Sum;
	}

	public async Task RefreshAsync()
	{
		await LocalStatsObject.Refresh();
	}
}
