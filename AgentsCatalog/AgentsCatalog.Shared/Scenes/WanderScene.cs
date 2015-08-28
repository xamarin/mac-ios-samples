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
	public class WanderScene : GameScene {

		public override string SceneName => "WANDERING";

		public WanderScene (CGSize size) : base(size)
		{
		}

		public override void DidMoveToView (SpriteKit.SKView view)
		{
			base.DidMoveToView (view);
			var wanderer = new AgentNode (this, DefaultAgentRadius, new CGPoint (Frame.GetMidX (), Frame.GetMidY ())) {
				Color = SKColor.Cyan
			};
			wanderer.Agent.Behavior = GKBehavior.FromGoal (GKGoal.GetGoalToWander(10), 100f);
			AgentSystem.AddComponent (wanderer.Agent);
		}
	}
}

