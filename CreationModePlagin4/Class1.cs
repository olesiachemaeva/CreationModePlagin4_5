﻿using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CreationModePlagin4
{ 

    [Transaction(TransactionMode.Manual)] 

    public class Main : IExternalCommand
{
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            Document doc = commandData.Application.ActiveUIDocument.Document; 

            Level level1, level2;
            TakeLevels(doc, out level1, out level2);
            CreateWalls(doc, level1, level2);
            return Result.Succeeded;
        }

        private static void CreateWalls(Document doc, Level level1, Level level2)
        {
            double width = UnitUtils.ConvertToInternalUnits(10000, UnitTypeId.Millimeters); 
            double depth = UnitUtils.ConvertToInternalUnits(5000, UnitTypeId.Millimeters); 
            double dx = width / 2;
            double dy = depth / 2;

            List<XYZ> points = new List<XYZ>(); 
            points.Add(new XYZ(-dx, -dy, 0));
            points.Add(new XYZ(dx, -dy, 0));
            points.Add(new XYZ(dx, dy, 0));
            points.Add(new XYZ(-dx, dy, 0));
            points.Add(new XYZ(-dx, -dy, 0));

            List<Wall> walls = new List<Wall>(); 

            Transaction ts = new Transaction(doc, "Построение стен"); 
            ts.Start();
            for (int i = 0; i < 4; i++)
            {
                Line line = Line.CreateBound(points[i], points[i + 1]); 
                Wall wall = Wall.Create(doc, line, level1.Id, false); 
                walls.Add(wall); 
                wall.get_Parameter(BuiltInParameter.WALL_HEIGHT_TYPE).Set(level2.Id); 
            }

            CreateDoor(doc, level1, walls[0]);
            CreateWindow(doc, level1, walls[1]);
            CreateWindow(doc, level1, walls[2]);
            CreateWindow(doc, level1, walls[3]);

            ts.Commit();
        }

        private static void CreateWindow(Document doc, Level level1, Wall wall)
        {
            FamilySymbol winType = new FilteredElementCollector(doc)
                .OfClass(typeof(FamilySymbol))
                .OfCategory(BuiltInCategory.OST_Windows)
                .OfType<FamilySymbol>()
                .Where(x => x.Name.Equals("0915 x 1220 мм"))
                .Where(x => x.FamilyName.Equals("Фиксированные"))
                .FirstOrDefault();

            LocationCurve hostCurve = wall.Location as LocationCurve;
            XYZ point1 = hostCurve.Curve.GetEndPoint(0);
            XYZ point2 = hostCurve.Curve.GetEndPoint(1);
            XYZ point = (point1 + point2) / 2;

            if (!winType.IsActive)
                winType.Activate();

            var window = doc.Create.NewFamilyInstance(point, winType, wall, level1, StructuralType.NonStructural);
            Parameter sillHeight = window.get_Parameter(BuiltInParameter.INSTANCE_SILL_HEIGHT_PARAM);
            double sh = UnitUtils.ConvertToInternalUnits(900, UnitTypeId.Millimeters);
            sillHeight.Set(sh);
        }


        private static void CreateDoor(Document doc, Level level1, Wall wall)
        {
            FamilySymbol doorType = new FilteredElementCollector(doc)
                .OfClass(typeof(FamilySymbol))
                .OfCategory(BuiltInCategory.OST_Doors) 
                .OfType<FamilySymbol>() 
                .Where(x => x.Name.Equals("0915 x 2134 мм"))
                .Where(x => x.FamilyName.Equals("Одиночные-Щитовые"))
                .FirstOrDefault();

            // точка в кт будем добавлять дверь
            LocationCurve hostCurve = wall.Location as LocationCurve;
            XYZ point1 = hostCurve.Curve.GetEndPoint(0);
            XYZ point2 = hostCurve.Curve.GetEndPoint(1);
            XYZ point = (point1 + point2) / 2;

            if (!doorType.IsActive)
                doorType.Activate();

            doc.Create.NewFamilyInstance(point, doorType, wall, level1, StructuralType.NonStructural);
        }

        private static void TakeLevels(Document doc, out Level level1, out Level level2)
        {
            List<Level> listLevel = new FilteredElementCollector(doc)
                .OfClass(typeof(Level)) // отфильтровали уровни
                .OfType<Level>()
                .ToList();

            level1 = listLevel
                .Where(x => x.Name.Equals("Уровень 1"))
                .FirstOrDefault();
            level2 = listLevel
                .Where(x => x.Name.Equals("Уровень 2"))
                .FirstOrDefault();
        }

        //public Result Execute(ExternalCommandData commandData, ref string message, Autodesk.Revit.DB.ElementSet elements)
        //{
        //    throw new NotImplementedException();
        //}
    }
}
