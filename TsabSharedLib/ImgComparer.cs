using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Threading.Tasks;

namespace TsabSharedLib
{
    public class ImgComparer
    {
        private readonly int _size;

        public ImgComparer(int size, int strictValue,IEnumerable<int> walls)
        {
            _size = size;
            _strictValue = strictValue;
            _map = new Dictionary<int, Dictionary<string, ImgMapper>>();
            foreach (var wall in walls)
            {
                _map.Add(wall,new Dictionary<string, ImgMapper>());
            }
        }

        public void Clear()
        {
            _map.Clear();
        }

        private readonly Dictionary<int, Dictionary<string, ImgMapper>> _map;
        private readonly int _strictValue;

        public bool CheckLoad(int wallId)
        {
            return _map.ContainsKey(wallId) && _map[wallId].Keys.Count > 0;
        }
        public bool CheckLoad(int wallId, string blob)
        {
            return _map[wallId].ContainsKey(blob);
        }
        public void Load(int wallId,string blob,Stream stream)
        {
            if (CheckLoad(wallId,blob))
                return;
            var img = Image.FromStream(stream);
            var mapper = new ImgMapper(img, _size);
            _map[wallId].Add(blob, mapper);
        }
        
        private int _compareSync(int wallId, ImgMapper input, string compareBlob)
        {
            var blob = _map[wallId][compareBlob];
            int r=0, g=0, b=0;
            for (var i = 0; i < _size; i++)
            {
                r += Math.Abs(input.Map[i].R - blob.Map[i].R);
                g += Math.Abs(input.Map[i].G - blob.Map[i].G);
                b += Math.Abs(input.Map[i].B - blob.Map[i].B);
            }
            return r + g + b;
        }

        public CompareStrictResult Compare(int wallId, ImgMapper input, string inputBlob, IEnumerable<string> order)
        {
            foreach (var blob in order)
            {
                var compare = _compareSync(wallId, input, blob);
                if (compare <= _strictValue)
                {
                    return new CompareStrictResult(inputBlob, blob, true) {Value = compare};
                }
            }
            return new CompareStrictResult(inputBlob, null, false);
        }

        public IEnumerable<string> Order(int wallId, ImgMapper input, string inputBlob,int number,int total)
        {
            var order = new Dictionary<string,int>();
            var ar = _map[wallId].Keys.ToArray();
            for(var i=number;i<ar.Length;i+=total)
            {
                var blob = ar[i];
                var compare = _compareSync(wallId, input, blob);
                order.Add(blob,compare);
            }
            return order.OrderBy(o => o.Value).Select(s => s.Key);
        }
    }
    public class CompareStrictResult
    {
        public CompareStrictResult(string inputBlob, string foundBlob, bool result)
        {
            InputBlob = inputBlob;
            FoundBlob = foundBlob;
            Result = result;
        }
        public string InputBlob { get; set; }
        public string FoundBlob { get; set; }
        public bool Result { get; set; }
        public int? Value{ get; set; }
    }
}