using System;
using System.Collections.Concurrent;
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

        public ImgComparer(int size, int strictValue, int strictMaxValue, IEnumerable<int> walls)
        {
            _size = size;
            _strictValue = strictValue;
            _strictMaxValue = strictMaxValue;
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
        private readonly int _strictMaxValue;

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
            return (r + g + b)/3;
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
                if (compare >= _strictMaxValue)
                {
                    return new CompareStrictResult(inputBlob, null, false);
                }
            }
            return new CompareStrictResult(inputBlob, null, false);
        }

        public IEnumerable<string> Order(int wallId, ImgMapper input, string inputBlob)
        {
            
           var array = _map[wallId].Keys.ToArray();
           var results = new ConcurrentBag<Dictionary<string, int>>();
            Parallel.For(0, Environment.ProcessorCount,
                (no) => results.Add(_order(array, wallId, input, no, Environment.ProcessorCount)));
            var list = results.SelectMany(items => items).ToDictionary(item => item.Key, item => item.Value);
            return list.OrderBy(o => o.Value).Select(s => s.Key);
        }

        private Dictionary<string, int> _order(string[] array,int wallId, ImgMapper input, int start,int total)
        {
            var result = new Dictionary<string, int>();
            for (var i = start; i < array.Length; i += total)
            {
                var blob = array[i];
                var compare = _compareSync(wallId, input, blob);
                if (compare < _strictMaxValue)
                    result.Add(blob, compare);
            }
            return result;
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