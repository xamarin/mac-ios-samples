using System;
using CoreGraphics;

namespace Adventure
{
	public class ChaseAI : ArtificialIntelligence
	{
		private static readonly float EnemyAlertRadius = Character.CollisionRadius * 500;

		public float ChaseRadius { get; set; }
		public float MaxAlertRadius { get; set; }

		public ChaseAI (Character character)
			: base(character)
		{
			MaxAlertRadius = 2 * EnemyAlertRadius;
			ChaseRadius = 2 * Character.CollisionRadius;
		}

		public override void UpdateWithTimeSinceLastUpdate (double interval)
		{
			if (Character.Dying) {
				Target = null;
				return;
			}

			var position = Character.Position;
			MultiplayerLayeredCharacterScene scene = Character.CharacterScene;
			nfloat closestHeroDistance = float.MaxValue;

			// Find the closest living hero, if any, within our alert distance.
			foreach (HeroCharacter hero in scene.Heroes) {
				var heroPosition = hero.Position;
				var distance = GraphicsUtilities.DistanceBetweenCGPoints(position, heroPosition);
				if (distance < EnemyAlertRadius
				    && distance < closestHeroDistance
				    && !hero.Dying) {
					closestHeroDistance = distance;
					Target = hero;
				}
			}

			// If there's no target, don't do anything.
			if (Target == null)
				return;

			// Otherwise chase or attack the target, if it's near enough.
			var heroPos = Target.Position;
			float chaseRadius = ChaseRadius;

			if (closestHeroDistance > MaxAlertRadius) {
				Target = null;
			} else if (closestHeroDistance > ChaseRadius) {
				Character.MoveTowards (heroPos, interval);
			} else if (closestHeroDistance < ChaseRadius) {
				Character.FaceTo(heroPos);
				Character.PerformAttackAction();
			}
		}
	}
}

