using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TsabConsole
{
    internal interface ITsabConsoleAction
    {
        string Syntax { get; }
        string Descriptioin { get; }
        string ActionName { get; }
        void Exec(string[] args);
    }
}
