using System.Text;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Syncfusion.XlsIO;

namespace SchedulerWpfApp.Helper
{
    /// <summary>
    /// Provides utility methods for the scheduler application.
    /// </summary>
    public static class Utility
    {
        /// <summary>
        /// Validates that an Excel worksheet contains data beyond just the header row.
        /// </summary>
        /// <param name="worksheet">The Excel worksheet to be validated.</param>
        /// <remarks>
        /// This method checks if the worksheet has rows beyond the header (row 1) and 
        /// verifies that at least one data row contains non-empty values.
        /// If the worksheet only contains a header row or if all data rows are empty,
        /// an exception is thrown to alert the user.
        /// </remarks>
        /// <exception cref="Exception">Thrown when the worksheet only contains a header row or when all data rows are empty.</exception>
        public static void IsOnlyHeader(IWorksheet worksheet)
        {
            int totalRow = worksheet.UsedRange.LastRow;
            int totalCol = worksheet.UsedRange.LastColumn;

            if (totalRow < 2)
            {
                throw new Exception("File chỉ chứa tiêu đề cột, vui lòng nhập lại!");
            }

            bool allDataRowsEmpty = true;
            for (int row = 2; row <= totalRow; row++)
            {
                bool rowEmpty = false;
                for (int col = 1; col <= totalCol; col++)
                {
                    var value = worksheet[row, col].Value;
                    if (!string.IsNullOrEmpty(value?.ToString().Trim()))
                    {
                        rowEmpty = true;
                        break;
                    }
                }
                if (rowEmpty)
                {
                    allDataRowsEmpty = false;
                    break;
                }
            }

            if (allDataRowsEmpty)
            {
                throw new Exception("File chỉ chứa tiêu đề cột, vui lòng nhập lại!");
            }
        }

        /// <summary>
        /// Checks for empty cells in an Excel worksheet and throws an exception if any are found.
        /// </summary>
        /// <param name="worksheet">The Excel worksheet to be analyzed.</param>
        /// <remarks>
        /// This method checks rows starting from row 2 (skipping the header row),
        /// and identifies rows where at least one cell contains a null or empty value.
        /// If empty cells are found, an exception is thrown with details about which rows contain empty cells.
        /// </remarks>
        /// <exception cref="Exception">Thrown when empty cells are found in the Excel worksheet, with details about which rows have empty cells.</exception>
        public static void IsEmptyExcelRow(IWorksheet worksheet, int totalRow, int totalCol)
        {
            var errorRows = new List<int>();

            for (int row = 2; row <= totalRow; row++)
            {
                bool hasNull = false;

                for (int col = 1; col <= totalCol; col++)
                {
                    var cellValue = worksheet[row, col].Value.ToString();
                    if (string.IsNullOrWhiteSpace(cellValue))
                    {
                        hasNull = true;
                        break;
                    }
                }

                if (hasNull)
                {
                    errorRows.Add(row);
                }
            }

            if (errorRows.Count > 0)
            {
                var message = errorRows.Count == 1
                    ? $"Import thất bại phát hiện ô trống tại dòng: {string.Join(", ", errorRows)}"
                    : $"Import thất bại phát hiện ô trống tại các dòng: {string.Join(", ", errorRows)}";

                throw new Exception(message);
            }
        }

        /// <summary>
        /// Checks for duplicate rows in an Excel worksheet and throws an exception if any are found.
        /// </summary>
        /// <param name="worksheet">The Excel worksheet to be analyzed.</param>
        /// <remarks>
        /// This method checks rows starting from row 2 (skipping the header row),
        /// and identifies rows that have identical content to other rows.
        /// A row is considered a duplicate if all of its cells contain exactly the same values as another row.
        /// If duplicates are found, an exception is thrown with details about which rows are duplicates.
        /// </remarks>
        /// <exception cref="Exception">Thrown when duplicate rows are found in the Excel worksheet, with details about which rows are duplicates.</exception>
        public static void IsRowDuplicated(IWorksheet worksheet, int totalRow, int totalCol)
        {
            var duplicatedMap = new Dictionary<string, int>();
            var duplicates = new List<(int duplicateRow, int originalRow)>();

            for (int row = 2; row <= totalRow; row++)
            {
                var rowData = new StringBuilder();

                for (int col = 1; col <= totalCol; col++)
                {
                    rowData.Append(worksheet[row, col].Value);
                    rowData.Append('|');
                }

                string rowKey = rowData.ToString();

                if (duplicatedMap.TryGetValue(rowKey, out int originalRow))
                {
                    duplicates.Add((row, originalRow));
                }
                else
                {
                    duplicatedMap[rowKey] = row;
                }
            }

            if (duplicates.Any())
            {
                var message = new StringBuilder();
                message.AppendLine("Import thất bại phát hiện các dòng trùng lặp:");

                foreach (var (dupRow, origRow) in duplicates)
                {
                    message.AppendLine($"- Dòng {dupRow} trùng với dòng {origRow}");
                }

                throw new Exception(message.ToString());
            }
        }

