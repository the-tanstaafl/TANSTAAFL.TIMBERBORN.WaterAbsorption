using System;
using System.Collections.Generic;
using System.Text;

namespace TANSTAAFL.TIMBERBORN.WaterAbsorption.WaterSearch
{
    internal class Direction
    {
        private static readonly (int x, int y) _north = (0, -1);
        private static readonly (int x, int y) _east = (-1, 0);
        private static readonly (int x, int y) _west = (1, 0);
        private static readonly (int x, int y) _south = (0, 1);
        private static readonly (int x, int y) _northeast = (-1, -1);
        private static readonly (int x, int y) _northwest = (1, -1);
        private static readonly (int x, int y) _southeast = (-1, 1);
        private static readonly (int x, int y) _southwest = (1, 1);

        internal enum CardinalDirection
        {
            North,
            East,
            West,
            South,
            NorthEast,
            NorthWest,
            SouthEast,
            SouthWest
        }

        internal static Dictionary<CardinalDirection, (int x, int y)> CardinalDirectionXY = new Dictionary<CardinalDirection, (int x, int y)>
        {
            { CardinalDirection.North, _north },
            { CardinalDirection.East, _east },
            { CardinalDirection.West, _west },
            { CardinalDirection.South, _south },
            { CardinalDirection.NorthEast, _northeast },
            { CardinalDirection.NorthWest, _northwest },
            { CardinalDirection.SouthEast, _southeast },
            { CardinalDirection.SouthWest, _southwest }
        };

        internal static Dictionary<CardinalDirection, CardinalDirection[]> CardinalDirectionPoints = new Dictionary<CardinalDirection, CardinalDirection[]>
        {
            { CardinalDirection.North,     new CardinalDirection[] { CardinalDirection.North, CardinalDirection.NorthEast, CardinalDirection.NorthWest } },
            { CardinalDirection.East,      new CardinalDirection[] { CardinalDirection.East, CardinalDirection.NorthEast, CardinalDirection.SouthEast } },
            { CardinalDirection.West,      new CardinalDirection[] { CardinalDirection.West, CardinalDirection.NorthWest, CardinalDirection.SouthWest } },
            { CardinalDirection.South,     new CardinalDirection[] { CardinalDirection.South, CardinalDirection.SouthEast, CardinalDirection.SouthWest } },
            { CardinalDirection.NorthEast, new CardinalDirection[] { CardinalDirection.NorthEast, CardinalDirection.North, CardinalDirection.East } },
            { CardinalDirection.NorthWest, new CardinalDirection[] { CardinalDirection.NorthWest, CardinalDirection.North, CardinalDirection.West } },
            { CardinalDirection.SouthEast, new CardinalDirection[] { CardinalDirection.SouthEast, CardinalDirection.South, CardinalDirection.East } },
            { CardinalDirection.SouthWest, new CardinalDirection[] { CardinalDirection.SouthWest, CardinalDirection.South, CardinalDirection.West } }
        };
    }
}
