using Eto.Drawing;

namespace CodingConnected.TLCProF.BmpUI
{
	public struct SimplePoint
	{
		public int X;
		public int Y;

		public SimplePoint(int x, int y)
		{
			X = x;
			Y = y;
		}
	}

	public class BitmapDetector
    {
		#region Public Fields

		public readonly string Name;
	    public readonly SimplePoint [] Points;

		#endregion // Public Fields
        
		#region Properties

        public bool Presence { get; set; }

        #endregion // Properties

        #region Constructor

        public BitmapDetector(string name, bool presence, SimplePoint[] points)
        {
            Name = name;
            Presence = presence;
	        Points = points;
        }

        #endregion // Constructor
    }
}
