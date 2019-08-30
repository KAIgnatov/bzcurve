using Topomatic.ApplicationPlatform;
using System.ComponentModel;
using Topomatic.Cad.View;
using Topomatic.Sfc.Layer;
using Topomatic.Sfc;
using System;
using Topomatic.Cad.Foundation;
using Topomatic.Dwg.Layer;
using Topomatic.Smt;

namespace BezierCurve
{
    public partial class BezierCurveModule : Module
    {
        private CallAction actBZCurve;

        public BezierCurveModule()
        {
            InitializeComponent();
        }

        public BezierCurveModule(IContainer container)
        {
            container.Add(this);
            InitializeComponent();
        }

        protected override void OnApplicationInitialized(object sender, ApplicationInitializedEventArgs e)
        {
            base.OnApplicationInitialized(sender, e);
        }

        private void InitializeComponent()
        {
            this.actBZCurve = new Topomatic.ApplicationPlatform.CallAction();
            // 
            // actBZCurve
            // 
            this.actBZCurve.Caption = "bezier curve";
            this.actBZCurve.Cmd = "bzcurve";
            this.actBZCurve.UID = "ID_BEZIER_CURVE";
            this.actBZCurve.Execute += new Topomatic.ApplicationPlatform.ExecuteEventHandler(this.actBZCurve_Execute);
            // 
            // BezierCurveModule
            // 
            this.Items.Add(this.actBZCurve);

        }

