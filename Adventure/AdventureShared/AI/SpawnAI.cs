using System;

using SpriteKit;
using CoreGraphics;

namespace Adventure
{
	public class SpawnAI : ArtificialIntelligence
	{
		private const float MinimumHeroDistance = 2048;

		public SpawnAI (Character character)
			: base(character)
		{
		}

		public override void UpdateWithTimeSinceLastUpdate (double interval)
		{
			Cave cave = (Cave)Character;

			if (cave.Health <= 0)
				return;

			MultiplayerLayeredCharacterScene scene = cave.CharacterScene;

			nfloat closestHeroDistance = MinimumHeroDistance;
			var closestHeroPosition = CGPoint.Empty;

			var cavePosition = cave.Position;
			foreach (SKNode hero in scene.Heroes) {
				var heroPosition = hero.Position;
				var distance = GraphicsUtilities.DistanceBetweenCGPoints (cavePosition, heroPosition);
				if (distance < closestHeroDistance) {
					closestHeroDistance = distance;
					closestHeroPosition = heroPosition;
				}
			}

			var distScale = closestHeroDistance / MinimumHeroDistance;

			// Generate goblins more quickly if the closest hero is getting closer.
			cave.TimeUntilNextGenerate -= (float)interval;

			// Either time to generate or the hero is so close we need to respond ASAP!
			int goblinCount = cave.ActiveGoblins.Count;
			if (goblinCount < 1
			    || cave.TimeUntilNextGenerate <= 0
			    || (distScale < 0.35f && cave.TimeUntilNextGenerate > 5)) {
				if (goblinCount < 1 || (goblinCount < 4 && closestHeroPosition != CGPoint.Empty)
				    && scene.CanSee (closestHeroPosition, cave.Position)) {
					cave.Generate ();
				}
				cave.TimeUntilNextGenerate = 4 * distScale;
			}
		}
	}
}

