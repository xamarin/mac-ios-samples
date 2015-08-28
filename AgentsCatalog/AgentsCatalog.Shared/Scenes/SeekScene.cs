using CoreGraphics;
using GameplayKit;

namespace AgentsCatalog.Shared.Scenes {
	public class SeekScene : GameScene {

		public AgentNode Player { get; set; }

		public GKGoal SeekGoal { get; set; }

		public override string SceneName => "SEEKING";

		public override bool Seeking {
			get {
				return base.Seeking;
			}
			set {
				base.Seeking = value;
				Player.Agent.Behavior.SetWeight (base.Seeking ? 1 : 0, SeekGoal);
				Player.Agent.Behavior.SetWeight (base.Seeking ? 0 : 1, StopGoal);
			}
		}

		public SeekScene (CGSize size) : base(size)
		{
		}

		public override void DidMoveToView (SpriteKit.SKView view)
		{
			base.DidMoveToView (view);
			Player = new AgentNode (this, DefaultAgentRadius, new CGPoint (Frame.GetMidX (), Frame.GetMidY ()));
			Player.Agent.Behavior = new GKBehavior ();
			AgentSystem.AddComponent (Player.Agent);
			SeekGoal = GKGoal.GetGoalToSeekAgent (TrackingAgent);
		}
	}
}

