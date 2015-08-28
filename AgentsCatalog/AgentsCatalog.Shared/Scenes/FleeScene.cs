using System;

using CoreGraphics;
using GameplayKit;

#if TARGET_OS_IOS
using UIKit;
using SKColor = UIKit.UIColor;
#elif TARGET_OS_MAC
using AppKit;
using SKColor = AppKit.NSColor;
#endif

namespace AgentsCatalog.Shared.Scenes {
	public class FleeScene : GameScene {
		public AgentNode Player { get; set; }

		public AgentNode Enemy { get; set; }

		public GKGoal SeekGoal { get; set; }

		public GKGoal FleeGoal { get; set; }

		public override string SceneName => "FLEEING";

		public override bool Seeking {
			get {
				return base.Seeking;
			}
			set {
				base.Seeking = value;
				Player.Agent.Behavior.SetWeight (Seeking ? 1 : 0, SeekGoal);
				Player.Agent.Behavior.SetWeight (Seeking ? 0 : 1, StopGoal);
			}
		}

		bool fleeing;
		public bool Fleeing {
			get {
				return fleeing;
			}
			set {
				fleeing = value;
				Enemy.Agent.Behavior.SetWeight (fleeing ? 1 : 0, FleeGoal);
				Enemy.Agent.Behavior.SetWeight (fleeing ? 0 : 1, StopGoal);
			}
		}

		public FleeScene (CGSize size) : base(size)
		{
		}

		public override void DidMoveToView (SpriteKit.SKView view)
		{
			base.DidMoveToView (view);

			Player = new AgentNode (this, DefaultAgentRadius, new CGPoint (Frame.GetMidX () - 150f, Frame.GetMidY ()));
			Player.Agent.Behavior = new GKBehavior ();
			AgentSystem.AddComponent (Player.Agent);

			Enemy = new AgentNode (this, DefaultAgentRadius, new CGPoint (Frame.GetMidX () + 150f, Frame.GetMidY ()));
			Enemy.Color = SKColor.Red;
			Enemy.Agent.Behavior = new GKBehavior ();
			AgentSystem.AddComponent (Enemy.Agent);

			SeekGoal = GKGoal.GetGoalToSeekAgent (TrackingAgent);
			FleeGoal = GKGoal.GetGoalToFleeAgent (Player.Agent);
		}

		public override void Update (double currentTime)
		{
			var distance = (Player.Agent.Position - Enemy.Agent.Position).Length;
			const float maxDistance = 200f;
			Fleeing = distance < maxDistance;
			base.Update (currentTime);
		}
	}
}

