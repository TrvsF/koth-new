using Sandbox.Diagnostics;
using Scene = Sandbox.Scene;

namespace KOTH;

public static partial class GameObjectExtensions
{
	public static void CopyPropertiesTo(this Component src, Component dst)
	{
		var json = src.Serialize().AsObject();
		json.Remove("__guid");
		dst.DeserializeImmediately(json);
	}

	public static string GetScenePath(this GameObject go)
	{
		return go is Scene ? "" : $"{go.Parent.GetScenePath()}/{go.Name}";
	}
}
