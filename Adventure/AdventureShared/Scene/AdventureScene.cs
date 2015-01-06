using System;
using SpriteKit;
using System.Diagnostics;
using System.Collections.Generic;

using CoreGraphics;

namespace Adventure
{
	public class AdventureScene : MultiplayerLayeredCharacterScene
	{
		const int WorldTileDivisor = 32;
		// number of tiles
		const int WorldSize = 4096;
		// pixel size of world (square)
		const int WorldTileSize = WorldSize / WorldTileDivisor;

		const int WorldCenter = 2048;

		const int LevelMapSize = 256;
		// pixel size of level map (square)
		const int LevelMapDivisor = WorldSize / LevelMapSize;

		readonly List<Cave> goblinCaves;
		readonly List<SKEmitterNode> particleSystems;
		readonly List<SKSpriteNode> parallaxSprites;
		readonly List<SKSpriteNode> trees;

		MapScaner levelMap;
		MapScaner treeMap;

		Boss levelBoss;
		// the big boss character

		static SKEmitterNode sharedProjectileSparkEmitter;

		SKEmitterNode SharedProjectileSparkEmitter {
			get {
				return sharedProjectileSparkEmitter;
			}
		}

		static SKEmitterNode sharedSpawnEmitter;

		protected override SKEmitterNode SharedSpawnEmitter {
			get {
				return sharedSpawnEmitter;
			}
		}

		static Tree sharedSmallTree;

		Tree SharedSmallTree {
			get {
				return sharedSmallTree;
			}
		}

		static Tree sharedBigTree;

		Tree SharedBigTree {
			get {
				return sharedBigTree;
			}
		}

		static SKEmitterNode sharedLeafEmitterA;

		SKEmitterNode SharedLeafEmitterA {
			get {
				return sharedLeafEmitterA;
			}
		}

		static SKEmitterNode sharedLeafEmitterB;

		SKEmitterNode SharedLeafEmitterB {
			get {
				return sharedLeafEmitterB;
			}
		}

		static SKNode[] backgroundTiles;

		SKNode[] BackgroundTiles {
			get {
				return backgroundTiles;
			}
		}

		HeroType heroType;

		public HeroType DefaultPlayerHeroType {
			get {
				return heroType;
			}
			set {
				heroType = value;
				DefaultPlayer.HeroType = heroType;
			}
		}

		Random random;

		Random Random {
			get {
				random = random ?? new Random ();
				return random;
			}
		}

		public AdventureScene (CGSize size)
			: base (size)
		{
			goblinCaves = new List<Cave> ();

			particleSystems = new List<SKEmitterNode> ();
			parallaxSprites = new List<SKSpriteNode> ();
			trees = new List<SKSpriteNode> ();
		}

		public override void Initialize ()
		{
			base.Initialize ();

			// Build level and tree maps from map_collision.png and map_foliage.png respectively.
			levelMap = GraphicsUtilities.CreateMapScaner ("map_level.png");
			treeMap = GraphicsUtilities.CreateMapScaner ("map_trees.png");

			Cave.GoblinCap = 32;

			BuildWorld ();

			// Center the camera on the hero spawn point.
			var startPosition = DefaultSpawnCGPoint;
			CenterWorld (startPosition);
		}

		#region World Building

		void BuildWorld ()
		{
			Console.WriteLine ("Building the world");

			// Configure physics for the world.
			PhysicsWorld.Gravity = new CGVector (); // no gravity
			PhysicsWorld.DidBeginContact += OnDidBeginContact;

			AddBackgroundTiles ();
			AddSpawnPoints ();
			AddTrees ();
			AddCollisionWalls ();
		}

		void AddBackgroundTiles ()
		{
			// Tiles should already have been pre-loaded in +loadSceneAssets.
			foreach (SKNode tileNode in BackgroundTiles)
				AddNode (tileNode, WorldLayer.Ground);
		}

