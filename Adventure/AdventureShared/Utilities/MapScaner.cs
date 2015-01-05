using System;

using CoreGraphics;

namespace Adventure
{
	public class MapScaner
	{
		private readonly nint MapWidth;
		private readonly byte[] Map;

		public MapScaner (byte[] mapData, nint mapWidth)
		{
			Map = mapData;
			MapWidth = mapWidth;
		}

		public DataMap QueryLevelMap (CGPoint point)
		{
			// Calc start index of pixel info
			var index = ConvertToIndex (point);

			return new DataMap {
				BossLocation = Map [index],			// Alpha chanel
				Wall = Map [++index],					// Red
				GoblinCaveLocation = Map [++index],	// Green
				HeroSpawnLocation = Map [++index]		// Blue
			};
		}

		public TreeMap QueryTreeMap (CGPoint point)
		{
			// Calc start index of pixel info
			var index = ConvertToIndex (point);

			return new TreeMap {
				UnusedA = Map [index],				// Alpha chanel
				BigTreeLocation = Map [++index],	// Red
				SmallTreeLocation = Map [++index],	// Green
				UnusedB = Map [++index]			// Blue
			};
		}

		private int ConvertToIndex (CGPoint point)
		{
			// One pixel take 4 bytes (Alpha + R + G + B)
			var index = 4 * (point.Y * MapWidth + point.X);
			return (int)index;
		}
	}
}

