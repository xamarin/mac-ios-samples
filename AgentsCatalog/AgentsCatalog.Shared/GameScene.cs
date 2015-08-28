using System;

using CoreGraphics;
using Foundation;
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

using AgentsCatalog.Shared.Scenes;

namespace AgentsCatalog.Shared {
	public abstract class GameScene : SKScene {
		public enum SceneType {
			Seek = 0,
			Wander,
			Flee,
			Avoid,
			Separate,
			Align,
			Flock,
			Path,
			Count
		}

		protected const float DefaultAgentRadius = 40.0f;

		double lastUpdateTime;

		public virtual string SceneName => "Default";

		public virtual bool Seeking { get; set; }

		public GKComponentSystem AgentSystem { get; private set; }

		public GKAgent2D TrackingAgent { get; private set; }

		GKGoal stopGoal;
		public GKGoal StopGoal { 
			get { 
				stopGoal = stopGoal ?? GKGoal.GetGoalToReachTargetSpeed (0);
				return stopGoal;
			}
			private set {
				stopGoal = value;
			}
		}

		protected GameScene (CGSize size) : base (size)
		{
		}

		public static GameScene SceneWith (SceneType sceneType, CGSize size)
		{
			switch (sceneType) {
			case SceneType.Seek:
				return new SeekScene (size);
			case SceneType.Wander:
				return new WanderScene (size);
			case SceneType.Flee:
				return new FleeScene (size);
			case SceneType.Avoid:
				return new AvoidScene (size);
			case SceneType.Separate:
				return new SeparateScene (size);
			case SceneType.Align:
				return new AlignScene (size);
			case SceneType.Flock:
				return new FlockScene (size);
			case SceneType.Path:
				return new PathScene (size);
			case SceneType.Count:
				throw new InvalidOperationException ("Cannot create scene from SceneType.Count");
			default:
				throw new InvalidOperationException ("Unknown Scene Type");
			}
		}

		public override void DidMoveToView (SKView view)
		{
			#if TARGET_OS_MAC
			var fontName = NSFont.SystemFontOfSize (65).FontName;
			var label = SKLabelNode.FromFont (fontName);
			label.Text = SceneName;
			label.FontSize = 65;
			label.HorizontalAlignmentMode = SKLabelHorizontalAlignmentMode.Left;
			label.VerticalAlignmentMode = SKLabelVerticalAlignmentMode.Top;
			label.Position = new CGPoint (Frame.GetMinX () + 10, Frame.GetMaxY () - 46);
			AddChild (label);
			#endif

			AgentSystem = new GKComponentSystem (typeof(GKAgent2D));
			TrackingAgent = new GKAgent2D ();
			TrackingAgent.Position = new Vector2 ((float)Frame.GetMidX (), (float)Frame.GetMidY ());
		}

		public override void Update (double currentTime)
		{
			if (lastUpdateTime == 0)
				lastUpdateTime = currentTime;

			var delta = currentTime - lastUpdateTime;
			lastUpdateTime = currentTime;
			AgentSystem.Update (delta);
		}


		#if TARGET_OS_IOS
		public override void TouchesBegan (NSSet touches, UIEvent evt)
		{
			Seeking = true;
		}

		public override void TouchesCancelled (NSSet touches, UIEvent evt)
		{
			Seeking = false;
		}

		public override void TouchesEnded (NSSet touches, UIEvent evt)
		{
			Seeking = false;
		}

		public override void TouchesMoved (NSSet touches, UIEvent evt)
		{
			var touch = (UITouch)touches.AnyObject;
			var position = touch.LocationInNode (this);
			TrackingAgent.Position = new Vector2 ((float)position.X, (float)position.Y);
		}

		#elif TARGET_OS_MAC
		public override void MouseDown(NSEvent theEvent)
		{
			Seeking = true;
		}

		public override void MouseUp(NSEvent theEvent)
		{
			Seeking = false;
		}

		public override void MouseDragged(NSEvent theEvent)
		{
			var position = theEvent.LocationInNode (this);
			TrackingAgent.Position = new Vector2((float)position.X, (float)position.Y);
		}
		#endif
	}
}

