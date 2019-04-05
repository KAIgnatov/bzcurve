using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Topomatic.ApplicationPlatform;

namespace BezierCurve
{
    public class BezierCurvePluginHost : PluginHost
    {
        protected override Type[] GetModules()
        {
            return new Type[] { typeof(BezierCurveModule) };
        }

        public override string PluginName
        {
            get { return "BezierCurve"; }
        }
    }
}
