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
        
        // Calculate widths for reasonable alignment without forcing extreme outliers
        var alignmentWidths = new int[_columnCount];
        
        for (int col = 0; col < _columnCount; col++)
        {
            var lengths = new List<int>();
            
            // Include header
            lengths.Add(_headers[col].Length);
            
            // Include all content lengths
            foreach (var row in _rows)
            {
                lengths.Add(row[col].Length);
            }
            
            lengths.Sort();
            
            // Use 75th percentile as the "reasonable" width for alignment
            // This prevents extreme outliers from forcing wide columns on everyone
            int percentileIndex = (int)(lengths.Count * 0.75);
            percentileIndex = Math.Min(percentileIndex, lengths.Count - 1);
            
            alignmentWidths[col] = lengths[percentileIndex];
        }
        
        return alignmentWidths;
    }
    
    private void WriteTableRow(string[] row, int[] columnWidths)
    {
        for (int col = 0; col < _columnCount; col++)
        {
            if (col > 0 || UseOuterPipes)
            {
                _writer.Write("| ");
            }
            
            string text = row[col];
            _writer.Write(text);
            
            bool isLastColumn = col == _columnCount - 1;
            
            // For non-last columns, pad to alignment width if content is shorter
            // If content is longer, just add the separator space and continue
            if (!isLastColumn)
            {
                int padding = columnWidths[col] - text.Length;
                if (padding > 0)
                {
                    _writer.WriteRepeatCharacter(' ', padding);
                }
                _writer.Write(" ");
            }
            else if (UseOuterPipes)
            {
                // For last column with outer pipes, pad if shorter than alignment width
                int padding = columnWidths[col] - text.Length;
                if (padding > 0)
                {
                    _writer.WriteRepeatCharacter(' ', padding);
                }
                _writer.Write(" |");
            }
        }
        _writer.WriteLine();
    }
    
    private void WriteHeaderSeparator(int[] columnWidths)
    {
        for (int col = 0; col < _columnCount; col++)
        {
            if (col > 0 || UseOuterPipes)
            {
                _writer.Write("| ");
            }
            
            _writer.WriteRepeatCharacter('-', columnWidths[col]);
            
            bool isLastColumn = col == _columnCount - 1;
            if (!isLastColumn || UseOuterPipes)
            {
                _writer.Write(" ");
            }
            
            if (isLastColumn && UseOuterPipes)
            {
                _writer.Write("|");
            }
        }
        _writer.WriteLine();
    }
}