        public static void IsColumnDuplicated(IWorksheet worksheet, string colName)
        {
            int colIndex = -1;
            int totalRow = worksheet.UsedRange.LastRow;
            int totalCol = worksheet.UsedRange.LastColumn;

            var duplicatedMap = new Dictionary<string, int>();
            var duplicates = new List<(int duplicateRow, int originalRow)>();

            for (int col = 1; col <= totalCol; col++)
            {
                string header = worksheet[1, col].Value?.ToString();
                if (string.Equals(header, colName, StringComparison.OrdinalIgnoreCase))
                {
                    colIndex = col;
                    break;
                }
            }

            if (colIndex == -1)
            {
                throw new Exception($"Không tìm thấy cột '{colName}' trong bảng tính.");
            }

            for (int row = 2; row <= totalRow; row++)
            {
                string cellValue = worksheet[row, colIndex].Value?.ToString().Trim();

                if (duplicatedMap.TryGetValue(cellValue, out int originalRow))
                {
                    duplicates.Add((row, originalRow));
                }
                else
                {
                    duplicatedMap[cellValue] = row;
                }
            }

            if (duplicates.Any())
            {
                var message = new StringBuilder();
                message.AppendLine($"Import thất bại, phát hiện các dòng trùng lặp trong cột '{colName}':");

                foreach (var (dupRow, origRow) in duplicates)
                {
                    message.AppendLine($"- Dòng {dupRow} trùng với dòng {origRow}");
                }

                throw new Exception(message.ToString());
            }
        }

        public static void IsColumnDuplicatedCSExcel(IWorksheet worksheet)
        {
            int colIndex1 = -1;
            int colIndex2 = -1;
            int colIndex3 = -1;

            int rowCount = worksheet.UsedRange.LastRow;
            int colCount = worksheet.UsedRange.LastColumn;

            var duplicatedMap = new Dictionary<string, int>();
            var duplicates = new List<(int duplicateRow, int originalRow, string value1, string value2, string value3)>();

            string curriculumCode = "CurriculumCode";
            string subjectCode = "SubjectCode";
            string termNo = "TermNo";

            for (int col = 1; col <= colCount; col++)
            {
                string header = worksheet[1, col].Value?.ToString().Trim();

                if (string.Equals(header, curriculumCode, StringComparison.OrdinalIgnoreCase))
                {
                    colIndex1 = col;
                }
                if (string.Equals(header, subjectCode, StringComparison.OrdinalIgnoreCase))
                {
                    colIndex2 = col;
                }
                if (string.Equals(header, termNo, StringComparison.OrdinalIgnoreCase))
                {
                    colIndex3 = col;
                }
            }

            if (colIndex1 == -1 || colIndex2 == -1 || colIndex3 == -1)
            {
                throw new Exception($"Không tìm thấy cột '{curriculumCode}, {subjectCode}, {termNo}' trong file Excel.");
            }

            for (int row = 2; row <= rowCount; row++)
            {
                string value1 = worksheet[row, colIndex1].Value?.ToString().Trim() ?? string.Empty;
                string value2 = worksheet[row, colIndex2].Value?.ToString().Trim() ?? string.Empty;
                string value3 = worksheet[row, colIndex3].Value?.ToString().Trim() ?? string.Empty;

                string compositeKey = $"{value1}\u001F{value2}\u001F{value3}";

                if (duplicatedMap.TryGetValue(compositeKey, out int origRow))
                {
                    duplicates.Add((row, origRow, value1, value2, value3));
                }
                else
                {
                    duplicatedMap[compositeKey] = row;
                }
            }

            if (duplicates.Count > 0)
            {
                var message = new StringBuilder();
                message.AppendLine($"Import thất bại, phát hiện các dòng trùng lặp cùng giá trị ở cột '{curriculumCode}', '{subjectCode}', '{termNo}':");
                foreach (var (dupRow, origRow, value1, value2, value3) in duplicates)
                {
                    message.AppendLine($"- Dòng {dupRow} ({curriculumCode}: '{value1}', {subjectCode}: '{value2}', {termNo}: '{value3}') trùng với dòng {origRow}");
                }
                throw new Exception(message.ToString());
            }
        }