		void AddSpawnPoints ()
		{
			// Add goblin caves and set hero/boss spawn points.
			for (int y = 0; y < LevelMapSize; y++) {
				for (int x = 0; x < LevelMapSize; x++) {
					var location = new CGPoint (x, y);
					DataMap spot = levelMap.QueryLevelMap (location);

					// Get the world space point for this level map pixel.
					CGPoint worldPoint = ConvertLevelMapPointToWorldPoint (location);

					if (spot.BossLocation <= 200) {
						levelBoss = new Boss (worldPoint);
						levelBoss.AddToScene (this);
					} else if (spot.GoblinCaveLocation >= 200) {
						var cave = new Cave (worldPoint);
						goblinCaves.Add (cave);
						parallaxSprites.Add (cave);
						cave.AddToScene (this);
					} else if (spot.HeroSpawnLocation >= 200) {
						DefaultSpawnCGPoint = worldPoint; // there's only one
					}
				}
			}
		}

		void AddTrees ()
		{
			for (int y = 0; y < LevelMapSize; y++) {
				for (int x = 0; x < LevelMapSize; x++) {
					var location = new CGPoint (x, y);
					var spot = treeMap.QueryTreeMap (location);

					var treePos = ConvertLevelMapPointToWorldPoint (location);
					WorldLayer treeLayer = WorldLayer.Top;
					Tree tree;

					if (spot.SmallTreeLocation >= 200) {
						// Create small tree at this location.
						treeLayer = WorldLayer.AboveCharacter;
						tree = (Tree)SharedSmallTree.Clone ();
					} else if (spot.BigTreeLocation >= 200) {
						// Create big tree with leaf emitters at this position.
						tree = (Tree)SharedBigTree.Clone ();

						// Pick one of the two leaf emitters for this tree.
						SKEmitterNode emitterToCopy = Random.Next (2) == 1 ? SharedLeafEmitterA : SharedLeafEmitterB;
						var emitter = (SKEmitterNode)emitterToCopy.Copy ();

						emitter.Position = treePos;
						emitter.Paused = true;
						AddNode (emitter, WorldLayer.AboveCharacter);
						particleSystems.Add (emitter);
					} else {
						continue;
					}

					tree.Position = treePos;
					tree.ZRotation = (float)(Random.NextDouble () * Math.PI * 2);
					AddNode (tree, treeLayer);
					parallaxSprites.Add (tree);
					trees.Add (tree);
				}
			}

			treeMap = null;
		}

		void AddCollisionWalls ()
		{
			var sw = new Stopwatch ();
			sw.Start ();

			var filled = new byte[LevelMapSize * LevelMapSize];

			int numVolumes = 0;
			int numBlocks = 0;

			// Add horizontal collision walls.
			for (int y = 0; y < LevelMapSize; y++) { // iterate in horizontal rows
				for (int x = 0; x < LevelMapSize; x++) {
					var location = new CGPoint (x, y);
					DataMap spot = levelMap.QueryLevelMap (location);

					// Get the world space point for this pixel.
					CGPoint worldPoint = ConvertLevelMapPointToWorldPoint (location);

					if (spot.Wall < 200)
						continue; // no wall

					int horizontalDistanceFromLeft = x;
					DataMap nextSpot = spot;
					while (horizontalDistanceFromLeft < LevelMapSize
					       && nextSpot.Wall >= 200 &&
					       filled [(y * LevelMapSize) + horizontalDistanceFromLeft] == 0) {
						horizontalDistanceFromLeft++;
						nextSpot = levelMap.QueryLevelMap (new CGPoint (horizontalDistanceFromLeft, y));
					}

					int wallWidth = horizontalDistanceFromLeft - x;
					int verticalDistanceFromTop = y;

					if (wallWidth > 8) {
						nextSpot = spot;
						while (verticalDistanceFromTop < LevelMapSize
						       && nextSpot.Wall >= 200) {
							verticalDistanceFromTop++;
							nextSpot = levelMap.QueryLevelMap (new CGPoint (x + (wallWidth / 2), verticalDistanceFromTop));
						}

						int wallHeight = (verticalDistanceFromTop - y);
						for (int j = y; j < verticalDistanceFromTop; j++) {
							for (int i = x; i < horizontalDistanceFromLeft; i++) {
								filled [j * LevelMapSize + i] = 255;
								numBlocks++;
							}
						}

						AddCollisionWallAtWorldPoint (worldPoint, LevelMapDivisor * wallWidth, LevelMapDivisor * wallHeight);
						numVolumes++;
					}
				}
			}

			// Add vertical collision walls.
			for (int x = 0; x < LevelMapSize; x++) { // iterate in vertical rows
				for (int y = 0; y < LevelMapSize; y++) {
					var location = new CGPoint (x, y);
					DataMap spot = levelMap.QueryLevelMap (location);

					// Get the world space point for this pixel.
					CGPoint worldPoint = ConvertLevelMapPointToWorldPoint (location);

					if (spot.Wall < 200 || filled [y * LevelMapSize + x] != 0)
						continue; // no wall, or already filled from X collision walls

					int verticalDistanceFromTop = y;
					DataMap nextSpot = spot;
					while (verticalDistanceFromTop < LevelMapSize
					       && nextSpot.Wall >= 200
					       && filled [verticalDistanceFromTop * LevelMapSize + x] == 0) {
						verticalDistanceFromTop++;
						nextSpot = levelMap.QueryLevelMap (new CGPoint (x, verticalDistanceFromTop));
					}

					int wallHeight = verticalDistanceFromTop - y;
					int horizontalDistanceFromLeft = x;

					if (wallHeight > 8) {
						nextSpot = spot;
						while (horizontalDistanceFromLeft < LevelMapSize
						       && nextSpot.Wall >= 200) {
							horizontalDistanceFromLeft++;
							nextSpot = levelMap.QueryLevelMap (new CGPoint (horizontalDistanceFromLeft, y + wallHeight / 2));
						}

						int wallLength = horizontalDistanceFromLeft - x;
						for (int j = y; j < verticalDistanceFromTop; j++) {
							for (int i = x; i < horizontalDistanceFromLeft; i++) {
								filled [j * LevelMapSize + i] = 255;
								numBlocks++;
							}
						}

						AddCollisionWallAtWorldPoint (worldPoint, LevelMapDivisor * wallLength, LevelMapDivisor * wallHeight);
						numVolumes++;
					}
				}
			}

			Console.WriteLine ("converted {0} collision blocks into {1} volumes in {2} seconds", numBlocks, numVolumes, sw.Elapsed.Seconds);
		}

