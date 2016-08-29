using System.Drawing;
using System.Net.Mime;

namespace TsabSharedLib
{
    public class TsabPixel
    {
        public TsabPixel()
        {
        }

        public int R { get; set; }
        public int G { get; set; }
        public int B { get; set; }
    }
    public class ImgMapper
    {
        private readonly int _size;

        public readonly TsabPixel[] Map;

        public bool Processed { get; private set; }

        public ImgMapper(Image img, int size)
        {
            _size = size;
            var bmp = new Bitmap(img, size, size);
            Map = new TsabPixel[size];

            for (var y = 0; y < size; y++)
            {
                Map[y] = new TsabPixel();
                for (var x = 0; x < size; x++)
                {
                    var pixel = bmp.GetPixel(x, y);
                    Map[y].R += pixel.R;
                    Map[y].G += pixel.G;
                    Map[y].B += pixel.B;
                }
            }
        }

    }
}