using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using iTextSharp.text;
using iTextSharp.text.pdf;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Internal;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;

// ReSharper disable All

namespace PN.Storage
{
    /// <summary>
    ///     Exporting module.
    /// </summary>
    public class ExportЁr
    {
        #region public "interface"

        /// <summary>
        ///     Export to XLSX format file.
        /// </summary>
        public static byte[] ToXLSX(bool withHeader, params object[] objects) => ToExport(withHeader, ExportFormat.XLSX, objects);

        /// <summary>
        ///     Export to XLSX format file.
        /// </summary>
        public static byte[] ToXLS(bool withHeader, params object[] objects) => ToExport(withHeader, ExportFormat.XLS, objects);


        /// <summary>
        ///     Import List<T></T> from file (<paramref name="bytes" />).
        /// </summary>
        public static List<T> FromXLSX<T>(bool withHeader, byte[] bytes, out List<ImportError> errors) => ToImport<T>(withHeader, ExportFormat.XLSX, bytes, out errors);

        /// <summary>
        ///     Import List<T></T> from file (<paramref name="bytes" />).
        /// </summary>
        public static List<T> FromXLS<T>(bool withHeader, byte[] bytes, out List<ImportError> errors) => ToImport<T>(withHeader, ExportFormat.XLS, bytes, out errors);


        /// <summary>
        ///     Export to PDF format file.
        /// </summary>
        public static byte[] ToPDF(bool withHeader, params object[] objects) => ToExport(withHeader, ExportFormat.PDF, objects);

        /// <summary>
        ///     Import List<T></T> from file (<paramref name="bytes" />).
        /// </summary>
        public static List<T> FromPDF<T>(bool withHeader, byte[] bytes, out List<ImportError> errors) => ToImport<T>(withHeader, ExportFormat.PDF, bytes, out errors);


        /// <summary>
        ///     Export to PDF format file.
        /// </summary>
        public static byte[] ToCSV(bool withHeader, params object[] objects) => ToExport(withHeader, ExportFormat.CSV, objects);


        /// <summary>
        ///     Import List<T></T> from file (<paramref name="bytes" />).
        /// </summary>
        public static List<T> FromCSV<T>(bool withHeader, byte[] bytes, out List<ImportError> errors) => ToImport<T>(withHeader, ExportFormat.CSV, bytes, out errors);


        /// <summary>
        ///     Import List<T></T> from file (<paramref name="bytes" />).
        /// </summary>
        public static List<T> FromIndefined<T>(bool withHeader, FormFile iFormFile, out List<ImportError> errors)
        {
            errors = new List<ImportError>();
            if (iFormFile.Length <= 0)
            {
                errors?.Add(new ImportError()
                {
                    Exception = new Exception("IFormFile is empty.")
                });

                return new List<T>();
            }

            var fileExtension = iFormFile.FileName.Split('.').LastOrDefault();
            if (SupportedImportFilesExtensions.IndexOf(fileExtension) == -1)
            {
                errors?.Add(new ImportError()
                {
                    Exception = new Exception("File not supported.")
                });

                return new List<T>();
            }

            using (var ms = new MemoryStream())
            {
                iFormFile.CopyTo(ms);
                return FromIndefined<T>(withHeader, ms.ToArray(), out errors);
            }
        }

        public delegate List<T> ImportDelegate<T>(bool withHeader, byte[] bytes, out List<ImportError> errors);

        /// <summary>
        ///     Import List<T></T> from file (<paramref name="bytes" />).
        /// </summary>
        public static List<T> FromIndefined<T>(bool withHeader, byte[] bytes, out List<ImportError> errors)
        {
            var functions = new ImportDelegate<T>[]
            {
                FromXLSX<T>, FromXLS<T>, FromCSV<T>, FromPDF<T>
            };
            foreach (var del in functions)
            {
                try
                {
                    return del.Invoke(withHeader, bytes, out errors);
                }
                catch { }
            }

            errors = new List<ImportError>();
            errors?.Add(new ImportError()
            {
                Exception = new Exception("Parse failed, sorry.")
            });

            return new List<T>();
        }