		void AddCollisionWallAtWorldPoint (CGPoint worldPoint, float width, float height)
		{
			var rect = new CGRect (0, 0, width, height);

			var wallNode = SKNode.Create ();
			wallNode.Position = new CGPoint (worldPoint.X + rect.Size.Width * 0.5f, worldPoint.Y - rect.Size.Height * 0.5f);
			wallNode.PhysicsBody = SKPhysicsBody.CreateRectangularBody (rect.Size);
			wallNode.PhysicsBody.Dynamic = false;
			wallNode.PhysicsBody.CategoryBitMask = (uint)ColliderType.Wall;
			wallNode.PhysicsBody.CollisionBitMask = 0;

			AddNode (wallNode, WorldLayer.Ground);
		}

		#endregion

		#region Level Start

		public void StartLevel ()
		{
			HeroCharacter hero = AddHeroFor (DefaultPlayer);

#if MOVE_NEAR_TO_BOSS
			CGPoint bossPosition = _levelBoss.Position; // set earlier from buildWorld in addSpawnPoints
			bossPosition.X += 128;
			bossPosition.Y += 512;
			hero.Position = bossPosition;
#endif

			CenterWorld (hero);
		}

		#endregion

		#region Heroes

		public override void HeroWasKilled (HeroCharacter hero)
		{
			foreach (Cave cave in goblinCaves)
				cave.StopGoblinsFromTargettingHero (hero);

			base.HeroWasKilled (hero);
		}

		#endregion

		#region Loop Update

		public override void UpdateWithTimeSinceLastUpdate (double timeSinceLast)
		{
			// Update all players' heroes.
			foreach (HeroCharacter hero in Heroes)
				hero.UpdateWithTimeSinceLastUpdate (timeSinceLast);

			// Update the level boss.
			levelBoss.UpdateWithTimeSinceLastUpdate (timeSinceLast);

			// Update the caves (and in turn, their goblins).
			foreach (Cave cave in goblinCaves)
				cave.UpdateWithTimeSinceLastUpdate (timeSinceLast);
		}

