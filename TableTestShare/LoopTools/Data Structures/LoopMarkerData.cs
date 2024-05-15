using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Text.RegularExpressions;

namespace TableTest.LoopTools
{
    public class LoopMarkerData : IEquatable<LoopMarkerData>, ICloneable
    {
        private string _zoneId;
        public string ZoneId
        {
            get => _zoneId;
            set
            {
                var match = Regex.Match(value, @"\d+");
                if (match.Success && int.TryParse(match.Value, out int num)) ZoneNum = num;

                var sysMatch = Regex.Match(value, @"[a-zA-Z]+");
                if (sysMatch.Success) System = sysMatch.Value;

                _zoneId = value;
            }
        }
        public int? ZoneNum { get; private set; }
        public string System { get; private set; }

        public string Manifold { get; set; }

        public string Loop { get; set; }


        
        public string LengthTotal => LengthInt + "'";
        public int LengthInt => LengthAddInt + LengthMainInt;


        private string _lengthAdd;
        public string LengthAdd
        {
            get => _lengthAdd;
            set => _lengthAdd = value.Trim().TrimEnd('\'');
        }
        public int LengthAddInt => int.TryParse(LengthAdd, out int num) ? num : 0;


        private string _lengthMain;
        public string LengthMain
        {
            get => _lengthMain;
            set => _lengthMain = value.Trim().TrimEnd('\'');
        }
        public int LengthMainInt => int.TryParse(LengthMain, out int num) ? num : 0;


        private string _lengthDisplay;
        public string LengthDisplay
        {
            get => _lengthDisplay;
            set => _lengthDisplay = value.Trim().TrimEnd('\'');
        }


        public string RoomName { get; set; }
        public bool Slab { get; set; } = false;
        public long Handle { get; set; }
        public ObjectId Id { get; } = ObjectId.Null;
        public Point3d MarkerPosition { get; private set; } = Point3d.Origin;

        public bool Equals(LoopMarkerData other)
        {
            if (other == null) return false;
            bool equals = LengthTotal.Equals(other.LengthTotal);
            if (!Manifold.Equals(other.Manifold)) equals = false;
            if (!Loop.Equals(other.Loop)) equals = false;
            if (!ZoneId.Equals(other.ZoneId)) equals = false;
            return equals;
        }

        public LoopMarkerData(BlockReference marker, Transaction tr)
        {
            Handle = marker.ObjectId.Handle.Value;
            MarkerPosition = marker.Position;
            foreach (ObjectId attId in marker.AttributeCollection)
            {
                using (AttributeReference ar = tr.GetObject(attId, OpenMode.ForRead, false, true) as AttributeReference)
                {
                    if (ar == null) continue;
                    switch (ar.Tag)
                    {
                        case "ZONE":
                            {
                                ZoneId = ar.TextString;
                                break;
                            }
                        case "MANIFOLD":
                            {
                                Manifold = ar.TextString;
                                break;
                            }
                        case "LOOP":
                            {
                                Loop = ar.TextString.Trim();
                                break;
                            }
                        case "LENGTH":
                            {
                                LengthMain = ar.TextString;
                                break;
                            }
                        case "LENGTH_ADD":
                            {
                                LengthAdd = ar.TextString;
                                break;
                            }
                        case "LENGTH_DISPLAY":
                            {
                                LengthDisplay = ar.TextString;
                                break;
                            }
                        case "ROOM":
                            {
                                RoomName = ar.TextString;
                                break;
                            }
                        case "SLAB":
                            {
                                Slab = ar.TextString.ToUpper() != "FALSE";
                                break;
                            }

                    }
                }
            }
        }

        private LoopMarkerData(LoopMarkerData data)
        {
            ZoneId = data.ZoneId;
            Manifold = data.Manifold;
            Loop = data.Loop;
            LengthMain = data.LengthMain;
            LengthAdd = data.LengthAdd;
            LengthDisplay = data.LengthDisplay;
            RoomName = data.RoomName;
            Slab = data.Slab;
            Handle = data.Handle;
            Id = data.Id;
            MarkerPosition = data.MarkerPosition;
        }

        public object Clone()
        {
            return new LoopMarkerData(this);
        }
    }
}
