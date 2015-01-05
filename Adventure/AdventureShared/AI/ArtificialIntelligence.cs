using System;

namespace Adventure
{
	public abstract class ArtificialIntelligence
	{
		public Character Character { get; private set; }
		public Character Target { get; protected set; }

		public ArtificialIntelligence (Character character)
		{
			Character = character;
		}

		public void ClearTarget(Character target)
		{
			if (Target == target)
				Target = null;
		}

		public abstract void UpdateWithTimeSinceLastUpdate (double interval);
	}
}

