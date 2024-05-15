using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using Autodesk.AutoCAD.DatabaseServices;

namespace WarmBoardTools.Utilities
{
    public static class ResourceFolder
    {
        private static bool _isInitialized = false;


        private static readonly string ResourceFolderPath = @"C:\Program Files\Autodesk\ApplicationPlugins\WarmBoardTools.bundle\Resources\";


        private static readonly string TemplateFileName = "Template.dwg";
        private static readonly string TemplatePath = Path.Combine(ResourceFolderPath, TemplateFileName);
        public static bool TemplateExists => File.Exists(TemplatePath);
        private static Database Template
        {
            get
            {
                if (!TemplateExists) return null;

                Database template = new Database(false, true);
                template.ReadDwgFile(TemplatePath, FileShare.Read, true, "");
                return template;
            }
        }


        private static readonly string BlockResourceFolderName = "Blocks";
        private static readonly string BlockResourceFolderPath = Path.Combine(ResourceFolderPath, BlockResourceFolderName);
        public static List<string> BlockResourceFilesList = new List<string>()
        {
            Path.Combine(BlockResourceFolderPath, "WBS_Blocks.dwg"),
            Path.Combine(BlockResourceFolderPath, "WBR_Blocks.dwg"),
            Path.Combine(BlockResourceFolderPath, "Modeling_Blocks.dwg"),
            Path.Combine(BlockResourceFolderPath, "Tubing_Blocks.dwg"),
            Path.Combine(BlockResourceFolderPath, "Paperspace_Blocks.dwg"),
            Path.Combine(BlockResourceFolderPath, "WCS_Blocks.dwg"),
        };
        public static bool BlocksFolderExists => Directory.Exists(BlockResourceFolderPath);


        public static bool Init()
        {
            if (Directory.Exists(ResourceFolderPath)) _isInitialized = true;

            return _isInitialized;
        }

        public static bool ReadTemplate(Action<Database> action, [CallerMemberName] string callerName = "")
        {
            if (!_isInitialized || !TemplateExists) return false;
            using (Database template = Template)
            {
                try
                {
                    if (template == null) return false;
                    action(template);
                }
                catch(Exception e)
                {
                    Active.WriteMessage($"Error in {nameof(ReadTemplate)} called by {callerName}: {e.Message}");
                    return false;
                }
            }

            return true;
        }

        public static bool ReadWriteTemplate(Action<Database> action, [CallerMemberName] string callerName = "")
        {
            if (!_isInitialized) return false;
            Database template = new Database(false, true);
            template.ReadDwgFile(TemplatePath, FileShare.ReadWrite, true, "");

            using (template)
            {
                try
                {
                    if (template == null) return false;
                    action(template);
                }
                catch (Exception e)
                {
                    Active.WriteMessage($"Error in {nameof(ReadTemplate)} called by {callerName}: {e.Message}");
                    return false;
                }
            }

            return true;
        }



        public static class LayerConverter
        {
            private const string LayerConversion = "LayerConversions.txt";
            private static readonly string ConversionPath = Path.Combine(ResourceFolderPath, LayerConversion);


            private static readonly Dictionary<string, string> _conversions = new Dictionary<string, string>();
            public static bool ConverterInitialized { get; private set; } = false;

            static LayerConverter()
            {
                if (File.Exists(ConversionPath))
                {
                    GetLayerConversions();
                    ConverterInitialized = true;
                }
            }

            static void GetLayerConversions()
            {
                using (StreamReader reader = new StreamReader(ConversionPath))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        string[] parts = line.Split('>');
                        _conversions.Add(parts[0], parts[1]);
                    }
                }
            }

            public static bool TryGetConversion(string layer, out string conversion)
            {
                conversion = string.Empty;
                if (!ConverterInitialized || _conversions.Count < 1) return false;
                return _conversions.TryGetValue(layer, out conversion);
            }
        }
    }
}