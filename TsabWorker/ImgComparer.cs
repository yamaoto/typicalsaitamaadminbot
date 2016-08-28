using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace TsabWorker
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

        private Task<int> _compare(int wallId, ImgMapper input, string compareBlob, bool strict = true)
        {
            return Task.Run(()=>_compareSync(wallId, input, compareBlob, strict));
        }
        private int _compareSync(int wallId, ImgMapper input, string compareBlob, bool strict = true)
        {           
            var result = 0;
            try
            {

                var f2 = _map[wallId][compareBlob];
                var queue = new List<Task>();
                Task.WaitAll(queue.ToArray());
                for (int x = 0; x < _size; x++)
                {
                    for (int y = 0; y < _size; y++)
                    {
                        var r = Math.Abs(input.Map[x, y].R - f2.Map[x, y].R);
                        var g = Math.Abs(input.Map[x, y].G - f2.Map[x, y].G);
                        var b = Math.Abs(input.Map[x, y].B - f2.Map[x, y].B);
                        result += r + b + g;
                    }
                }
                result = result / (_size * _size);
            }
            catch (Exception e)
            {
                return int.MaxValue;
            }
            return result;
        }

        public async Task<CompareStrictResult> CompareStrict(int wallId, ImgMapper input, string inputBlob,KeyValuePair<string, int>[] order)
        {
            foreach (var blob in order)
            {
                var compare = _compareSync(wallId, input, blob.Key, true);
                if (compare <= _strictValue)
                {
                    return new CompareStrictResult(inputBlob, blob.Key, true) {Value = compare};
                }
            }
            return new CompareStrictResult(inputBlob, null, false);
        }
        public async Task<KeyValuePair<string,int>[]> CompareOrder(int wallId, ImgMapper input, string inputBlob,int number, int total)
        {
            var list = new List<KeyValuePair<string, Task<int>>>();
            var ar = _map[wallId].Keys.ToArray();
            for (var i=number;i< ar.Length; i+=total)
            {
                var blob = ar[i];
                var compare = _compare(wallId, input, blob, false);
                list.Add(new KeyValuePair<string, Task<int>>(blob, compare));
            }
            Task[] tasks = list.Select(s => s.Value).ToArray();
            Task.WaitAll(tasks);
            return list.Select(s=>new KeyValuePair<string, int>(s.Key,s.Value.Result)).OrderBy(o=>o.Value).ToArray();
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
    public class CompareResult
    {
        public CompareResult(string inputBlob, string compareBlob, int result)
        {
            InputBlob = inputBlob;
            CompareBlob = compareBlob;
            Result = result;
        }
        public string InputBlob { get; set; }
        public string CompareBlob { get; set; }
        public int Result { get; set; }
    }
}