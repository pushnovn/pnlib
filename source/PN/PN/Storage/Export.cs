using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;

using NPOI.HSSF.UserModel;
using NPOI.XSSF.UserModel;
using NPOI.SS.UserModel;
using iTextSharp.text;
using iTextSharp.text.pdf;
using System.Text;
using System.Collections;

namespace PN.Storage
{  
    /// <summary>
    /// Exporting module.
    /// </summary>
    public class ExportЁr
    {
        #region public "interface"

        /// <summary>
        /// Export to XLSX format file.
        /// </summary>
        public static byte[] ToXLSX(bool withHeader, params object[] objects) => ToExport(withHeader, Format.XLSX, objects);

        /// <summary>
        /// Export to XLSX format file.
        /// </summary>
        public static byte[] ToXLS(bool withHeader, params object[] objects) => ToExport(withHeader, Format.XLS, objects);


        /// <summary>
        ///Import List<T> from file (<paramref name="bytes"/>).
        /// </summary>
        public static List<T> FromXLSX<T>(bool withHeader, byte[] bytes) => ToImport<T>(withHeader, Format.XLSX, bytes);

        /// <summary>
        ///Import List<T> from file (<paramref name="bytes"/>).
        /// </summary>
        public static List<T> FromXLS<T>(bool withHeader, byte[] bytes) => ToImport<T>(withHeader, Format.XLS, bytes);


        /// <summary>
        /// Export to PDF format file.
        /// </summary>
        public static byte[] ToPDF(bool withHeader, params object[] objects) => ToExport(withHeader, Format.PDF, objects);

        /// <summary>
        /// Import List<T> from file (<paramref name="bytes"/>).
        /// </summary>
        public static List<T> FromPDF<T>(bool withHeader, byte[] bytes) => ToImport<T>(withHeader, Format.PDF, bytes);


        /// <summary>
        /// Export to PDF format file.
        /// </summary>
        public static byte[] ToCSV(bool withHeader, params object[] objects) => ToExport(withHeader, Format.CSV, objects);


        /// <summary>
        /// Import List<T> from file (<paramref name="bytes"/>).
        /// </summary>
        public static List<T> FromCSV<T>(bool withHeader, byte[] bytes) => ToImport<T>(withHeader, Format.CSV, bytes);

        #endregion

        #region ToExport / ToImport

        private static byte[] ToExport(bool withHeader, Format format, params object[] objects)
        {
            objects = ConvertArrayWithSingleListToArrayOfItems(objects);

            if (objects.Count() == 0)
                return null;

            var props = GetExportPropertiesFromType(objects.FirstOrDefault()?.GetType());

            switch (format)
            {
                case Format.XLSX:
                    return GenerateXLSX(withHeader, objects.ToList(), props);

                case Format.XLS:
                    return GenerateXLS(withHeader, objects.ToList(), props);

                case Format.PDF:
                    return GeneratePDF(withHeader, objects.ToList(), props);

                case Format.CSV:
                    return GenerateCSV(withHeader, objects.ToList(), props);

                default:
                    return null;
            }
        }

        private static List<T> ToImport<T>(bool withHeader, Format format, byte[] bytes)
        {
            var props = GetExportPropertiesFromType(typeof(T));

            switch (format)
            {
                case Format.XLSX:
                    return ParseXLSX<T>(withHeader, bytes, props);

                case Format.XLS:
                    return ParseXLS<T>(withHeader, bytes, props);

                case Format.PDF:
                    return ParsePDF<T>(withHeader, bytes, props);

                case Format.CSV:
                    return ParseCSV<T>(withHeader, bytes, props);

                default:
                    return null;
            }
        }

        #endregion


        #region XLSX