        public static void CheckDuplicateRowsByColumns(IWorksheet worksheet, List<string> columnNames)
        {
            int rowCount = worksheet.UsedRange.LastRow;
            int colCount = worksheet.UsedRange.LastColumn;

            var colIndices = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            for (int col = 1; col <= colCount; col++)
            {
                string header = worksheet[1, col].Value?.ToString().Trim();
                if (header != null && columnNames.Contains(header, StringComparer.OrdinalIgnoreCase))
                {
                    colIndices[header] = col;
                }
            }

            var missingColumns = columnNames.Where(name => !colIndices.ContainsKey(name)).ToList();
            if (missingColumns.Any())
            {
                throw new Exception($"Không tìm thấy các cột sau trong Excel: {string.Join(", ", missingColumns)}");
            }

            var duplicatedMap = new Dictionary<string, int>();
            var duplicates = new List<(int dupRow, int origRow, Dictionary<string, string> values)>();

            for (int row = 2; row <= rowCount; row++)
            {
                var rowValues = new Dictionary<string, string>();

                foreach (var colName in columnNames)
                {
                    int colIndex = colIndices[colName];
                    string cellValue = worksheet[row, colIndex].Value?.ToString().Trim() ?? string.Empty;
                    rowValues[colName] = cellValue;
                }

                string compositeKey = string.Join("||", rowValues.Values);

                if (duplicatedMap.TryGetValue(compositeKey, out int origRow))
                {
                    duplicates.Add((row, origRow, new Dictionary<string, string>(rowValues)));
                }
                else
                {
                    duplicatedMap[compositeKey] = row;
                }
            }

            if (duplicates.Any())
            {
                var message = new StringBuilder();
                message.AppendLine($"Import thất bại, phát hiện các dòng trùng lặp theo các cột: {string.Join(", ", columnNames)}");

                foreach (var (dupRow, origRow, values) in duplicates)
                {
                    var valueStr = string.Join(", ", values.Select(kv => $"{kv.Key}: '{kv.Value}'"));
                    message.AppendLine($"- Dòng {dupRow} ({valueStr}) trùng với dòng {origRow}");
                }

                throw new Exception(message.ToString());
            }
        }

        public static void IsColumnDuplicatedLsExcel(IWorksheet worksheet)
        {
            int colIndex1 = -1;
            int colIndex2 = -1;
            int colIndex3 = -1;
            int colIndex4 = -1;

            int rowCount = worksheet.UsedRange.LastRow;
            int colCount = worksheet.UsedRange.LastColumn;

            var duplicatedMap = new Dictionary<string, int>();
            var duplicates = new List<(int duplicateRow, int originalRow, string value1, string value2, string value3, string value4)>();

            string lectureId = "MAGV";
            string subjectCode = "MAMH";
            string major = "NGANH";
            string term = "KY";

            for (int col = 1; col <= colCount; col++)
            {
                string header = worksheet[1, col].Value?.ToString().Trim();

                if (string.Equals(header, lectureId, StringComparison.OrdinalIgnoreCase))
                {
                    colIndex1 = col;
                }
                if (string.Equals(header, subjectCode, StringComparison.OrdinalIgnoreCase))
                {
                    colIndex2 = col;
                }
                if (string.Equals(header, major, StringComparison.OrdinalIgnoreCase))
                {
                    colIndex3 = col;
                }
                if (string.Equals(header, term, StringComparison.OrdinalIgnoreCase))
                {
                    colIndex4 = col;
                }
            }

            if (colIndex1 == -1 || colIndex2 == -1 || colIndex3 == -1 || colIndex4 == -1)
            {
                throw new Exception($"Không tìm thấy cột '{lectureId}, {subjectCode}, {major}, {term}' trong file Excel.");
            }

            for (int row = 2; row <= rowCount; row++)
            {
                string value1 = worksheet[row, colIndex1].Value?.ToString().Trim() ?? string.Empty;
                string value2 = worksheet[row, colIndex2].Value?.ToString().Trim() ?? string.Empty;
                string value3 = worksheet[row, colIndex3].Value?.ToString().Trim() ?? string.Empty;
                string value4 = worksheet[row, colIndex3].Value?.ToString().Trim() ?? string.Empty;


                string compositeKey = $"{value1}\u001F{value2}\u001F{value3}\u001F{value4}";

                if (duplicatedMap.TryGetValue(compositeKey, out int origRow))
                {
                    duplicates.Add((row, origRow, value1, value2, value3, value4));
                }
                else
                {
                    duplicatedMap[compositeKey] = row;
                }
            }

            if (duplicates.Count > 0)
            {
                var message = new StringBuilder();
                message.AppendLine($"Import thất bại, phát hiện các dòng trùng lặp cùng giá trị ở cột '{lectureId}', '{subjectCode}', '{major}, {term}':");
                foreach (var (dupRow, origRow, value1, value2, value3, value4) in duplicates)
                {
                    message.AppendLine($"- Dòng {dupRow} ({lectureId}: '{value1}', {subjectCode}: '{value2}', {major}: '{value3}, {term}: {value4}') trùng với dòng {origRow}");
                }
                throw new Exception(message.ToString());
            }
        }
    }
}

