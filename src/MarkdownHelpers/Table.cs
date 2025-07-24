namespace MarkdownHelpers;

public class Table : IDisposable
{
    private readonly IWriter _writer;
    private readonly int[]? _columns;
    private int _columnMax;
    private int _column = 0;
    private int _realLength = 0;
    private int _specLength = 0;
    
    // Dynamic table properties
    private readonly bool _isDynamic;
    private readonly int _maxWidth;
    private readonly double _percentileThreshold;
    private readonly List<string[]> _rows = new();
    private string[]? _headers;
    private int[]? _calculatedColumns;
    private bool _headerWritten = false;
    private bool _disposed = false;
    
    public bool UseOuterPipes { get; set; } = true;

    // Original constructor for backward compatibility
    public Table(IWriter writer, int[] columns)
    {
        _writer = writer;
        _columns = columns;
        _columnMax = columns.Length - 1;
        _isDynamic = false;
        _maxWidth = 0;
        _percentileThreshold = 0;
    }

    // New constructor for dynamic sizing
    public Table(IWriter writer, int maxWidth = 60, double percentileThreshold = 0.5)
    {
        _writer = writer;
        _columns = null;
        _columnMax = -1; // Will be set when headers are provided
        _isDynamic = true;
        _maxWidth = maxWidth;
        _percentileThreshold = percentileThreshold;
    }

    public void EndRow()
    {
        if (_isDynamic)
        {
            // We're collecting data, nothing to write yet
            _column = 0;
            return;
        }

        _column = 0;
        _realLength = 0;
        _specLength = 0;
        _writer.WriteLine();
    }

    public void WriteHeader(ReadOnlySpan<string> labels)
    {
        if (_isDynamic)
        {
            _headers = labels.ToArray();
            _columnMax = _headers.Length - 1;
            _headerWritten = true;
            return;
        }

        for (int i = 0; i < labels.Length; i++)
        {
            WriteColumn(labels[i]);
        }

        EndRow();

        for (int i = 0; i < labels.Length; i++)
        {
            WriteColumn("", '-', true);
        }

        EndRow();
    }

    public void WriteColumn(string text, char repeatCharacter = ' ', bool headerPolicy = false)
    {
        if (_isDynamic)
        {
            if (_column == 0)
            {
                // Starting a new row, create array for this row
                _rows.Add(new string[_columnMax + 1]);
            }

            if (_column <= _columnMax)
            {
                _rows[^1][_column] = text;
            }

            _column++;
            return;
        }

        ThrowIfTooManyColumns(_column);

        int preSpace = 0;
        if (_column > 0 || UseOuterPipes)
        {
            _writer.Write("| ");
            preSpace = 2;
        }

        _writer.Write(text);

        bool lastColumn = _column == _columnMax;
        bool writeEndPipe = lastColumn && UseOuterPipes;
        bool endWithText = lastColumn && !UseOuterPipes && !headerPolicy;
        bool writeRepeatToEnd = headerPolicy && lastColumn && !UseOuterPipes;

        if (endWithText)
        {
            return;
        }

        int postSpace = 1;

        if (writeEndPipe)
        {
            postSpace = 2;
        }
        else if (writeRepeatToEnd)
        {
            postSpace = 0;
        }

        _realLength +=  preSpace + text.Length + postSpace;
        _specLength += _columns![_column];
        _column++;

        int remaining = _specLength - _realLength;
        if (remaining > 0)
        {
            _writer.WriteRepeatCharacter(repeatCharacter, remaining);
            _realLength += remaining;
        }

        if (writeRepeatToEnd)
        {
            return;
        }

        _writer.Write(' ');

        if (writeEndPipe)
        {
            _writer.Write('|');
        }
    }

    private void ThrowIfTooManyColumns(int count)
    {
        if (count > _columnMax)
        {
            throw new("Too many columns");
        }
    }

    private void CalculateColumnWidths()
    {
        if (_headers == null || _rows.Count == 0)
            return;

        int columnCount = _headers.Length;
        _calculatedColumns = new int[columnCount];

        // Start with header widths as minimum
        for (int col = 0; col < columnCount; col++)
        {
            _calculatedColumns[col] = _headers[col].Length;
        }

        // Calculate desired width for each column based on percentile
        for (int col = 0; col < columnCount; col++)
        {
            List<int> columnWidths = new();
            
            foreach (var row in _rows)
            {
                if (col < row.Length && row[col] != null)
                {
                    columnWidths.Add(row[col].Length);
                }
            }

            if (columnWidths.Count > 0)
            {
                columnWidths.Sort();
                int percentileIndex = (int)(columnWidths.Count * _percentileThreshold);
                if (percentileIndex >= columnWidths.Count)
                    percentileIndex = columnWidths.Count - 1;
                
                int percentileWidth = columnWidths[percentileIndex];
                _calculatedColumns[col] = Math.Max(_calculatedColumns[col], percentileWidth);
            }
        }

        // Adjust widths to fit within max table width
        int totalWidth = CalculateTotalTableWidth(_calculatedColumns);
        if (totalWidth > _maxWidth)
        {
            AdjustColumnsToFitWidth(_calculatedColumns);
        }
    }

