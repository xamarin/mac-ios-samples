using System;

namespace Adventure
{
	// Bitmask for the different entities with physics bodies.
	[Flags]
	public enum ColliderType {
		Hero             = 1,
		GoblinOrBoss     = 2,
		Projectile       = 4,
		Wall             = 8,
		Cave             = 16
	}
}

