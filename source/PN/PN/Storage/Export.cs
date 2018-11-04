using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;

using NPOI.XSSF.UserModel;
using iTextSharp.text;
using iTextSharp.text.pdf;
using System.Text;

namespace PN.Storage
{
    /// <summary>
    /// Exporting module.
    /// </summary>
    public class Export
    {
        #region XLSX

        /// <summary>
        /// Export to XLSX format file.
        /// </summary>
        public static byte[] ToXLSX(params object[] objects) => ToExport(Format.XLSX, objects);
        
        private static byte[] GenerateXLSX(List<object> objects, List<PropertyInfo> props)
        {
            var workbook = new XSSFWorkbook();

            var sheet = workbook.CreateSheet(objects.FirstOrDefault().GetType().GetCustomAttribute<ExportNameAttribute>()?.Name ?? "Информация");

            var headerRow = sheet.CreateRow(0);
            headerRow.CreateCell(0).SetCellValue("#");
            for (int i = 0; i < props.Count; i++)
            {
                headerRow.CreateCell(i+1).SetCellValue(props[i].GetCustomAttribute<ExportNameAttribute>()?.Name ?? props[i].Name);
            }

            for (int j = 0; j < objects.Count; j++)
            {
                var currentRow = sheet.CreateRow(j+1);
                currentRow.CreateCell(0).SetCellValue((j+1).ToString());

                for (int i = 0; i < props.Count; i++)
                {
                    currentRow.CreateCell(i + 1).SetCellValue(props[i].GetValue(objects[j])?.ToString() ?? string.Empty);
                }
            }
            
            for (int i = 0; i < props.Count; i++)
                sheet.AutoSizeColumn(i);

            using (var stream = new MemoryStream())
            {
                workbook.Write(stream, true);
                
                return Utils.Converters.StreamToBytes(stream);
            }
        }

        #endregion

        #region PDF

        /// <summary>
        /// Export to PDF format file.
        /// </summary>
        public static byte[] ToPDF(params object[] objects) => ToExport(Format.PDF, objects);

        private static byte[] GeneratePDF(List<object> objects, List<PropertyInfo> props)
        {
            using (var stream = new MemoryStream())
            using (var document = new Document(PageSize.A4, 10, 10, 10, 10))
            using (var writer = PdfWriter.GetInstance(document, stream))
            {
                document.Open();

                var table = new PdfPTable(props.Count + 1);

                table.AddCell("#");
                for (int i = 0; i < props.Count; i++)
                {
                    table.AddCell(CreatePhrase(props[i].GetCustomAttribute<ExportNameAttribute>()?.Name ?? props[i].Name));
                }

                for (int j = 0; j < objects.Count; j++)
                {
                    table.AddCell((j + 1).ToString());

                    for (int i = 0; i < props.Count; i++)
                    {
                        table.AddCell(CreatePhrase(props[i].GetValue(objects[j])?.ToString() ?? string.Empty));
                    }
                }

                document.Add(table);
                
                document.Close();

                return stream.GetBuffer();// Utils.Converters.StreamToBytes(stream);
            }
        }

        private static Phrase CreatePhrase(string text)
        {
            return new Phrase(text, GetCalibri());
        }

        private static Font GetCalibri()
        {
            try
            {
                var fontName = "Calibri";
                if (!FontFactory.IsRegistered(fontName))
                {
                    string someFontTTF = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "Calibri.TTF");
                    FontFactory.Register(someFontTTF);
                }
                return FontFactory.GetFont(fontName, BaseFont.IDENTITY_H, BaseFont.EMBEDDED);
            }
            catch
            {
                return new Font(Font.FontFamily.TIMES_ROMAN);
            }
        }

        #endregion

        #region CSV

        /// <summary>
        /// Export to PDF format file.
        /// </summary>
        public static byte[] ToCSV(params object[] objects) => ToExport(Format.CSV, objects);

        private static byte[] GenerateCSV(List<object> objects, List<PropertyInfo> props)
        {
            var result = "#";
            for (int i = 0; i < props.Count; i++)
            {
                result += $",{(props[i].GetCustomAttribute<ExportNameAttribute>()?.Name ?? props[i].Name)}";
            }

            for (int j = 0; j < objects.Count; j++)
            {
                result += Environment.NewLine;
                result += (j + 1).ToString();

                for (int i = 0; i < props.Count; i++)
                {
                    result += $",{(props[i].GetValue(objects[j])?.ToString() ?? string.Empty)}";
                }
            }

            return Encoding.UTF8.GetBytes(result);
        }

        #endregion

        private static byte[] ToExport(Format format, params object[] objects)
        {
            objects = Utils.Converters.ConvertArrayWithSingleListToArrayOfItems(objects);

            if (objects.Count() == 0)
                return null;

            var props = GetExportPropertiesFromType(objects.FirstOrDefault()?.GetType());

            if (format == Format.XLSX)
                return GenerateXLSX(objects.ToList(), props);
            else if (format == Format.PDF)
                return GeneratePDF(objects.ToList(), props);
            else 
                return GenerateCSV(objects.ToList(), props);
        }

        private enum Format
        {
            XLSX, PDF, CSV
        } 

        private static BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.NonPublic |
                                                   BindingFlags.Static | BindingFlags.Instance |
                                                   BindingFlags.DeclaredOnly;

        static List<PropertyInfo> GetExportPropertiesFromType(Type resultType)
        {
            return resultType.GetProperties(bindingFlags).Where(prop => prop.GetCustomAttribute<ExportIgnoreAttribute>() == null).ToList();
        }

        #region Attributes

        /// <summary>
        /// Column name for tables on export.
        /// </summary>
        [AttributeUsage(AttributeTargets.All, Inherited = false, AllowMultiple = false)]
        public class ExportNameAttribute : Attribute
        {
            /// <summary>
            /// Name of the column for export.
            /// </summary>
            public readonly string Name;

            /// <summary>
            /// Please, provide name you wish for export column.
            /// </summary>
            public ExportNameAttribute(string name)
            {
                Name = name;
            }
        }

        /// <summary>
        /// All properties with such attributes will be ignored while exporting.
        /// </summary>
        [AttributeUsage(AttributeTargets.All, Inherited = false, AllowMultiple = false)]
        public class ExportIgnoreAttribute : Attribute { }

        #endregion
    }
}
