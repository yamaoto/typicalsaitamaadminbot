using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TsabWebApi
{
    [AttributeUsage(AttributeTargets.Class,AllowMultiple = false)]
    public class BotActionAttribute:Attribute
    {
    }
}