    private int CalculateTotalTableWidth(int[] columnWidths)
    {
        int total = 0;
        for (int i = 0; i < columnWidths.Length; i++)
        {
            // Add column width
            total += columnWidths[i];
            
            // Add separator width: "| " for each column except potentially the last
            if (i > 0 || UseOuterPipes)
                total += 2; // "| "
            
            // Add space after column (except for last column without outer pipes)
            bool isLastColumn = (i == columnWidths.Length - 1);
            bool addTrailingSpace = !isLastColumn || UseOuterPipes;
            if (addTrailingSpace)
                total += 1; // " "
            
            // Add closing pipe for last column if using outer pipes
            if (isLastColumn && UseOuterPipes)
                total += 1; // "|"
        }
        return total;
    }

    private void AdjustColumnsToFitWidth(int[] columnWidths)
    {
        int currentWidth = CalculateTotalTableWidth(columnWidths);
        int excessWidth = currentWidth - _maxWidth;
        
        if (excessWidth <= 0)
            return;

        // Reduce column widths proportionally, but keep minimum as header width
        int[] minWidths = new int[columnWidths.Length];
        for (int i = 0; i < columnWidths.Length; i++)
        {
            minWidths[i] = _headers![i].Length;
        }

        // Calculate total reducible width
        int totalReducible = 0;
        for (int i = 0; i < columnWidths.Length; i++)
        {
            totalReducible += Math.Max(0, columnWidths[i] - minWidths[i]);
        }

        if (totalReducible == 0)
            return; // Cannot reduce further

        // Reduce proportionally
        int remainingReduction = excessWidth;
        for (int i = 0; i < columnWidths.Length && remainingReduction > 0; i++)
        {
            int reducible = Math.Max(0, columnWidths[i] - minWidths[i]);
            if (reducible > 0)
            {
                int reduction = Math.Min(remainingReduction, (reducible * excessWidth) / totalReducible);
                columnWidths[i] -= reduction;
                remainingReduction -= reduction;
            }
        }
    }

    private void RenderTable()
    {
        if (_headers == null || _calculatedColumns == null)
            return;

        // Render header
        for (int i = 0; i < _headers.Length; i++)
        {
            RenderColumn(_headers[i], i);
        }
        RenderEndRow();

        // Render header separator
        for (int i = 0; i < _headers.Length; i++)
        {
            RenderColumn("", i, '-', true);
        }
        RenderEndRow();

        // Render data rows
        foreach (var row in _rows)
        {
            for (int i = 0; i < Math.Min(row.Length, _headers.Length); i++)
            {
                RenderColumn(row[i] ?? "", i);
            }
            RenderEndRow();
        }
    }

    private void RenderColumn(string text, int columnIndex, char repeatCharacter = ' ', bool headerPolicy = false)
    {
        if (columnIndex > 0 || UseOuterPipes)
        {
            _writer.Write("| ");
        }

        _writer.Write(text);

        bool lastColumn = columnIndex == _calculatedColumns!.Length - 1;
        bool writeEndPipe = lastColumn && UseOuterPipes;
        bool endWithText = lastColumn && !UseOuterPipes && !headerPolicy;

        if (endWithText)
        {
            return;
        }

        // Calculate padding needed
        int targetWidth = _calculatedColumns[columnIndex];
        int paddingNeeded = targetWidth - text.Length;

        if (paddingNeeded > 0)
        {
            _writer.WriteRepeatCharacter(repeatCharacter, paddingNeeded);
        }

        // Add trailing space (except for last column without outer pipes in header separator)
        bool addTrailingSpace = !lastColumn || UseOuterPipes;
        if (addTrailingSpace)
        {
            _writer.Write(' ');
        }

        if (writeEndPipe)
        {
            _writer.Write('|');
        }
    }

    private void RenderEndRow()
    {
        _writer.WriteLine();
    }

    public void Dispose()
    {
        if (!_disposed && _isDynamic && _headerWritten)
        {
            CalculateColumnWidths();
            RenderTable();
            _disposed = true;
        }
    }

}
