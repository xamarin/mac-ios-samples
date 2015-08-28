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

namespace AgentsCatalog.Shared {
	public class AgentNode : SKNode, IGKAgentDelegate {

		public GKAgent2D Agent { get; private set; }

		public SKColor Color { 
			get { 
				return TriangleShape.StrokeColor;
			} 
			set { 
				TriangleShape.StrokeColor = value; 
			} 
		}

		bool drawsTail;
		public bool DrawsTail {
			get {
				return drawsTail;
			}
			set {
				drawsTail = value;
				Particles.ParticleBirthRate = drawsTail ? DefaultParticleRate : 0;
			}
		}

		public SKShapeNode TriangleShape { get; set; }

		public SKEmitterNode Particles { get; set; }

		public float DefaultParticleRate { get; set; }

		public AgentNode (SKScene scene, float radius, CGPoint position)
		{
			InitAgent (scene, radius, position);
		}

		public void InitAgent(SKScene scene, float radius, CGPoint position)
		{
			Position = position;
			ZPosition = 10f;
			scene.AddChild (this);

			Agent = new GKAgent2D {
				Radius = radius,
				Position = new Vector2 ((float)position.X, (float)position.Y),
				Delegate = this,
				MaxSpeed = 100f,
				MaxAcceleration = 50f
			};

			var circleShape = SKShapeNode.FromCircle (radius);
			circleShape.LineWidth = 2.5f;
			circleShape.FillColor = SKColor.Gray;
			circleShape.ZPosition = 1f;
			AddChild (circleShape);

			const float triangleBackSideAngle = (float)((135f / 360f) * (2 * Math.PI));
			var points = new [] {
				new CGPoint (radius, 0f), // Tip
				new CGPoint (radius * Math.Cos (triangleBackSideAngle), radius * Math.Sin (triangleBackSideAngle)), // Back bottom
				new CGPoint (radius * Math.Cos (triangleBackSideAngle), -radius * Math.Sin (triangleBackSideAngle)), // Back top
				new CGPoint (radius, 0f) // Back top
			};

			TriangleShape = SKShapeNode.FromPoints (ref points [0], (nuint)points.Length);
			TriangleShape.LineWidth = 2.5f;
			TriangleShape.ZPosition = 1f;
			AddChild (TriangleShape);

			Particles = SKNode.FromFile<SKEmitterNode> ("Trail.sks");
			DefaultParticleRate = (float)Particles.ParticleBirthRate;
			Particles.Position = new CGPoint (-radius + 5, 0);
			Particles.TargetNode = scene;
			Particles.ZPosition = 0f;
			AddChild (Particles);
		}

		[Foundation.Export ("agentWillUpdate:")]
		public void AgentWillUpdate (GKAgent agent)
		{
		}

		[Foundation.Export ("agentDidUpdate:")]
		public void AgentDidUpdate (GKAgent2D agent)
		{
			Position = new CGPoint (agent.Position.X, agent.Position.Y);
			ZRotation = agent.Rotation;
		}
	}
}