        private static byte[] GenerateXLSX(bool withHeader, List<object> objects, List<PropertyInfo> props) => GenerateExcel(withHeader, objects, props);
        private static byte[] GenerateXLS(bool withHeader, List<object> objects, List<PropertyInfo> props) => GenerateExcel(withHeader, objects, props, false);
        private static byte[] GenerateExcel(bool withHeader, List<object> objects, List<PropertyInfo> props, bool isXlsx = true)
        {
            var workbook = CreateExcelWorkbook(isXlsx);

            var sheet = workbook.CreateSheet(objects.FirstOrDefault().GetType().GetCustomAttribute<ExportNameAttribute>()?.Name ?? "Информация");

            if (withHeader)
            {
                var headerRow = sheet.CreateRow(0);
                headerRow.CreateCell(0).SetCellValue("#");
                for (int i = 0; i < props.Count; i++)
                {
                    headerRow.CreateCell(i + 1).SetCellValue(props[i].GetCustomAttribute<ExportNameAttribute>()?.Name ?? props[i].Name);
                }
            }

            var headerExtraShiftForCycle = withHeader ? 1 : 0;
            for (int j = 0; j < objects.Count; j++)
            {
                var currentRow = sheet.CreateRow(j + headerExtraShiftForCycle);
                currentRow.CreateCell(0).SetCellValue((j + 1).ToString());

                for (int i = 0; i < props.Count; i++)
                {
                    currentRow.CreateCell(i + 1).SetCellValue(props[i].GetValue(objects[j])?.ToString() ?? string.Empty);
                }
            }

            for (int i = 0; i < props.Count; i++)
                sheet.AutoSizeColumn(i);

            using (var stream = new MemoryStream())
            {
                workbook.Write(stream);

                return stream.GetBuffer();
            }
        }

        private static List<T> ParseXLSX<T>(bool withHeader, byte[] bytes, List<PropertyInfo> props) => ParseExcel<T>(withHeader, bytes, props);
        private static List<T> ParseXLS<T>(bool withHeader, byte[] bytes, List<PropertyInfo> props) => ParseExcel<T>(withHeader, bytes, props, false);
        private static List<T> ParseExcel<T>(bool withHeader, byte[] bytes, List<PropertyInfo> props, bool isXlsx = true)
        {
            var resultList = CreateList<T>();
            
            var workbook = CreateExcelWorkbook(isXlsx, bytes);
            
            var sheet = workbook.GetSheet(typeof(T).GetCustomAttribute<ExportNameAttribute>()?.Name ?? "Информация");

            for (int row = withHeader ? 1 : 0; row <= sheet.LastRowNum; row++)
            {
                var currentRow = sheet.GetRow(row);

                if (currentRow == null)
                    continue;

                var resObj = Activator.CreateInstance<T>();

                for (int i = 0; i < props.Count; i++)
                {
                    var strValue = currentRow.GetCell(i + 1).StringCellValue ?? string.Empty;

                    props[i].SetValue(resObj, Convert.ChangeType(strValue, props[i].PropertyType));
                }

                resultList.Add(resObj);
            }

            return resultList;
        }

        static IWorkbook CreateExcelWorkbook(bool isXLSX, byte[] bytes = null)
        {
            IWorkbook workbook;

            if (isXLSX)
                workbook = bytes == null ? new XSSFWorkbook() : new XSSFWorkbook(new MemoryStream(bytes));
            else
                workbook = bytes == null ? new HSSFWorkbook() : new HSSFWorkbook(new MemoryStream(bytes));

            return workbook;
        }

        #endregion

        #region PDF

