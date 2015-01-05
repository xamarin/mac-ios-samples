using System;
using SpriteKit;
using System.Diagnostics;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using Foundation;
using CoreGraphics;

namespace Adventure
{
	public class AdventureScene : MultiplayerLayeredCharacterScene
	{
		private const int WorldTileDivisor = 32;
		// number of tiles
		private const int WorldSize = 4096;
		// pixel size of world (square)
		private const int WorldTileSize = WorldSize / WorldTileDivisor;

		private const int WorldCenter = 2048;

		private const int LevelMapSize = 256;
		// pixel size of level map (square)
		private const int LevelMapDivisor = WorldSize / LevelMapSize;

		private readonly List<Cave> _goblinCaves;
		private readonly List<SKEmitterNode> _particleSystems;
		private readonly List<SKSpriteNode> _parallaxSprites;
		private readonly List<SKSpriteNode> _trees;

		private MapScaner _levelMap;
		private MapScaner _treeMap;

		private Boss _levelBoss;
		// the big boss character

		private static SKEmitterNode _sharedProjectileSparkEmitter;

		private SKEmitterNode SharedProjectileSparkEmitter {
			get {
				return _sharedProjectileSparkEmitter;
			}
		}

		private static SKEmitterNode _sharedSpawnEmitter;

		protected override SKEmitterNode SharedSpawnEmitter {
			get {
				return _sharedSpawnEmitter;
			}
		}

		private static Tree _sharedSmallTree;

		private Tree SharedSmallTree {
			get {
				return _sharedSmallTree;
			}
		}

		private static Tree _sharedBigTree;

		private Tree SharedBigTree {
			get {
				return _sharedBigTree;
			}
		}

		private static SKEmitterNode _sharedLeafEmitterA;

		private SKEmitterNode SharedLeafEmitterA {
			get {
				return _sharedLeafEmitterA;
			}
		}

		private static SKEmitterNode _sharedLeafEmitterB;

		private SKEmitterNode SharedLeafEmitterB {
			get {
				return _sharedLeafEmitterB;
			}
		}

		private static SKNode[] _backgroundTiles;

		private SKNode[] BackgroundTiles {
			get {
				return _backgroundTiles;
			}
		}

		private HeroType _heroType;

		public HeroType DefaultPlayerHeroType {
			get {
				return _heroType;
			}
			set {
				_heroType = value;
				DefaultPlayer.HeroType = _heroType;
			}
		}

		private Random _random;

		private Random Random {
			get {
				_random = _random ?? new Random ();
				return _random;
			}
		}

		public AdventureScene (CGSize size)
			: base (size)
		{
			_goblinCaves = new List<Cave> ();

			_particleSystems = new List<SKEmitterNode> ();
			_parallaxSprites = new List<SKSpriteNode> ();
			_trees = new List<SKSpriteNode> ();
		}

		public override void Initialize ()
		{
			base.Initialize ();

			// Build level and tree maps from map_collision.png and map_foliage.png respectively.
			_levelMap = GraphicsUtilities.CreateMapScaner ("map_level.png");
			_treeMap = GraphicsUtilities.CreateMapScaner ("map_trees.png");

			Cave.GoblinCap = 32;

			BuildWorld ();

			// Center the camera on the hero spawn point.
			var startPosition = DefaultSpawnCGPoint;
			CenterWorld (startPosition);
		}

		#region World Building

		private void BuildWorld ()
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

		private void AddBackgroundTiles ()
		{
			// Tiles should already have been pre-loaded in +loadSceneAssets.
			foreach (SKNode tileNode in BackgroundTiles)
				AddNode (tileNode, WorldLayer.Ground);
		}

		private void AddSpawnPoints ()
		{
			// Add goblin caves and set hero/boss spawn points.
			for (int y = 0; y < LevelMapSize; y++) {
				for (int x = 0; x < LevelMapSize; x++) {
					var location = new CGPoint (x, y);
					DataMap spot = _levelMap.QueryLevelMap (location);

					// Get the world space point for this level map pixel.
					CGPoint worldPoint = ConvertLevelMapPointToWorldPoint (location);

					if (spot.BossLocation <= 200) {
						_levelBoss = new Boss (worldPoint);
						_levelBoss.AddToScene (this);
					} else if (spot.GoblinCaveLocation >= 200) {
						Cave cave = new Cave (worldPoint);
						_goblinCaves.Add (cave);
						_parallaxSprites.Add (cave);
						cave.AddToScene (this);
					} else if (spot.HeroSpawnLocation >= 200) {
						DefaultSpawnCGPoint = worldPoint; // there's only one
					}
				}
			}
		}