		public override void DidSimulatePhysics ()
		{
			base.DidSimulatePhysics ();

			// Get the position either of the default hero or the hero spawn point.
			HeroCharacter defaultHero = DefaultPlayer.Hero;
			CGPoint position;
			if (defaultHero != null && Heroes.Contains (defaultHero))
				position = defaultHero.Position;
			else
				position = DefaultSpawnCGPoint;

			// Update the alphas of any trees that are near the hero (center of the camera) and therefore visible or soon to be visible.
			foreach (Tree tree in trees) {
				if (GraphicsUtilities.DistanceBetweenCGPoints (tree.Position, position) < 1024)
					tree.UpdateAlphaWithScene (this);
			}

			if (!WorldMovedForUpdate)
				return;

			// Show any nearby hidden particle systems and hide those that are too far away to be seen.
			foreach (SKEmitterNode particles in particleSystems) {
				bool particlesAreVisible = GraphicsUtilities.DistanceBetweenCGPoints (particles.Position, position) < 1024;

				if (!particlesAreVisible && !particles.Paused)
					particles.Paused = true;
				else if (particlesAreVisible && particles.Paused)
					particles.Paused = false;
			}

			// Update nearby parallax sprites.
			foreach (ParallaxSprite sprite in parallaxSprites) {
				if (GraphicsUtilities.DistanceBetweenCGPoints (sprite.Position, position) >= 1024)
					continue;

				sprite.UpdateOffset ();
			}
		}

		#endregion

		#region Physics Delegate

		void OnDidBeginContact (object sender, EventArgs e)
		{
			var contact = (SKPhysicsContact)sender;
			// Either bodyA or bodyB in the collision could be a character.
			var node = contact.BodyA.Node as Character;
			if (node != null)
				node.CollidedWith (contact.BodyB);

			// Check bodyB too.
			node = contact.BodyB.Node as Character;
			if (node != null)
				node.CollidedWith (contact.BodyA);

			// Handle collisions with projectiles.
			var isBodyA = (contact.BodyA.CategoryBitMask & (uint)ColliderType.Projectile) != 0;
			var isBodyB = (contact.BodyB.CategoryBitMask & (uint)ColliderType.Projectile) != 0;
			if (isBodyA || isBodyB) {
				SKNode projectile = isBodyA ? contact.BodyA.Node : contact.BodyB.Node;
				projectile.RunAction (SKAction.RemoveFromParent ());

				// Build up a "one shot" particle to indicate where the projectile hit.
				var emitter = (SKEmitterNode)SharedProjectileSparkEmitter.Copy ();
				AddNode (emitter, WorldLayer.AboveCharacter);
				emitter.Position = projectile.Position;
				GraphicsUtilities.RunOneShotEmitter (emitter, 0.15f);
			}
		}

		#endregion

		#region Mapping

		public override nfloat DistanceToWall (CGPoint pos0, CGPoint pos1)
		{
			var a = ConvertWorldPointToLevelMapPoint (pos0);
			var b = ConvertWorldPointToLevelMapPoint (pos1);

			var deltaX = b.X - a.X;
			var deltaY = b.Y - a.Y;
			var dist = GraphicsUtilities.DistanceBetweenCGPoints (a, b);
			var inc = 1f / dist;
			var p = CGPoint.Empty;

			for (nfloat i = 0; i <= 1; i += inc) {
				p.X = a.X + i * deltaX;
				p.Y = a.Y + i * deltaY;

				DataMap point = levelMap.QueryLevelMap (p);
				if (point.Wall > 200) {
					CGPoint wpos2 = ConvertLevelMapPointToWorldPoint (p);
					return GraphicsUtilities.DistanceBetweenCGPoints (pos0, wpos2);
				}
			}
			return nfloat.MaxValue;
		}

		public override bool CanSee (CGPoint pos0, CGPoint pos1)
		{
			var a = ConvertWorldPointToLevelMapPoint (pos0);
			var b = ConvertWorldPointToLevelMapPoint (pos1);

			var deltaX = b.X - a.Y;
			var deltaY = b.Y - a.Y;
			var dist = GraphicsUtilities.DistanceBetweenCGPoints (a, b);
			var inc = 1 / dist;
			var p = CGPoint.Empty;

			for (nfloat i = 0; i <= 1; i += inc) {
				p.X = a.X + i * deltaX;
				p.Y = a.Y + i * deltaY;

				DataMap point = levelMap.QueryLevelMap (p);
				if (point.Wall > 200)
					return false;
			}
			return true;
		}

		#endregion

		#region Point Conversion

