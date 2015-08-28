using System;

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
	public class PathScene : GameScene {

		public override string SceneName =>  "FOLLOW PATH";

		public PathScene (CGSize size) : base(size)
		{
		}

		public override void DidMoveToView (SKView view)
		{
			base.DidMoveToView (view);

			var follower = new AgentNode (this, DefaultAgentRadius, new CGPoint (Frame.GetMidX (), Frame.GetMidY ())) {
				Color = SKColor.Cyan
			};

			var center = new Vector2 ((float)Frame.GetMidX (), (float)Frame.GetMidY ());
			var points = new [] {
				new Vector2 (center.X, center.Y + 50),
				new Vector2 (center.X + 50, center.Y + 150),
				new Vector2 (center.X + 100, center.Y + 150),
				new Vector2 (center.X + 200, center.Y + 200),
				new Vector2 (center.X + 350, center.Y + 150),
				new Vector2 (center.X + 300, center.Y),
				new Vector2 (center.X, center.Y - 200),
				new Vector2 (center.X - 200, center.Y - 100),
				new Vector2 (center.X - 200, center.Y),
				new Vector2 (center.X - 100, center.Y + 50),
			};

			var path = GKPath.FromPoints (points, DefaultAgentRadius, true);

			follower.Agent.Behavior = GKBehavior.FromGoal (GKGoal.GetGoalToFollowPath (path, 1.5, true), 1);
			AgentSystem.AddComponent (follower.Agent);

			var cgPoints = new CGPoint[11];
			for (var i = 0; i < 10; i++)
				cgPoints [i] = new CGPoint (points [i].X, points [i].Y);

			cgPoints [10] = cgPoints [0];
			var pathShape = SKShapeNode.FromPoints (ref cgPoints [0], 11);
			pathShape.LineWidth = 2;
			pathShape.StrokeColor = SKColor.Magenta;
			AddChild (pathShape);
		}
	}
}

