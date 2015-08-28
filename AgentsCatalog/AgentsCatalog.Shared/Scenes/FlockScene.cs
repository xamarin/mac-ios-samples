using System;
using System.Collections.Generic;

using CoreGraphics;
using GameplayKit;
using SpriteKit;

namespace AgentsCatalog.Shared.Scenes {
	public class FlockScene : GameScene {

		public override string SceneName => "FLOCKING";

		public override bool Seeking {
			get {
				return base.Seeking;
			}
			set {
				base.Seeking = value;
				foreach (GKAgent2D agent in AgentSystem.Components)
					agent.Behavior.SetWeight (Convert.ToInt32 (Seeking), SeekGoal);
			}
		}

		public GKGoal SeekGoal { get; set; }

		public FlockScene (CGSize size) : base(size)
		{
		}

		public override void DidMoveToView (SKView view)
		{
			base.DidMoveToView (view);

			var agents = new List<GKAgent2D> (20);
			const int agentsPerRow = 4;
			for (int i = 0; i < agentsPerRow * agentsPerRow; i++) {
				var x = Frame.GetMidX () + i % agentsPerRow * 20;
				var y = Frame.GetMidY () + i / agentsPerRow * 20;
				var boid = new AgentNode (this, 10, new CGPoint (x, y));
				AgentSystem.AddComponent (boid.Agent);
				agents.Add (boid.Agent);
				boid.DrawsTail = false;
			}

			const float separationRadius = 0.553f * 50;
			const float separationAngle = (float)(3 * Math.PI / 4.0f);
			const float separationWeight = 10.0f;

			const float alignmentRadius = 0.83333f * 50;
			const float alignmentAngle = (float)(Math.PI / 4.0f);
			const float alignmentWeight = 12.66f;

			const float cohesionRadius = 1.0f * 100;
			const float cohesionAngle = (float)(Math.PI / 2.0f);
			const float cohesionWeight = 8.66f;

			// Separation, alignment, and cohesion goals combined cause the flock to move as a group.
			var behavior = new GKBehavior ();
			behavior.SetWeight (separationWeight, GKGoal.GetGoalToSeparate (agents.ToArray (), separationRadius, separationAngle));
			behavior.SetWeight (alignmentWeight, GKGoal.GetGoalToAlign (agents.ToArray (), alignmentRadius, alignmentAngle));
			behavior.SetWeight (cohesionWeight, GKGoal.GetGoalToCohere (agents.ToArray (), cohesionRadius, cohesionAngle));

			foreach (GKAgent2D agent in agents)
				agent.Behavior = behavior;

			SeekGoal = GKGoal.GetGoalToSeekAgent (TrackingAgent);
		}
	}
}

