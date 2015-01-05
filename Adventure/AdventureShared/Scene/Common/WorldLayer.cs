using System;

namespace Adventure
{
	// The layers in a scene
	public enum WorldLayer
	{
		Ground = 0,
		BelowCharacter,
		Character,
		AboveCharacter,
		Top,
		Count // this value contains value's count
	}
}