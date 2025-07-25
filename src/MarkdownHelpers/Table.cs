namespace MarkdownHelpers;

public class Table
{
    private readonly IWriter _writer;
    private readonly List<string[]> _rows = new();
    private string[] _headers = Array.Empty<string>();
    private string[] _currentRow = Array.Empty<string>();
    private int _currentColumn = 0;
    private int _columnCount = 0;
    
    public bool UseOuterPipes { get; set; } = true;
    public int MaxTableWidth { get; set; } = (int)(80 * 0.75); // Default to 75% of 80 characters
    public double PercentileThreshold { get; set; } = 0.5; // Default to 50th percentile (median)
    public double ToleranceMultiplier { get; set; } = 1.2; // Default to 20% tolerance
    
    public Table(IWriter writer)
    {
        _writer = writer;
    }
    
    public Table(IWriter writer, int maxTableWidth)
    {
        _writer = writer;
        MaxTableWidth = maxTableWidth;
    }

    public void EndRow()
    {
        if (_columnCount > 0 && _currentRow.Length > 0)
        {
            // Ensure all cells in the row are filled
            for (int i = _currentColumn; i < _columnCount; i++)
            {
                _currentRow[i] = string.Empty;
            }
            _rows.Add(_currentRow);
            _currentRow = new string[_columnCount];
        }
        _currentColumn = 0;
    }

    public void WriteHeader(ReadOnlySpan<string> labels)
    {
        _columnCount = labels.Length;
        _headers = new string[_columnCount];
        for (int i = 0; i < labels.Length; i++)
        {
            _headers[i] = labels[i];
        }
        _currentRow = new string[_columnCount];
    }

    public void WriteColumn(string text)
    {
        ThrowIfTooManyColumns(_currentColumn);
        _currentRow[_currentColumn] = text ?? string.Empty;
        _currentColumn++;
    }

    private void ThrowIfTooManyColumns(int columnIndex)
    {
        if (columnIndex >= _columnCount)
        {
            throw new ArgumentException($"Too many columns. Expected {_columnCount}, but trying to write column {columnIndex + 1}.");
        }
    }
    
    public void Render()
    {
        if (_columnCount == 0)
            return;
            
        var columnWidths = CalculateColumnWidths();
        
        // Write header
        WriteTableRow(_headers, columnWidths);
        WriteHeaderSeparator(columnWidths);
        
        // Write data rows
        foreach (var row in _rows)
        {
            WriteTableRow(row, columnWidths);
        }
    }
    
    private int[] CalculateColumnWidths()
    {
        if (_columnCount == 0) return Array.Empty<int>();
        
        // Algorithm: We need three numbers for each column:
        // - Header length (sets the min)
        // - Length of the configured percentile row content
        // - Length of the longest row that is only within tolerance of the percentile
        // Take the max of those and that's our default width for the outer pipe.
        // All rows should align with that unless they go longer.
        // If the row content is already past the default width, it should add one pad space and stop immediately.
        
        var alignmentWidths = new int[_columnCount];
        
        for (int col = 0; col < _columnCount; col++)
        {
            // 1. Header length (sets the min)
            int headerLength = _headers[col].Length;
            
            // 2. Get all content lengths (excluding header)
            var contentLengths = new List<int>();
            foreach (var row in _rows)
            {
                contentLengths.Add(row[col].Length);
            }
            
            if (contentLengths.Count == 0)
            {
                alignmentWidths[col] = headerLength;
                continue;
            }
            
            contentLengths.Sort();
            
            // 2. Length of the configured percentile row content
            int percentileIndex = (int)(contentLengths.Count * PercentileThreshold);
            percentileIndex = Math.Min(percentileIndex, contentLengths.Count - 1);
            int percentileLength = contentLengths[percentileIndex];
            
            // 3. Length of the longest row that is only within tolerance of the percentile
            double tolerance = percentileLength * ToleranceMultiplier;
            int maxWithinToleranceLength = percentileLength;
            
            foreach (int length in contentLengths)
            {
                if (length <= tolerance)
                {
                    maxWithinToleranceLength = length;
                }
                else
                {
                    break; // contentLengths is sorted, so we can stop here
                }
            }
            
            // Take the max of the three numbers
            alignmentWidths[col] = Math.Max(headerLength, Math.Max(percentileLength, maxWithinToleranceLength));
        }
        
        return alignmentWidths;
    }
    
    private void WriteTableRow(string[] row, int[] columnWidths)
    {
        int realLength = 0;  // How much space we've actually used
        int specLength = 0;  // How much space we're supposed to use
        
        for (int col = 0; col < _columnCount; col++)
        {
            int preSpace = 0;
            if (col > 0 || UseOuterPipes)
            {
                _writer.Write("| ");
                preSpace = 2;
            }
            
            string text = row[col];
            _writer.Write(text);
            
            bool isLastColumn = col == _columnCount - 1;
            bool writeEndPipe = isLastColumn && UseOuterPipes;
            bool endWithText = isLastColumn && !UseOuterPipes;
            
            if (endWithText)
            {
                continue; // No padding needed for last column without outer pipes
            }
            
            int postSpace = writeEndPipe ? 2 : 1; // " |" vs " "
            
            // Track lengths like the original implementation
            realLength += preSpace + text.Length + postSpace;
            specLength += preSpace + columnWidths[col] + postSpace;
            
            // Only pad if we haven't exceeded the expected width (like original)
            int remaining = specLength - realLength;
            if (remaining > 0)
            {
                _writer.WriteRepeatCharacter(' ', remaining);
                realLength += remaining;
            }
            
            _writer.Write(writeEndPipe ? " |" : " ");
        }
        _writer.WriteLine();
    }
    
    private void WriteHeaderSeparator(int[] columnWidths)
    {
        int realLength = 0;  // How much space we've actually used
        int specLength = 0;  // How much space we're supposed to use
        
        for (int col = 0; col < _columnCount; col++)
        {
            int preSpace = 0;
            if (col > 0 || UseOuterPipes)
            {
                _writer.Write("| ");
                preSpace = 2;
            }
            
            // Start with the calculated column width (which includes our three-number algorithm)
            _writer.WriteRepeatCharacter('-', columnWidths[col]);
            
            bool isLastColumn = col == _columnCount - 1;
            bool writeEndPipe = isLastColumn && UseOuterPipes;
            bool endWithText = isLastColumn && !UseOuterPipes;
            
            if (endWithText)
            {
                continue; // No padding needed for last column without outer pipes
            }
            
            int postSpace = writeEndPipe ? 2 : 1; // " |" vs " "
            
            // Track lengths using the calculated column width
            realLength += preSpace + columnWidths[col] + postSpace;
            specLength += preSpace + columnWidths[col] + postSpace;
            
            // For header separator, realLength and specLength should always match
            // since we're using the calculated width directly
            
            _writer.Write(writeEndPipe ? " |" : " ");
        }
        _writer.WriteLine();
    }
}