		private void AddTrees ()
		{
			for (int y = 0; y < LevelMapSize; y++) {
				for (int x = 0; x < LevelMapSize; x++) {
					var location = new CGPoint (x, y);
					var spot = _treeMap.QueryTreeMap (location);

					var treePos = ConvertLevelMapPointToWorldPoint (location);
					WorldLayer treeLayer = WorldLayer.Top;
					Tree tree = null;

					if (spot.SmallTreeLocation >= 200) {
						// Create small tree at this location.
						treeLayer = WorldLayer.AboveCharacter;
						tree = (Tree)SharedSmallTree.Clone ();
					} else if (spot.BigTreeLocation >= 200) {
						// Create big tree with leaf emitters at this position.
						tree = (Tree)SharedBigTree.Clone ();

						// Pick one of the two leaf emitters for this tree.
						SKEmitterNode emitterToCopy = Random.Next (2) == 1 ? SharedLeafEmitterA : SharedLeafEmitterB;
						SKEmitterNode emitter = (SKEmitterNode)((NSObject)emitterToCopy).Copy ();

						emitter.Position = treePos;
						emitter.Paused = true;
						AddNode (emitter, WorldLayer.AboveCharacter);
						_particleSystems.Add (emitter);
					} else {
						continue;
					}

					tree.Position = treePos;
					tree.ZRotation = (float)(Random.NextDouble () * Math.PI * 2);
					AddNode (tree, treeLayer);
					_parallaxSprites.Add (tree);
					_trees.Add (tree);
				}
			}

			_treeMap = null;
		}

