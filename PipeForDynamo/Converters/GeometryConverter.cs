﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PipeDataModel.Types;
using ppg = PipeDataModel.Types.Geometry;
using ppc = PipeDataModel.Types.Geometry.Curve;
using pps = PipeDataModel.Types.Geometry.Surface;
using dg = Autodesk.DesignScript.Geometry;

namespace PipeForDynamo.Converters
{
    internal class GeometryConverter : PipeConverter<dg.Geometry, IPipeMemberType>
    {
        internal PointConverter ptConv;

        internal GeometryConverter() :
            base()
        {
            ptConv = new PointConverter();
            AddConverter(ptConv);
            var vecConv = new VectorConverter();
            var curConv = new CurveConverter(ptConv, vecConv);
            AddConverter(curConv);
            var surfConv = new SurfaceConverter(vecConv, ptConv, curConv);
            AddConverter(surfConv);

            //solids - only one way mapping
            AddConverter(new PipeConverter<dg.Solid, pps.PolySurface>(
                (ds) => {
                    List<List<int>> adjacency = new List<List<int>>();
                    var faces = ds.Faces.Select((f) => {
                        var surf = (pps.NurbsSurface)surfConv.ToPipe<dg.Surface, pps.Surface>(f.SurfaceGeometry().ToNurbsSurface());
                        surf.OuterTrims.Clear();
                        try
                        {
                            var closedTrim = dg.PolyCurve.ByJoinedCurves(f.Edges.Select((e) => e.CurveGeometry));
                            surf.OuterTrims.Add(curConv.ToPipe<dg.Curve, ppc.Curve>(closedTrim));
                        }
                        catch (Exception e)
                        {
                            //do nothing
                            //surf.OuterTrims.AddRange(ds.Edges.Select((edge) => curConv.ToPipe<dg.Curve, ppc.Curve>(edge.CurveGeometry)));
                        }
                        adjacency.Add(f.Edges.SelectMany((e) => e.AdjacentFaces.ToList()).Distinct().Select((af) =>
                                ds.Faces.ToList().IndexOf(af)).ToList());
                        return (pps.Surface)surf;
                    }).ToList();
                    return new pps.PolySurface(faces, adjacency);
                },
                null// null because of oneway mapping
            ));
        }
    }

    internal class PointConverter : PipeConverter<dg.Point, ppg.Vec>
    {
        internal PointConverter() :
            base(
                    (pt) => { return new ppg.Vec(pt.X, pt.Y, pt.Z); },
                    (ppt) =>
                    {
                        var pt = ppg.Vec.Ensure3D(ppt);
                        return dg.Point.ByCoordinates(pt.Coordinates[0], pt.Coordinates[1], pt.Coordinates[2]);
                    }
                )
        { }
    }

    internal class VectorConverter: PipeConverter<dg.Vector, ppg.Vec>
    {
        internal VectorConverter():
            base(
                    (v) => { return new ppg.Vec(v.X, v.Y, v.Z); },
                    (pvec) => {
                        var pt = ppg.Vec.Ensure3D(pvec);
                        return dg.Vector.ByCoordinates(pt.Coordinates[0], pt.Coordinates[1], pt.Coordinates[2]);
                    }
                )
        { }
    }
}
