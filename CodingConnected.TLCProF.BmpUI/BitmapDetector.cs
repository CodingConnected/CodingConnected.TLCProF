using Eto.Drawing;

namespace CodingConnected.TLCProF.BmpUI
{
    public class BitmapDetector
    {
        #region Properties

        public readonly string Name;
        public bool Presence { get; set; }
        public System.Drawing.Point Coordinate { get; }
        public readonly Point [] Points;

        #endregion // Properties

        #region Constructor

        public BitmapDetector(string name, bool presence, System.Drawing.Point coordinate, Point[] points)
        {
            Name = name;
            Presence = presence;
            Coordinate = coordinate;
            Points = points;
        }

        #endregion // Constructor
    }
}
