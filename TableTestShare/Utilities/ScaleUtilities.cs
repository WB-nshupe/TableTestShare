using Autodesk.AutoCAD.DatabaseServices;

namespace WarmBoardTools.Utilities
{
    public static class ScaleUtilities
    {
        public static StandardScaleType FromStringToScaleType(string scale)
        {
            if (scale.Contains("1/4")) return StandardScaleType.Scale1To4inchAnd1ft;
            if (scale.Contains("3/16")) return StandardScaleType.Scale3To16inchAnd1ft;
            if (scale.Contains("1/8")) return StandardScaleType.Scale1To8inchAnd1ft;
            if (scale.Contains("3/32")) return StandardScaleType.Scale3To32inchAnd1ft;
            if (scale.Contains("1/16")) return StandardScaleType.Scale1To16inchAnd1ft;
            if (scale.Contains("1/32")) return StandardScaleType.Scale1To32inchAnd1ft;
            if (scale.Contains("1/64")) return StandardScaleType.Scale1To64inchAnd1ft;

            return StandardScaleType.CustomScale;
        }

        public static StandardScaleType GetPrelimScale(string inputString)
        {
            var input = FromStringToScaleType(inputString);

            input -= 2;

            return input < StandardScaleType.Scale1To128inAnd1ft ? StandardScaleType.CustomScale : input;
        }

        public static string GetPrelimScaleString(string inputString)
        {
            
            var answer = GetPrelimScale(inputString);

            return GetScaleString(answer);
        }

        public static string GetScaleString(StandardScaleType input)
        {
            if (input == StandardScaleType.Scale1To4inchAnd1ft) return "1/4\" = 1'";
            if (input == StandardScaleType.Scale3To16inchAnd1ft) return "3/16\" = 1'";
            if (input == StandardScaleType.Scale1To8inchAnd1ft) return "1/8\" = 1'";
            if (input == StandardScaleType.Scale3To32inchAnd1ft) return "3/32\" = 1'";
            if (input == StandardScaleType.Scale1To16inchAnd1ft) return "1/16\" = 1'";
            if (input == StandardScaleType.Scale1To32inchAnd1ft) return "1/32\" = 1'";
            if (input == StandardScaleType.Scale1To64inchAnd1ft) return "1/64\" = 1'";
            if (input == StandardScaleType.Scale1To128inAnd1ft) return "1/128\" = 1'";

            return null;
        }
    }
}