		private void AddCollisionWalls ()
		{
			Stopwatch sw = new Stopwatch ();
			sw.Start ();

			byte[] filled = new byte[LevelMapSize * LevelMapSize];

			int numVolumes = 0;
			int numBlocks = 0;

			// Add horizontal collision walls.
			for (int y = 0; y < LevelMapSize; y++) { // iterate in horizontal rows
				for (int x = 0; x < LevelMapSize; x++) {
					var location = new CGPoint (x, y);
					DataMap spot = _levelMap.QueryLevelMap (location);

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
						nextSpot = _levelMap.QueryLevelMap (new CGPoint (horizontalDistanceFromLeft, y));
					}

					int wallWidth = horizontalDistanceFromLeft - x;
					int verticalDistanceFromTop = y;

					if (wallWidth > 8) {
						nextSpot = spot;
						while (verticalDistanceFromTop < LevelMapSize
						       && nextSpot.Wall >= 200) {
							verticalDistanceFromTop++;
							nextSpot = _levelMap.QueryLevelMap (new CGPoint (x + (wallWidth / 2), verticalDistanceFromTop));
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
					DataMap spot = _levelMap.QueryLevelMap (location);

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
						nextSpot = _levelMap.QueryLevelMap (new CGPoint (x, verticalDistanceFromTop));
					}

					int wallHeight = verticalDistanceFromTop - y;
					int horizontalDistanceFromLeft = x;

					if (wallHeight > 8) {
						nextSpot = spot;
						while (horizontalDistanceFromLeft < LevelMapSize
						       && nextSpot.Wall >= 200) {
							horizontalDistanceFromLeft++;
							nextSpot = _levelMap.QueryLevelMap (new CGPoint (horizontalDistanceFromLeft, y + wallHeight / 2));
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

		private void AddCollisionWallAtWorldPoint (CGPoint worldPoint, float width, float height)
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
			foreach (Cave cave in _goblinCaves)
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
			_levelBoss.UpdateWithTimeSinceLastUpdate (timeSinceLast);

			// Update the caves (and in turn, their goblins).
			foreach (Cave cave in _goblinCaves)
				cave.UpdateWithTimeSinceLastUpdate (timeSinceLast);
		}

		public override void DidSimulatePhysics ()
		{
			base.DidSimulatePhysics ();

			// Get the position either of the default hero or the hero spawn point.
			HeroCharacter defaultHero = DefaultPlayer.Hero;
			CGPoint position = CGPoint.Empty;
			if (defaultHero != null && Heroes.Contains (defaultHero))
				position = defaultHero.Position;
			else
				position = DefaultSpawnCGPoint;

			// Update the alphas of any trees that are near the hero (center of the camera) and therefore visible or soon to be visible.
			foreach (Tree tree in _trees) {
				if (GraphicsUtilities.DistanceBetweenCGPoints (tree.Position, position) < 1024)
					tree.UpdateAlphaWithScene (this);
			}

			if (!WorldMovedForUpdate)
				return;

			// Show any nearby hidden particle systems and hide those that are too far away to be seen.
			foreach (SKEmitterNode particles in _particleSystems) {
				bool particlesAreVisible = GraphicsUtilities.DistanceBetweenCGPoints (particles.Position, position) < 1024;

				if (!particlesAreVisible && !particles.Paused)
					particles.Paused = true;
				else if (particlesAreVisible && particles.Paused)
					particles.Paused = false;
			}

			// Update nearby parallax sprites.
			foreach (ParallaxSprite sprite in _parallaxSprites) {
				if (GraphicsUtilities.DistanceBetweenCGPoints (sprite.Position, position) >= 1024)
					continue;

				sprite.UpdateOffset ();
			}
		}

		#endregion

		#region Physics Delegate

		private void OnDidBeginContact (object sender, EventArgs e)
		{
			var contact = (SKPhysicsContact)sender;
			// Either bodyA or bodyB in the collision could be a character.
			Character node = contact.BodyA.Node as Character;
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
				SKEmitterNode emitter = (SKEmitterNode)((NSObject)SharedProjectileSparkEmitter).Copy ();
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

				DataMap point = _levelMap.QueryLevelMap (p);
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

				DataMap point = _levelMap.QueryLevelMap (p);
				if (point.Wall > 200)
					return false;
			}
			return true;
		}

		#endregion

		#region Point Conversion

		private CGPoint ConvertLevelMapPointToWorldPoint (CGPoint location)
		{
			// Given a level map pixel point, convert up to a world point.
			// This determines which "tile" the point falls in and centers within that tile.
			var x = (location.X * LevelMapDivisor) - (WorldCenter + WorldTileSize / 2);
			var y = -((location.Y * LevelMapDivisor) - (WorldCenter + WorldTileSize / 2));

			return new CGPoint (x, y);
		}

		private CGPoint ConvertWorldPointToLevelMapPoint (CGPoint location)
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
			_sharedProjectileSparkEmitter = GraphicsUtilities.EmitterNodeWithEmitterNamed ("ProjectileSplat");
			_sharedSpawnEmitter = GraphicsUtilities.EmitterNodeWithEmitterNamed ("Spawn");

			_sharedSmallTree = new Tree (new SKNode[] {
				SKSpriteNode.FromTexture (atlas.TextureNamed ("small_tree_base.png")),
				SKSpriteNode.FromTexture (atlas.TextureNamed ("small_tree_middle.png")),
				SKSpriteNode.FromTexture (atlas.TextureNamed ("small_tree_top.png"))
			}, 25);
			_sharedBigTree = new Tree (new SKNode[] {
				SKSpriteNode.FromTexture (atlas.TextureNamed ("big_tree_base.png")),
				SKSpriteNode.FromTexture (atlas.TextureNamed ("big_tree_middle.png")),
				SKSpriteNode.FromTexture (atlas.TextureNamed ("big_tree_top.png"))
			}, 150);
			_sharedBigTree.FadeAlpha = true;
			_sharedLeafEmitterA = GraphicsUtilities.EmitterNodeWithEmitterNamed ("Leaves_01");
			_sharedLeafEmitterB = GraphicsUtilities.EmitterNodeWithEmitterNamed ("Leaves_02");

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

		private void LoadWorldTiles ()
		{
			Stopwatch sw = new Stopwatch ();
			sw.Start ();

			Console.WriteLine ("Loading world tiles");

			SKTextureAtlas tileAtlas = SKTextureAtlas.FromName ("Tiles");

			_backgroundTiles = new SKNode[1024];
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
					_backgroundTiles [tileNumber] = tileNode;
				}
			}
			Console.WriteLine ("Loaded all world tiles in {0} seconds", sw.Elapsed);
			sw.Stop ();
		}

		public override void ReleaseSceneAssets ()
		{
			// Get rid of everything unique to this scene (but not the characters, which might appear in other scenes).
			_backgroundTiles = null;
			_sharedProjectileSparkEmitter = null;
			_sharedSpawnEmitter = null;
			_sharedLeafEmitterA = null;
			_sharedLeafEmitterB = null;
		}

		#endregion
	}
}

