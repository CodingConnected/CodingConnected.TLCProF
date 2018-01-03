using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Eto.Drawing;

namespace CodingConnected.TLCProF.BmpUI
{
	internal static class BmpUITools
	{
		public static SimplePoint[] GetPoints(Bitmap bmp, SimplePoint pt)
		{
			if (pt.X == 0 || pt.Y == 0 || pt.X >= bmp.Width || pt.Y >= bmp.Height)
				return null;

			var targetColor = bmp.GetPixel(pt.X, pt.Y);
			if (Math.Abs(targetColor.R) < 0.001 &&
				Math.Abs(targetColor.G) < 0.001 &&
				Math.Abs(targetColor.B) < 0.001)
				return null;

			var l = new List<SimplePoint>();

			var pixels = new Stack<SimplePoint>();

			pixels.Push(pt);
			while (pixels.Count != 0)
			{
				var temp = pixels.Pop();
				int y1 = temp.Y;
				while (y1 >= 0 && bmp.GetPixel(temp.X, y1) == targetColor)
				{
					y1--;
				}
				y1++;
				var spanLeft = false;
				var spanRight = false;
				while (y1 < bmp.Height && bmp.GetPixel(temp.X, y1) == targetColor)
				{
					l.Add(new SimplePoint(temp.X, y1));

					if (!spanLeft && temp.X > 0 && bmp.GetPixel(temp.X - 1, y1) == targetColor)
					{
						var np = new SimplePoint(temp.X - 1, y1);
						if (!l.Any(x => x.X == np.X && x.Y == np.Y))
						{
							pixels.Push(np);
							spanLeft = true;
						}
					}
					else if (spanLeft && temp.X - 1 == 0 && bmp.GetPixel(temp.X - 1, y1) != targetColor)
					{
						spanLeft = false;
					}
					if (!spanRight && temp.X < bmp.Width - 1 && bmp.GetPixel(temp.X + 1, y1) == targetColor)
					{
						var np = new SimplePoint(temp.X + 1, y1);
						if (!l.Any(x => x.X == np.X && x.Y == np.Y))
						{
							pixels.Push(np);
							spanRight = true;
						}
					}
					else if (spanRight && temp.X < bmp.Width - 1 && bmp.GetPixel(temp.X + 1, y1) != targetColor)
					{
						spanRight = false;
					}
					y1++;
				}
			}
			return l.ToArray();
		}

		public static void FloodFill(Bitmap bmp, SimplePoint pt, Color replacementColor)
		{
			if (pt.X == 0 || pt.Y == 0 || pt.X >= bmp.Width || pt.Y >= bmp.Height)
				return;

			var targetColor = bmp.GetPixel(pt.X, pt.Y);
			if (targetColor == replacementColor)
			{
				return;
			}
			var pixels = new Stack<SimplePoint>();

			pixels.Push(pt);
			while (pixels.Count != 0)
			{
				var temp = pixels.Pop();
				int y1 = temp.Y;
				while (y1 >= 0 && bmp.GetPixel(temp.X, y1) == targetColor)
				{
					y1--;
				}
				y1++;
				var spanLeft = false;
				var spanRight = false;
				while (y1 < bmp.Height && bmp.GetPixel(temp.X, y1) == targetColor)
				{
					bmp.SetPixel(temp.X, y1, replacementColor);

					if (!spanLeft && temp.X > 0 && bmp.GetPixel(temp.X - 1, y1) == targetColor)
					{
						var np = new SimplePoint(temp.X - 1, y1);
						pixels.Push(np);
						spanLeft = true;
					}
					else if (spanLeft && temp.X - 1 == 0 && bmp.GetPixel(temp.X - 1, y1) != targetColor)
					{
						spanLeft = false;
					}
					if (!spanRight && temp.X < bmp.Width - 1 && bmp.GetPixel(temp.X + 1, y1) == targetColor)
					{
						var np = new SimplePoint(temp.X + 1, y1);
						pixels.Push(np);
						spanRight = true;
					}
					else if (spanRight && temp.X < bmp.Width - 1 && bmp.GetPixel(temp.X + 1, y1) != targetColor)
					{
						spanRight = false;
					}
					y1++;
				}
			}
		}

	}
}
