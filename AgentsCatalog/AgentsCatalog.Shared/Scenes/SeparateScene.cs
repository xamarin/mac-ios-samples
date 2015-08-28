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

namespace AgentsCatalog.Shared.Scenes
{
	public class SeparateScene : GameScene
	{
		public override string SceneName => "SEPARATION";

		public override bool Seeking {
			get {
				return base.Seeking;
			}
			set {
				base.Seeking = value;
				foreach (GKAgent2D agent in AgentSystem.Components) {
					agent.Behavior.SetWeight (Seeking ? 1 : 0, SeekGoal);
					agent.Behavior.SetWeight (Seeking ? 0 : 1, StopGoal);
				}
			}
		}

		public AgentNode Player { get; set; }

		public AgentNode[] Friends { get; set; }

		public GKGoal SeparateGoal { get; set; }

		public GKGoal SeekGoal { get; set; }

		public SeparateScene (CGSize size) : base(size)
		{
		}

		public override void DidMoveToView (SpriteKit.SKView view)
		{
			base.DidMoveToView (view);
			Player = new AgentNode (this, DefaultAgentRadius, new CGPoint (Frame.GetMidX (), Frame.GetMidY ()));
			Player.Agent.Behavior = new GKBehavior ();
			AgentSystem.AddComponent (Player.Agent);
			Player.Agent.MaxSpeed *= 1.2f;

			SeekGoal = GKGoal.GetGoalToSeekAgent (TrackingAgent);

			Friends = new [] {
				AddFriend (new CGPoint (Frame.GetMidX () - 150f, Frame.GetMidY ())),
				AddFriend (new CGPoint (Frame.GetMidX () + 150f, Frame.GetMidY ()))
			};
			SeparateGoal = GKGoal.GetGoalToSeparate (new [] { Player.Agent }, 100f, (float)(Math.PI * 2));
			var behavior = GKBehavior.FromGoal (SeparateGoal, 100f);

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

