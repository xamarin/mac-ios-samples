using System;

using GameplayKit;
using CoreGraphics;

#if TARGET_OS_IOS
using UIKit;
using SKColor = UIKit.UIColor;
#elif TARGET_OS_MAC
using AppKit;
using SKColor = AppKit.NSColor;
#endif

namespace AgentsCatalog.Shared.Scenes {
	public class AlignScene : GameScene {
		public AgentNode Player { get; set; }

		public AgentNode[] Friends  { get; set; }

		public GKGoal AlignGoal  { get; set; }

		public GKGoal SeekGoal  { get; set; }

		public override string SceneName => "ALIGNMENT";

		public override bool Seeking {
			get {
				return base.Seeking;
			}
			set {
				base.Seeking = value;
				foreach (GKAgent2D agent in AgentSystem.Components) {
					agent.Behavior.SetWeight (Seeking ? 1f : 0f, SeekGoal);
					agent.Behavior.SetWeight (Seeking ? 0f : 1f, StopGoal);
				}
			}
		}

		public AlignScene (CGSize size) : base(size)
		{
		}

		public override void DidMoveToView (SpriteKit.SKView view)
		{
			base.DidMoveToView (view);

			Player = new AgentNode (this, DefaultAgentRadius, new CGPoint (Frame.GetMidX(), Frame.GetMidY()));
			Player.Agent.Behavior = new GKBehavior ();
			AgentSystem.AddComponent (Player.Agent);
			Player.Agent.MaxSpeed *= 1.2f;

			SeekGoal = GKGoal.GetGoalToSeekAgent (TrackingAgent);

			AlignGoal = GKGoal.GetGoalToAlign (new [] { Player.Agent }, 100, (float)(Math.PI * 2));
			var behavior = GKBehavior.FromGoal (AlignGoal, 100f);

			Friends = new [] {
				AddFriend (new CGPoint (Frame.GetMidX () - 150f, Frame.GetMidY ())),
				AddFriend (new CGPoint (Frame.GetMidX () + 150f, Frame.GetMidY ()))
			};

			foreach (var friend in Friends)
				friend.Agent.Behavior = behavior;
		}

		AgentNode AddFriend (CGPoint point)
		{
			var friend = new AgentNode (this, DefaultAgentRadius, point) {
				Color = SKColor.Cyan
			};
			AgentSystem.AddComponent (friend.Agent);
			return friend;
		}
	}
}