		CGPoint ConvertLevelMapPointToWorldPoint (CGPoint location)
		{
			// Given a level map pixel point, convert up to a world point.
			// This determines which "tile" the point falls in and centers within that tile.
			var x = (location.X * LevelMapDivisor) - (WorldCenter + WorldTileSize / 2);
			var y = -((location.Y * LevelMapDivisor) - (WorldCenter + WorldTileSize / 2));

			return new CGPoint (x, y);
		}

		CGPoint ConvertWorldPointToLevelMapPoint (CGPoint location)
		{
			// Given a world based point, resolve to a pixel location in the level map.
			int x = ((int)location.X + WorldCenter) / LevelMapDivisor;
			int y = (WorldSize - ((int)location.Y + WorldCenter)) / LevelMapDivisor;

			return new CGPoint (x, y);
		}

		#endregion

		#region Shared Assets

		protected override void LoadSceneAssets ()
		{
			SKTextureAtlas atlas = SKTextureAtlas.FromName ("Environment");

			// Load archived emitters and create copyable sprites.
			sharedProjectileSparkEmitter = GraphicsUtilities.EmitterNodeWithEmitterNamed ("ProjectileSplat");
			sharedSpawnEmitter = GraphicsUtilities.EmitterNodeWithEmitterNamed ("Spawn");

			sharedSmallTree = new Tree (new SKNode[] {
				SKSpriteNode.FromTexture (atlas.TextureNamed ("small_tree_base.png")),
				SKSpriteNode.FromTexture (atlas.TextureNamed ("small_tree_middle.png")),
				SKSpriteNode.FromTexture (atlas.TextureNamed ("small_tree_top.png"))
			}, 25);
			sharedBigTree = new Tree (new SKNode[] {
				SKSpriteNode.FromTexture (atlas.TextureNamed ("big_tree_base.png")),
				SKSpriteNode.FromTexture (atlas.TextureNamed ("big_tree_middle.png")),
				SKSpriteNode.FromTexture (atlas.TextureNamed ("big_tree_top.png"))
			}, 150);
			sharedBigTree.FadeAlpha = true;
			sharedLeafEmitterA = GraphicsUtilities.EmitterNodeWithEmitterNamed ("Leaves_01");
			sharedLeafEmitterB = GraphicsUtilities.EmitterNodeWithEmitterNamed ("Leaves_02");

			// Load the tiles that make up the ground layer.
			LoadWorldTiles ();

			// Load assets for all the sprites within this scene.
			Cave.LoadSharedAssetsOnce ();
			HeroCharacter.LoadSharedAssetsOnce ();
			Archer.LoadSharedAssetsOnce ();
			Warrior.LoadSharedAssetsOnce ();
			Goblin.LoadSharedAssetsOnce ();
			Boss.LoadSharedAssetsOnce ();
		}

		void LoadWorldTiles ()
		{
			var sw = new Stopwatch ();
			sw.Start ();

			Console.WriteLine ("Loading world tiles");

			SKTextureAtlas tileAtlas = SKTextureAtlas.FromName ("Tiles");

			backgroundTiles = new SKNode[1024];
			for (int y = 0; y < WorldTileDivisor; y++) {
				for (int x = 0; x < WorldTileDivisor; x++) {
					int tileNumber = (y * WorldTileDivisor) + x;

					var textureName = string.Format ("tile{0}.png", tileNumber);
					var texture = tileAtlas.TextureNamed (textureName);
					SKSpriteNode tileNode = SKSpriteNode.FromTexture (texture);

					int xPos = x * WorldTileSize - WorldCenter;
					var yPos = WorldSize - y * WorldTileSize - WorldCenter;
					var position = new CGPoint (xPos, yPos);
					tileNode.Position = position;
					tileNode.ZPosition = -1;
					tileNode.BlendMode = SKBlendMode.Replace;
					backgroundTiles [tileNumber] = tileNode;
				}
			}
			Console.WriteLine ("Loaded all world tiles in {0} seconds", sw.Elapsed);
			sw.Stop ();
		}

		public override void ReleaseSceneAssets ()
		{
			// Get rid of everything unique to this scene (but not the characters, which might appear in other scenes).
			backgroundTiles = null;
			sharedProjectileSparkEmitter = null;
			sharedSpawnEmitter = null;
			sharedLeafEmitterA = null;
			sharedLeafEmitterB = null;
		}

		#endregion
	}
}