        private void actBZCurve_Execute(object sender, ExecuteEventArgs e)
        {
            var cadView = this.CadView;
            if (cadView == null)
            {
                return;
            }
            var surfaceLayer = SurfaceLayer.GetSurfaceLayer(cadView);
            if (surfaceLayer == null)
            {
                return;
            }
            var surface = surfaceLayer.Surface;
            if (surface == null)
            {
                return;
            }

            cadView.SelectionSet.Clear();
            surface.BeginUpdate("Сглаживание структурной линии");

            try
            {
                var line = surfaceLayer.SelectOneStructureLine(null, "Выберите структурную линию") as StructureLine;
                if (line == null)
                {
                    return;
                }

                double step = 10.0;
                GetPointResult gpr_delta = Topomatic.Cad.View.Hints.CadCursors.GetDoubleWithDefault(cadView, ref step, "Укажите укажите размер сегмента после сглаживания, м <10>:");
                if (gpr_delta != GetPointResult.Accept)
                {
                    return;
                }

                int Nb = 3;
                GetPointResult gpr_code = Topomatic.Cad.View.Hints.CadCursors.GetIntegerWithDefault(cadView, ref Nb, "Укажите количество сегментов для разбиения коротких участков <3>:");
                if (gpr_code != GetPointResult.Accept)
                {
                    return;
                }

                var editor = new PointEditor(surface);

                var newline = new Topomatic.Sfc.StructureLine();

                newline.CopyProperty(line);

                SurfacePoint[,] segments = new SurfacePoint[(line.Count - 1), 4];

                var node = line[0];
                var point = surface.Points[node.Index];
                segments[0, 0] = point.Clone();


                double coef = 0.341, raznicaS = 1.0, raznicaF = 1.0;

                for (int i = 0; i < line.Count-2; i++)
                {
                    double LS = 0, LF = 0;

                    var nodeS = line[i];
                    var pointS = surface.Points[nodeS.Index].Clone();
                    var nodeF = line[i+1];
                    var pointF = surface.Points[nodeF.Index].Clone();
                    var nodeN = line[i+2];
                    var pointN = surface.Points[nodeN.Index].Clone();

                    LS = Math.Sqrt(Math.Pow(pointF.Vertex.X - pointS.Vertex.X, 2) + Math.Pow(pointF.Vertex.Y - pointS.Vertex.Y, 2) + Math.Pow(pointF.Vertex.Z - pointS.Vertex.Z, 2));
                    LF = Math.Sqrt(Math.Pow(pointN.Vertex.X - pointF.Vertex.X, 2) + Math.Pow(pointN.Vertex.Y - pointF.Vertex.Y, 2) + Math.Pow(pointN.Vertex.Z - pointF.Vertex.Z, 2));

                    double D1 = DirAngle(pointS, pointF);
                    double D2 = DirAngle(pointF, pointN);
                    double gamma=0, C1, C2;

                    //if (D1 > D2)
                    //{
                    //    gamma = Math.PI + D2 - D1;
                    //}
                    //else if (D2 < D1)
                    //{
                    //    gamma = Math.PI + D1 - D2;
                    //}
                    //else { gamma = Math.PI; }

                    gamma = D2 - D1 + 2 * 3.1415926535897932384626433832795;


                    if (gamma < Math.PI )
                    {
                        C1 = D2 + ((Math.PI - gamma) / 2) + Math.PI*1.5;
                        C2 = C1 + Math.PI;
                    }
                    else
                    {
                        if (D1 > D2)
                        {
                            C1 = D2 + ((Math.PI - gamma) / 2) + Math.PI * 1.5;
                            C2 = C1 + Math.PI;
                        }
                        else
                        {
                            C1 = D2 - gamma + ((gamma - Math.PI) / 2) + Math.PI * 0.5;
                            C2 = C1 + Math.PI;
                        }
                    }


                    //if(D1>D2)
                    //{
                    //    double temp = C2;
                    //    C2 = C1;
                    //    C1 = temp;
                    //}

                    if (LS / LF > 10)
                        LS=LF;

                    if (LF / LS > 10)
                        LF=LS;


                    double dy = LS * coef * Math.Cos(C1)*raznicaS;
                    double dx = LS * coef * Math.Sin(C1)*raznicaS;

                    var vectorFC1 = new Vector3D(pointF.Vertex.X + dx, pointF.Vertex.Y + dy, pointF.Vertex.Z);

                    dy = LF * coef * Math.Cos(C2)*raznicaF;
                    dx = LF * coef * Math.Sin(C2)*raznicaF;
                    var vectorFC2 = new Vector3D(pointF.Vertex.X + dx, pointF.Vertex.Y + dy, pointF.Vertex.Z);

                    SurfacePoint pointFC1 = new SurfacePoint(vectorFC1);
                    SurfacePoint pointFC2 = new SurfacePoint(vectorFC2);

                    segments[i, 3] = pointF.Clone();
                    segments[i + 1, 0] = pointF.Clone();

                    if (i == 0)
                    {
                        //double C0 = D1 - ((Math.PI - gamma) / 2);
                        double C0 = C1 - 3.14 - gamma;
                        dy = LS * coef * Math.Cos(C0);
                        dx = LS * coef * Math.Sin(C0);
                        var vectorSC0 = new Vector3D(pointS.Vertex.X + dx, pointS.Vertex.Y + dy, pointS.Vertex.Z);
                        SurfacePoint pointSC1 = new SurfacePoint(vectorSC0);
                        segments[0, 1] = pointSC1;
                    }

                    if (i == line.Count - 3)
                    {
                        //double C3 = Math.PI + D2 + ((Math.PI - gamma) / 2);
                        double C3 = C2 + 3.14 + gamma;
                        dy = LF * coef * Math.Cos(C3);
                        dx = LF * coef * Math.Sin(C3);
                        var vectorNC3 = new Vector3D(pointN.Vertex.X + dx, pointN.Vertex.Y + dy, pointN.Vertex.Z);
                        SurfacePoint pointNC3 = new SurfacePoint(vectorNC3);
                        segments[i+1, 2] = pointNC3;
                        segments[i + 1, 3] = pointN.Clone();
                    }

                    segments[i, 2] = pointFC1;
                    segments[i+1, 1] = pointFC2;
                }

                var drawingLayer = DrawingLayer.GetDrawingLayer(cadView);
                if (drawingLayer == null)
                {
                    return;
                }
                var drawing = drawingLayer.Drawing;
                if (drawing == null)
                {
                    return;
                }


                drawing.ActiveSpace.BeginUpdate();

                try
                {
                    /*for (int i = 0; i < segments.GetLength(0); i++)
                    {
                        var cLineS = new Topomatic.Dwg.Entities.DwgPolyline();
                        var cLineF = new Topomatic.Dwg.Entities.DwgPolyline();

                        var posS1 = new Topomatic.Cad.Foundation.Vector2D(segments[i, 0].Vertex.X, segments[i, 0].Vertex.Y);
                        cLineS.Add(new Topomatic.Cad.Foundation.BugleVector2D(posS1, 0));
                        var posF1 = new Topomatic.Cad.Foundation.Vector2D(segments[i, 1].Vertex.X, segments[i, 1].Vertex.Y);
                        cLineS.Add(new Topomatic.Cad.Foundation.BugleVector2D(posF1, 0));
                        var posS2 = new Topomatic.Cad.Foundation.Vector2D(segments[i, 2].Vertex.X, segments[i, 2].Vertex.Y);
                        cLineF.Add(new Topomatic.Cad.Foundation.BugleVector2D(posS2, 0));
                        var posF2 = new Topomatic.Cad.Foundation.Vector2D(segments[i, 3].Vertex.X, segments[i, 3].Vertex.Y);
                        cLineF.Add(new Topomatic.Cad.Foundation.BugleVector2D(posF2, 0));

                        drawing.ActiveSpace.Add(cLineS);
                        drawing.ActiveSpace.Add(cLineF);
                    }*/
                }
                finally
                {
                    drawing.ActiveSpace.EndUpdate();
                }

                for (int i = 0; i < segments.GetLength(0); i++)
                {
                    double t = 0.0;
                    //int step = 100;
                    double L1 = Math.Sqrt(Math.Pow(segments[i,3].Vertex.X - segments[i, 0].Vertex.X, 2) + //длина исходного сегмента
                        Math.Pow(segments[i, 3].Vertex.Y - segments[i, 0].Vertex.Y, 2) + 
                        Math.Pow(segments[i, 3].Vertex.Z - segments[i, 0].Vertex.Z, 2));

                    double L3 = Math.Sqrt(Math.Pow(segments[i, 1].Vertex.X - segments[i, 0].Vertex.X, 2) +//длина полилинии, проходящей через начальную, две контрольные и конечную точки
                        Math.Pow(segments[i, 1].Vertex.Y - segments[i, 0].Vertex.Y, 2) +
                        Math.Pow(segments[i, 1].Vertex.Z - segments[i, 0].Vertex.Z, 2))+
                        
                        Math.Sqrt(Math.Pow(segments[i, 2].Vertex.X - segments[i, 1].Vertex.X, 2) +
                        Math.Pow(segments[i, 2].Vertex.Y - segments[i, 1].Vertex.Y, 2) +
                        Math.Pow(segments[i, 2].Vertex.Z - segments[i, 1].Vertex.Z, 2))+
                        
                        Math.Sqrt(Math.Pow(segments[i, 3].Vertex.X - segments[i, 2].Vertex.X, 2) +
                        Math.Pow(segments[i, 3].Vertex.Y - segments[i, 2].Vertex.Y, 2) +
                        Math.Pow(segments[i, 3].Vertex.Z - segments[i, 2].Vertex.Z, 2));

                    double L2 = 1.01 * (L3+L1)/2;
                    int N = (Int32)Math.Round(L2/step);//количество шагов кривой

                    if (N < Nb)
                        N = Nb;

                    double d = 1.0 / N; //шаг дельта t
                    SurfacePoint[] p = new SurfacePoint[4];

                    for (int j = 0; j < 4; j++)
                    {
                        p[j] = segments[i, j];
                    }

                    SurfacePoint q = new SurfacePoint();
                    SurfacePoint r = CastR(p, t, 3, 0).Clone();

                    var index = new Topomatic.Sfc.PointEditor(surface).Add(r);
                    newline.Add(index);

                    for (int k = 0; k < N; k++)
                    {
                        t += d;
                        q = CastR(p, t, 3, 0).Clone();
                        r = q.Clone();
                        r.IsExtended = true;
                        r.IsDynamic = true;
                        index = new Topomatic.Sfc.PointEditor(surface).Add(r);
                        newline.Add(index);
                        
                    }
                }
                var ind = new Topomatic.Sfc.PointEditor(surface).Add(segments[segments.GetLength(0)-1,3]);
                newline.Add(ind);

                surface.StructureLines.Add(newline);

                for (int l = 0; l < line.Count; l++)
                {
                    line.Remove(new StructureLineNode(line[l].Index));
                }
                surface.StructureLines.Remove(line);


            }
            finally
            {
                surface.EndUpdate();
            }

        }

