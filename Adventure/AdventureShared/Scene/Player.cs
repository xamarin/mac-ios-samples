using System;

#if __IOS__
using UIKit;

#else
using AppKit;
#endif

using Foundation;
using ObjCRuntime;
using CoreGraphics;
using GameController;

namespace Adventure
{
	public class Player : NSObject
	{
		public static readonly int StartLives = 3;

		public HeroCharacter Hero { get; set; }

		public HeroType HeroType { get; set; }

		public bool MoveForward { get; set; }

		public bool MoveLeft { get; set; }

		public bool MoveRight { get; set; }

		public bool MoveBack { get; set; }

		public bool FireAction { get; set; }

		public CGPoint HeroMoveDirection { get; set; }

		public int LivesLeft { get; set; }

		public int Score  { get; set; }

		public GCController Controller  { get; set; }

		#if __IOS__
		public UITouch MovementTouch { get; set; }
		// track whether a touch is move or fire action
		public CGPoint TargetLocation { get; set; }
		// track target location
		public bool MoveRequested { get; set; }
		// track whether a move was requested
		#endif

		public Player ()
		{
			LivesLeft = StartLives;

			Random rnd = new Random ();
			HeroType = rnd.Next (1) == 0 ? HeroType.Warrior : HeroType.Archer;
		}
	}
}

