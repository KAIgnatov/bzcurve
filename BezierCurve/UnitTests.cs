using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace BezierCurve
{
    [TestFixture]
    class UnitTests
    {
        [Test]
        public static void TestOperations()
        {
            BezierCurveModule c = new BezierCurveModule();

            Assert.AreEqual(Math.PI, c.DirAngle(new Topomatic.Sfc.SurfacePoint(new Topomatic.Cad.Foundation.Vector3D(0, 1, 0)), new Topomatic.Sfc.SurfacePoint(new Topomatic.Cad.Foundation.Vector3D(1, 0, 0))));
        }
    }
}