        public static byte[] ToSmthByFormat(ExportFormat exportFormat, bool withHeader, params object[] objects)
        {
            switch (exportFormat)
            {
                case ExportFormat.XLSX:
                    return ToXLSX(withHeader, objects);

                case ExportFormat.CSV:
                    return ToCSV(withHeader, objects);

                case ExportFormat.PDF:
                    return ToPDF(withHeader, objects);

                case ExportFormat.XLS:
                    return ToXLS(withHeader, objects);

                default:
                    return null;
            }
        }

        #endregion

        #region ToExport / ToImport

        private static byte[] ToExport(bool withHeader, ExportFormat format, params object[] objects)
        {
            objects = ConvertArrayWithSingleListToArrayOfItems(objects);

            if (objects.Count() == 0)
                return null;

            var props = GetExportPropertiesFromType(objects.FirstOrDefault()?.GetType());

            switch (format)
            {
                case ExportFormat.XLSX:
                    return GenerateXLSX(withHeader, objects.ToList(), props);

                case ExportFormat.XLS:
                    return GenerateXLS(withHeader, objects.ToList(), props);

                case ExportFormat.PDF:
                    return GeneratePDF(withHeader, objects.ToList(), props);

                case ExportFormat.CSV:
                    return GenerateCSV(withHeader, objects.ToList(), props);

                default:
                    return null;
            }
        }