        private static byte[] GeneratePDF(bool withHeader, List<object> objects, List<PropertyInfo> props)
        {
            using (var stream = new MemoryStream())
            using (var document = new Document(PageSize.A4, 10, 10, 10, 10))
            using (var writer = PdfWriter.GetInstance(document, stream))
            {
                document.Open();

                var table = new PdfPTable(props.Count + 1);

                if (withHeader)
                {
                    table.AddCell("#");
                    for (int i = 0; i < props.Count; i++)
                    {
                        table.AddCell(CreatePhrase(props[i].GetCustomAttribute<ExportNameAttribute>()?.Name ?? props[i].Name));
                    }
                }
                
                for (int j = 0; j < objects.Count; j++)
                {
                    table.AddCell((j + 1).ToString());

                    for (int i = 0; i < props.Count; i++)
                    {
                        table.AddCell(CreatePhrase(props[i].GetValue(objects[j])?.ToString() ?? string.Empty));
                    }
                }

                #region Meta info for parsing future importing PDFs

                var field = new TextField(writer, new Rectangle(0, 0, 0, 0), "hidden-text")
                {
                    Text = Encoding.UTF8.GetString(ToCSV(withHeader, objects)),
                    Visibility = BaseField.HIDDEN,
                };
                writer.AddAnnotation(field.GetTextField());
                
                #endregion
                
                document.Add(table);

                document.Close();

                return stream.GetBuffer();
            }
        }

        private static List<T> ParsePDF<T>(bool withHeader, byte[] bytes, List<PropertyInfo> props)
        {
            var pdfReader = new PdfReader(bytes);
            var pageDict = pdfReader.GetPageN(1);
            var annotArray = pageDict.GetAsArray(PdfName.ANNOTS);
            var curAnnot = annotArray.GetAsDict(0);
            var strWithCSV = curAnnot.GetAsString(PdfName.V).ToString();
            var bytesFromCSV = Encoding.UTF8.GetBytes(strWithCSV);
            return FromCSV<T>(withHeader, bytesFromCSV);
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

        private static byte[] GenerateCSV(bool withHeader, List<object> objects, List<PropertyInfo> props)
        {
            var result = string.Empty;

            if (withHeader)
            {
                result += "#";
                for (int i = 0; i < props.Count; i++)
                {
                    result += $",{(props[i].GetCustomAttribute<ExportNameAttribute>()?.Name ?? props[i].Name)}";
                }
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

            return Encoding.UTF8.GetBytes(result.Trim());
        }


        private static List<T> ParseCSV<T>(bool withHeader, byte[] bytes, List<PropertyInfo> props)
        {
            var resultList = CreateList<T>();

            var text = Encoding.UTF8.GetString(bytes);

            var rows = text.Split(new string[] { Environment.NewLine }, StringSplitOptions.None).ToList();
            if (withHeader)
                rows.RemoveAt(0);

            foreach (var row in rows)
            {
                var values = row.Split(',').ToList();

                var resObj = Activator.CreateInstance<T>();

                for (int i = 0; i < props.Count; i++)
                {
                    var strValue = values[i + 1] ?? string.Empty;

                    props[i].SetValue(resObj, Convert.ChangeType(strValue, props[i].PropertyType));
                }

                resultList.Add(resObj);
            }

            return resultList;
        }

        #endregion


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

        #region Utils

        private enum Format
        {
            XLS, XLSX, PDF, CSV
        }

        static BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.NonPublic |
                                           BindingFlags.Static | BindingFlags.Instance |
                                           BindingFlags.DeclaredOnly;

        static List<PropertyInfo> GetExportPropertiesFromType(Type resultType)
        {
            return resultType.GetProperties(bindingFlags).Where(prop => prop.GetCustomAttribute<ExportIgnoreAttribute>() == null).ToList();
        }

        private static object[] ConvertArrayWithSingleListToArrayOfItems(object[] data)
        {
            var values = new List<object>();

            foreach (var dat in data ?? new object[0])
            {
                if (dat is IEnumerable enumerable && enumerable is string == false)
                {
                    foreach (object obj in enumerable)
                    {
                        values.Add(obj);
                    }
                }
                else
                {
                    values.Add(dat);
                }
            }

            return values.ToArray();
        }
        
        static List<T> CreateList<T>()
        {
            try
            {
                var genericListType = typeof(List<>).MakeGenericType(typeof(T));
                return (List<T>)Activator.CreateInstance(genericListType);
            }
            catch
            {
                return null;
            }
        }

        #endregion
    }
}