using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Game.Enemies.Spawning
{
    public static class EnemyPatternCsvParser
    {
        private static readonly string[]
            RequiredHeaders =
            {
                "EventId",
                "SpawnTime",
                "EnemyId",
                "SpawnPointId",
                "RouteId"
            };

        public static bool TryParse(
            string csvText,
            out List<EnemyPatternEntry> entries,
            out List<string> errors
        )
        {
            entries =
                new List<EnemyPatternEntry>();

            errors =
                new List<string>();

            if (
                string.IsNullOrWhiteSpace(
                    csvText
                )
            )
            {
                errors.Add(
                    "CSVが空です。"
                );

                return false;
            }

            List<List<string>> rows =
                ParseRows(
                    csvText
                );

            if (rows.Count == 0)
            {
                errors.Add(
                    "CSVに行がありません。"
                );

                return false;
            }

            Dictionary<string, int> headerMap =
                BuildHeaderMap(
                    rows[0]
                );

            for (
                int index = 0;
                index < RequiredHeaders.Length;
                index++
            )
            {
                string required =
                    RequiredHeaders[index];

                if (!headerMap.ContainsKey(required))
                {
                    errors.Add(
                        $"必須列「{required}」がありません。"
                    );
                }
            }

            if (errors.Count > 0)
            {
                return false;
            }

            HashSet<string> eventIds =
                new HashSet<string>(
                    StringComparer.OrdinalIgnoreCase
                );

            for (
                int rowIndex = 1;
                rowIndex < rows.Count;
                rowIndex++
            )
            {
                List<string> row =
                    rows[rowIndex];

                int lineNumber =
                    rowIndex + 1;

                if (IsEmptyRow(row))
                {
                    continue;
                }

                string eventId =
                    GetCell(
                        row,
                        headerMap["EventId"]
                    );

                string spawnTimeText =
                    GetCell(
                        row,
                        headerMap["SpawnTime"]
                    );

                string enemyId =
                    GetCell(
                        row,
                        headerMap["EnemyId"]
                    );

                string spawnPointId =
                    GetCell(
                        row,
                        headerMap["SpawnPointId"]
                    );

                string routeId =
                    GetCell(
                        row,
                        headerMap["RouteId"]
                    );

                if (
                    string.IsNullOrWhiteSpace(
                        eventId
                    )
                )
                {
                    errors.Add(
                        $"Line {lineNumber}: "
                        + "EventIdが空です。"
                    );

                    continue;
                }

                if (!eventIds.Add(eventId))
                {
                    errors.Add(
                        $"Line {lineNumber}: "
                        + $"EventId「{eventId}」が"
                        + "重複しています。"
                    );

                    continue;
                }

                if (
                    !float.TryParse(
                        spawnTimeText,
                        NumberStyles.Float,
                        CultureInfo.InvariantCulture,
                        out float spawnTime
                    )
                )
                {
                    errors.Add(
                        $"Line {lineNumber}: "
                        + $"SpawnTime「{spawnTimeText}」を"
                        + "数値へ変換できません。"
                    );

                    continue;
                }

                if (spawnTime < 0.0f)
                {
                    errors.Add(
                        $"Line {lineNumber}: "
                        + "SpawnTimeは0以上にしてください。"
                    );

                    continue;
                }

                if (
                    string.IsNullOrWhiteSpace(
                        enemyId
                    )
                    ||
                    string.IsNullOrWhiteSpace(
                        spawnPointId
                    )
                    ||
                    string.IsNullOrWhiteSpace(
                        routeId
                    )
                )
                {
                    errors.Add(
                        $"Line {lineNumber}: "
                        + "EnemyId、SpawnPointId、RouteIdは"
                        + "必須です。"
                    );

                    continue;
                }

                entries.Add(
                    new EnemyPatternEntry(
                        lineNumber,
                        eventId.Trim(),
                        spawnTime,
                        enemyId.Trim(),
                        spawnPointId.Trim(),
                        routeId.Trim()
                    )
                );
            }

            entries.Sort(
                (
                    left,
                    right
                ) =>
                    left.SpawnTime.CompareTo(
                        right.SpawnTime
                    )
            );

            return errors.Count == 0;
        }

        private static Dictionary<string, int>
            BuildHeaderMap(
                List<string> headerRow
            )
        {
            Dictionary<string, int> map =
                new Dictionary<string, int>(
                    StringComparer.OrdinalIgnoreCase
                );

            for (
                int index = 0;
                index < headerRow.Count;
                index++
            )
            {
                string header =
                    headerRow[index].Trim();

                if (!map.ContainsKey(header))
                {
                    map.Add(
                        header,
                        index
                    );
                }
            }

            return map;
        }

        private static string GetCell(
            List<string> row,
            int index
        )
        {
            if (
                index < 0
                ||
                index >= row.Count
            )
            {
                return string.Empty;
            }

            return row[index].Trim();
        }

        private static bool IsEmptyRow(
            List<string> row
        )
        {
            for (
                int index = 0;
                index < row.Count;
                index++
            )
            {
                if (
                    !string.IsNullOrWhiteSpace(
                        row[index]
                    )
                )
                {
                    return false;
                }
            }

            return true;
        }

        private static List<List<string>>
            ParseRows(
                string text
            )
        {
            List<List<string>> rows =
                new List<List<string>>();

            List<string> currentRow =
                new List<string>();

            StringBuilder currentCell =
                new StringBuilder();

            bool insideQuotes =
                false;

            for (
                int index = 0;
                index < text.Length;
                index++
            )
            {
                char character =
                    text[index];

                if (character == '"')
                {
                    bool escapedQuote =
                        insideQuotes
                        &&
                        index + 1 < text.Length
                        &&
                        text[index + 1] == '"';

                    if (escapedQuote)
                    {
                        currentCell.Append('"');
                        index++;
                    }
                    else
                    {
                        insideQuotes =
                            !insideQuotes;
                    }

                    continue;
                }

                if (
                    character == ','
                    &&
                    !insideQuotes
                )
                {
                    currentRow.Add(
                        currentCell.ToString()
                    );

                    currentCell.Clear();

                    continue;
                }

                bool isLineBreak =
                    character == '\n'
                    ||
                    character == '\r';

                if (
                    isLineBreak
                    &&
                    !insideQuotes
                )
                {
                    if (
                        character == '\r'
                        &&
                        index + 1 < text.Length
                        &&
                        text[index + 1] == '\n'
                    )
                    {
                        index++;
                    }

                    currentRow.Add(
                        currentCell.ToString()
                    );

                    currentCell.Clear();

                    rows.Add(
                        currentRow
                    );

                    currentRow =
                        new List<string>();

                    continue;
                }

                currentCell.Append(
                    character
                );
            }

            if (
                currentCell.Length > 0
                ||
                currentRow.Count > 0
            )
            {
                currentRow.Add(
                    currentCell.ToString()
                );

                rows.Add(
                    currentRow
                );
            }

            return rows;
        }
    }
}