using Sandbox.Diagnostics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KOTH.Utils
{     
	internal static class WorldUtil
	{
		public static CharacterDefinition GetRandomCharacter()
		{
			var ClassList = GameMode.Instance.Components.Get<ClassList>();
			Assert.NotNull(ClassList);

			return ClassList.ClassDefinitions.FirstOrDefault();
		}
	}
}
