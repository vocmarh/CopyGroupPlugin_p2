using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CopyGroupPlugin_p2
{
    [TransactionAttribute(TransactionMode.Manual)]


    public class CopyGroupCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                UIDocument uidoc = commandData.Application.ActiveUIDocument;
                Document doc = uidoc.Document;

                GroupPickFilter groupPickFilter = new GroupPickFilter(); 		
                Reference reference = uidoc.Selection.PickObject(ObjectType.Element, groupPickFilter, "Выберите группу для копирования");
                Element element = doc.GetElement(reference);
                Group group = element as Group;
                XYZ groupCenter = GetElementCenter(group);		
                Room room = GetRoomByPoint(doc, groupCenter);	
                XYZ roomCenter = GetElementCenter(room);		
                XYZ offset = groupCenter - roomCenter;			

                XYZ point = uidoc.Selection.PickPoint("Выберите точку для вставки");
                Room selectedRoom = GetRoomByPoint(doc, point); 		
                XYZ selectedRoomCenter = GetElementCenter(selectedRoom); 
                XYZ pastePoint = selectedRoomCenter + offset; 	

                Transaction ts = new Transaction(doc);
                ts.Start("Идет копирование группы объектов");
                doc.Create.PlaceGroup(pastePoint, group.GroupType);
                ts.Commit();
            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException)  
            {
                return Result.Cancelled;		
            }
            catch (Exception ex)
            {
                message = ex.Message;			
                return Result.Failed;			
            }

            return Result.Succeeded;
        }
        public XYZ GetElementCenter(Element element)		
        {
            BoundingBoxXYZ bounding = element.get_BoundingBox(null);
            return (bounding.Max + bounding.Min) / 2;			
        }

        public Room GetRoomByPoint(Document doc, XYZ point) 		
        {
            FilteredElementCollector collector = new FilteredElementCollector(doc);    
            collector.OfCategory(BuiltInCategory.OST_Rooms);				
            foreach (Element e in collector)
            {
                Room room = e as Autodesk.Revit.DB.Architecture.Room;		
                if (room != null)
                {
                    if (room.IsPointInRoom(point))		
                    {
                        return room;
                    }
                }
            }
            return null;
        }

        public class GroupPickFilter : ISelectionFilter				
        {
            public bool AllowElement(Element elem)
            {
                if (elem.Category.Id.IntegerValue == (int)BuiltInCategory.OST_IOSModelGroups)  
                { return true; }
                else return false;
            }
            public bool AllowReference(Reference reference, XYZ position)  	
            {
                return false;
            }
        }
    }

}