        private static List<T> ToImport<T>(bool withHeader, ExportFormat format, byte[] bytes, out List<ImportError> errors)
        {
            var props = GetExportPropertiesFromType(typeof(T));
            errors = new List<ImportError>();

            switch (format)
            {
                case ExportFormat.XLSX:
                    return ParseXLSX<T>(withHeader, bytes, props, out errors);

                case ExportFormat.XLS:
                    return ParseXLS<T>(withHeader, bytes, props, out errors);

                case ExportFormat.PDF:
                    return ParsePDF<T>(withHeader, bytes, props);

                case ExportFormat.CSV:
                    return ParseCSV<T>(withHeader, bytes, props, out errors);

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
                try
                {
                    sheet.AutoSizeColumn(i);
                }
                catch { }

            using (var stream = new MemoryStream())
            {
                workbook.Write(stream);

                return stream.GetBuffer();
            }
        }


        private static List<T> ParseXLSX<T>(bool withHeader, byte[] bytes, List<PropertyInfo> props, out List<ImportError> errors) =>
                ParseExcel<T>(withHeader, bytes, props, out errors);

        private static List<T> ParseXLS<T>(bool withHeader, byte[] bytes, List<PropertyInfo> props, out List<ImportError> errors) =>
                ParseExcel<T>(withHeader, bytes, props, out errors, false);

        private static List<T> ParseExcel<T>(bool withHeader, byte[] bytes, List<PropertyInfo> props, out List<ImportError> errors, bool isXlsx = true)
        {
            var resultList = CreateList<T>();

            var workbook = CreateExcelWorkbook(isXlsx, bytes);

            var sheet = workbook.GetSheet(typeof(T).GetCustomAttribute<ExportNameAttribute>()?.Name ?? "Информация");

            errors = new List<ImportError>();

            for (int row = withHeader ? 1 : 0; row <= sheet.LastRowNum; row++)
            {
                var currentRow = sheet.GetRow(row);

                if (currentRow == null || currentRow.Cells.All(d => d.CellType == CellType.Blank))
                    continue;

                var resObj = Activator.CreateInstance<T>();

                for (int column = 0; column < props.Count; column++)
                {
                    try
                    {
                        var strValue = currentRow.GetCell(column + 1).StringCellValue ?? string.Empty;

                        props[column].SetValue(resObj, Convert.ChangeType(strValue, props[column].PropertyType));
                    }
                    catch (Exception ex)
                    {
                        errors.Add(new ImportError()
                        {
                            Row = row + (withHeader ? 0 : 1),
                            Column = column + 1,
                            Exception = ex,
                            ColumnName = props[column].GetCustomAttribute<ExportNameAttribute>()?.Name ?? props[column].Name,
                        });
                    }
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
            using (var document = new Document(PropsCountToPageSize(props.Count), 10, 10, 10, 10))
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

                //var field = new TextField(writer, new Rectangle(0, 0, 0, 0), "hidden-text")
                //{
                //    Text = Encoding.UTF8.GetString(ToCSV(withHeader, objects)),
                //    Visibility = BaseField.HIDDEN,
                //};
                //writer.AddAnnotation(field.GetTextField());

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
            var errors = new List<ImportError>();
            return FromCSV<T>(withHeader, bytesFromCSV, out errors);
        }

        private static Phrase CreatePhrase(string text)
        {
            return new Phrase(text, PdfExportFont);
        }

        private static Font _pdfExportFont;
        private static Font PdfExportFont => _pdfExportFont ?? (_pdfExportFont = UpdateFont());

        private static Font UpdateFont()
        {
            try
            {
                var fontName = "Calibri";
                if (!FontFactory.IsRegistered(fontName))
                {
                    string someFontTTF = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "Calibri.TTF");

                    Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

                    FontFactory.Register(someFontTTF);
                }

                return FontFactory.GetFont(fontName, BaseFont.IDENTITY_H, BaseFont.EMBEDDED, 10.0f);
            }
            catch (Exception ex)
            {
                return new Font(Font.FontFamily.TIMES_ROMAN);
            }
        }

        private static Rectangle PropsCountToPageSize(int count)
        {
            if (count <= 6)
                return PageSize.A4;

            if (count <= 12)
                return PageSize.A3;

            if (count <= 18)
                return PageSize.A2;

            if (count <= 24)
                return PageSize.A1;

            return PageSize.A0;
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


        private static List<T> ParseCSV<T>(bool withHeader, byte[] bytes, List<PropertyInfo> props, out List<ImportError> errors)
        {
            var resultList = CreateList<T>();

            var text = Encoding.UTF8.GetString(bytes);

            var rows = text.Split(NewLineSeparator, StringSplitOptions.None).ToList();

            if (withHeader)
                rows.RemoveAt(0);

            errors = new List<ImportError>();

            foreach (var row in rows)
            {
                var values = row.Split(',').ToList();

                var resObj = Activator.CreateInstance<T>();

                for (int column = 0; column < props.Count; column++)
                {
                    try
                    {
                        var strValue = values[column + 1] ?? string.Empty;

                        props[column].SetValue(resObj, Convert.ChangeType(strValue, props[column].PropertyType));
                    }
                    catch (Exception ex)
                    {
                        errors.Add(new ImportError()
                        {
                            Row = rows.IndexOf(row) + 1,
                            Column = column + 1,
                            Exception = ex,
                            ColumnName = props[column].GetCustomAttribute<ExportNameAttribute>()?.Name ?? props[column].Name,
                        });
                    }
                }

                resultList.Add(resObj);
            }

            return resultList;
        }

        #endregion


        #region Attributes

        /// <summary>
        ///     Column name for tables on export.
        /// </summary>
        [AttributeUsage(AttributeTargets.All, Inherited = false, AllowMultiple = false)]
        public class ExportNameAttribute : Attribute
        {
            /// <summary>
            ///     Name of the column for export.
            /// </summary>
            public readonly string Name;

            /// <summary>
            ///     Please, provide name you wish for export column.
            /// </summary>
            public ExportNameAttribute(string name)
            {
                Name = name;
            }
        }

        /// <summary>
        ///     All properties with such attributes will be ignored while exporting.
        /// </summary>
        [AttributeUsage(AttributeTargets.All, Inherited = false, AllowMultiple = false)]
        public class ExportIgnoreAttribute : Attribute { }

        #endregion

        #region Utils

        public class ImportError
        {
            public int Row { get; set; }
            public int Column { get; set; }
            public Exception Exception { get; set; }
            public string ColumnName { get; set; }
        }

        public enum ExportFormat
        {
            XLS, XLSX, PDF, CSV
        }

        private static List<string> SupportedImportFilesExtensions = new List<string>()
        {
            ".csv", ".xls", ".xlsx"
        };

        public static string GetContentType(ExportFormat exportFormat)
        {
            switch (exportFormat)
            {
                case ExportFormat.XLSX:
                    return "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

                case ExportFormat.CSV:
                    return "text/csv";

                case ExportFormat.PDF:
                    return "application/pdf";

                case ExportFormat.XLS:
                    return "application/vnd.ms-excel";

                default:
                    return "application/octet-stream";
            }
        }

        static string[] NewLineSeparator = new string[]
        {
            Environment.NewLine
        };

        static BindingFlags BindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly;

        static List<PropertyInfo> GetExportPropertiesFromType(Type resultType)
        {
            return resultType.GetProperties(BindingFlags).Where(prop => prop.GetCustomAttribute<ExportIgnoreAttribute>() == null).ToList();
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