       public double DirAngle(SurfacePoint begin, SurfacePoint end)
        {
            double xb, xe, yb, ye, rumb, dy, dx;

            xb = begin.Vertex.X;//разобраться с отрицательными координатами
            xe = end.Vertex.X;
            yb = begin.Vertex.Y;
            ye = end.Vertex.Y;

            dy = (ye - yb);
            dx = (xe - xb);
            rumb = Math.Atan( dx / dy);//???????????

            if (dx >=0)
            {
                if (dy >= 0)
                { return rumb; }
                else
                { return Math.PI - Math.Abs(rumb); }
            }
            else
            {
                if (dy <= 0)
                { return rumb + Math.PI; }
                else
                { return 2 * Math.PI - Math.Abs(rumb); }

            }
        }

        SurfacePoint Lin1(SurfacePoint p1, SurfacePoint p2, double t)
        {
            SurfacePoint q = new SurfacePoint();

            q.Vertex.X = p2.Vertex.X * t + p1.Vertex.X * (1 - t);
            q.Vertex.Y = p2.Vertex.Y * t + p1.Vertex.Y * (1 - t);
            q.Vertex.Z = p2.Vertex.Z * t + p1.Vertex.Z * (1 - t);
            return q;
        }

        SurfacePoint CastR(SurfacePoint[] p, double t, int n, int m)
        {
            if (n == 0)
                return p[m];
            else
                return Lin1(CastR(p, t, n - 1, m), CastR(p, t, n - 1, m + 1), t);
        }
    }
}