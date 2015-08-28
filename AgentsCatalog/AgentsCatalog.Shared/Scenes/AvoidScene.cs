using CoreGraphics;
using GameplayKit;
using OpenTK;
using SpriteKit;

#if TARGET_OS_IOS
using UIKit;
using SKColor = UIKit.UIColor;
#elif TARGET_OS_MAC
using AppKit;
using SKColor = AppKit.NSColor;
#endif

namespace AgentsCatalog.Shared.Scenes {
	public class AvoidScene : GameScene {
		public AgentNode Player { get; set; }

		public GKGoal SeekGoal { get; set; }

		public override string SceneName => "AVOID OBSTACLES";

		public override bool Seeking {
			get {
				return base.Seeking;
			}
			set {
				base.Seeking = value;
				Player.Agent.Behavior.SetWeight (Seeking ? 1f : 0f, SeekGoal);
				Player.Agent.Behavior.SetWeight (Seeking ? 0f : 1f, StopGoal);
			}
		}

		public AvoidScene (CGSize size) : base(size)
		{
		}

		public override void DidMoveToView (SKView view)
		{
			base.DidMoveToView (view);

			var obstacles = new [] {
				AddObstacle (new CGPoint (Frame.GetMidX (), Frame.GetMidY () + 150f)),
				AddObstacle (new CGPoint (Frame.GetMidX () - 200f, Frame.GetMidY () - 150f)),
				AddObstacle (new CGPoint (Frame.GetMidX () + 200f, Frame.GetMidY () - 150f))
			};

			Player = new AgentNode (this, DefaultAgentRadius, new CGPoint (Frame.GetMidX (), Frame.GetMidY ()));
			Player.Agent.Behavior = new GKBehavior ();
			AgentSystem.AddComponent (Player.Agent);
			SeekGoal = GKGoal.GetGoalToSeekAgent (TrackingAgent);
			Player.Agent.Behavior.SetWeight (100, GKGoal.GetGoalToAvoidObstacles (obstacles, 1));
		}

		GKObstacle AddObstacle(CGPoint point) 
		{
			var circleShape = SKShapeNode.FromCircle (DefaultAgentRadius);
			circleShape.LineWidth = 2.5f;
			circleShape.FillColor = SKColor.Gray;
			circleShape.StrokeColor = SKColor.Red;
			circleShape.ZPosition = 1;
			circleShape.Position = point;
			AddChild (circleShape);

			var obstacle = new GKCircleObstacle (DefaultAgentRadius);
			obstacle.Position = new Vector2((float)point.X, (float)point.Y);

			return obstacle;
		}
	}
}

