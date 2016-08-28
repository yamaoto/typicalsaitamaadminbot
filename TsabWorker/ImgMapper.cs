using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace TsabWorker
{
    public class TsabPixel
    {
        public TsabPixel(Color color)
        {
            R = color.R;
            G = color.G;
            B = color.B;
        }

        public TsabPixel(int r, int g, int b)
        {
            R = r;
            G = g;
            B = b;
        }

        public int R { get; set; }
        public int G { get; set; }
        public int B { get; set; }
    }
    public class ImgMapper
    {
        private readonly int _size;

        public readonly TsabPixel[,] Map;

        public bool Processed { get; private set; }

        public ImgMapper(Image img, int size)
        {
            _size = size;
            var bmp = new Bitmap(img,size,size);
            Map = new TsabPixel[size, size];
            for (var x = 0; x < size; x++)
            {
                for (var y = 0; y < size; y++)
                {
                    Map[x,y] = new TsabPixel(bmp.GetPixel(x, y));
                }
            }
        }
                
    }
}