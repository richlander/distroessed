using System.Text;

namespace MarkdownHelpers;

public class Table
{
    private readonly StringBuilder _output = new();
    private readonly List<string[]> _rows = new();
    private string[] _headers = Array.Empty<string>();
    private string[] _currentRow = Array.Empty<string>();
    private int _currentColumn = 0;
    private int _columnCount = 0;
    private List<int>[] _columnLengths = Array.Empty<List<int>>();
    private bool _headerWritten = false;
    
    public bool UseOuterPipes { get; set; } = true;
    
    private double _percentileThreshold = 0.5;
    private double _toleranceMultiplier = 1.2;
    
    public double PercentileThreshold 
    { 
        get => _percentileThreshold;
        set
        {
            if (value < 0.0 || value > 1.0)
                throw new ArgumentOutOfRangeException(nameof(value), "PercentileThreshold must be between 0.0 and 1.0");
            _percentileThreshold = value;
        }
    }
    
    public double ToleranceMultiplier 
    { 
        get => _toleranceMultiplier;
        set
        {
            if (value <= 0.0)
                throw new ArgumentOutOfRangeException(nameof(value), "ToleranceMultiplier must be greater than 0.0");
            _toleranceMultiplier = value;
        }
    }
    
    public Table()
    {
    }

    public Table NewRow()
    {
        if (!_headerWritten)
        {
            throw new InvalidOperationException("Header must be written before starting a new row. Call WriteHeader first.");
        }
        
        if (_currentColumn != _columnCount && _currentColumn > 0)
        {
            throw new InvalidOperationException($"Current row is incomplete. Expected {_columnCount} columns, but only {_currentColumn} were written.");
        }
        
        // Complete current row if we have data
        if (_currentColumn > 0)
        {
            _rows.Add(_currentRow);
        }
        
        // Start new row
        _currentRow = new string[_columnCount];
        _currentColumn = 0;
        return this;
    }

    public void WriteHeader(ReadOnlySpan<string> labels)
    {
        if (_headerWritten)
        {
            throw new InvalidOperationException("Header has already been written. WriteHeader can only be called once.");
        }
        
        if (labels.Length == 0)
        {
            throw new ArgumentException("Header must have at least one column.", nameof(labels));
        }
        
        _columnCount = labels.Length;
        _headers = new string[_columnCount];
        _columnLengths = new List<int>[_columnCount];
        
        for (int i = 0; i < labels.Length; i++)
        {
            _headers[i] = labels[i] ?? string.Empty;
            _columnLengths[i] = new List<int>();
        }
        _currentRow = new string[_columnCount];
        _headerWritten = true;
    }

    public Table WriteColumn(string text)
    {
        if (!_headerWritten)
        {
            throw new InvalidOperationException("Header must be written before writing columns. Call WriteHeader first.");
        }
        if (_currentColumn >= _columnCount)
        {
            throw new ArgumentOutOfRangeException(nameof(_currentColumn), $"Too many columns. Expected {_columnCount}, but trying to write column {_currentColumn + 1}.");
        }
        string cellText = text ?? string.Empty;
        _currentRow[_currentColumn] = cellText;
        _columnLengths[_currentColumn].Add(cellText.Length);
        _currentColumn++;
        return this;
    }
    
    public override string ToString()
    {
        if (_columnCount == 0)
            return string.Empty;
            
        // Complete any incomplete row
        if (_currentColumn > 0)
        {
            // Fill missing columns with empty strings
            for (int i = _currentColumn; i < _columnCount; i++)
            {
                _currentRow[i] = string.Empty;
            }
            _rows.Add(_currentRow);
            _currentColumn = 0;
        }
        
        var columnWidths = CalculateColumnWidths();
        
        _output.Clear();
        
        // Write header
        WriteTableRow(_headers, columnWidths);
        WriteHeaderSeparator(columnWidths);
        
        // Write data rows
        foreach (var row in _rows)
        {
            WriteTableRow(row, columnWidths);
        }
        
        return _output.ToString();
    }
    
    private void Render()
    {
        // Legacy method - kept for potential internal use
        ToString();
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
            
            // 2. Get content lengths (already collected during WriteColumn)
            var contentLengths = _columnLengths[col];
            
            if (contentLengths.Count == 0)
            {
                alignmentWidths[col] = headerLength;
                continue;
            }
            
            // Sort the already-collected lengths
            var sortedLengths = new List<int>(contentLengths);
            sortedLengths.Sort();
            
            // 2. Length of the configured percentile row content
            int percentileIndex = Math.Min((int)Math.Ceiling(sortedLengths.Count * PercentileThreshold) - 1, sortedLengths.Count - 1);
            percentileIndex = Math.Max(0, percentileIndex); // Ensure non-negative
            int percentileLength = sortedLengths[percentileIndex];
            
            // 3. Length of the longest row that is only within tolerance of the percentile
            double tolerance = percentileLength * ToleranceMultiplier;
            int maxWithinToleranceLength = sortedLengths.Where(length => length <= tolerance).DefaultIfEmpty(percentileLength).Last();
            
            // Take the max of the three numbers
            alignmentWidths[col] = Math.Max(headerLength, Math.Max(percentileLength, maxWithinToleranceLength));
        }
        
        return alignmentWidths;
    }
    
    private void WriteTableRow(string[] row, int[] columnWidths)
    {
        int actualWidth = 0;  // How much space we've actually used
        int targetWidth = 0;  // How much space we're supposed to use
        
        for (int col = 0; col < _columnCount; col++)
        {
            int preSpace = 0;
            if (col > 0 || UseOuterPipes)
            {
                _output.Append("| ");
                preSpace = 2;
            }
            
            string text = row[col];
            _output.Append(text);
            
            bool isLastColumn = col == _columnCount - 1;
            bool writeEndPipe = isLastColumn && UseOuterPipes;
            bool endWithText = isLastColumn && !UseOuterPipes;
            
            if (endWithText)
            {
                continue; // No padding needed for last column without outer pipes
            }
            
            int postSpace = writeEndPipe ? 2 : 1; // " |" vs " "
            
            // Track actual vs target widths for smart padding
            actualWidth += preSpace + text.Length + postSpace;
            targetWidth += preSpace + columnWidths[col] + postSpace;
            
            // Only pad if we haven't exceeded the expected width
            int remaining = targetWidth - actualWidth;
            if (remaining > 0)
            {
                _output.Append(' ', remaining);
                actualWidth += remaining;
            }
            
            _output.Append(writeEndPipe ? " |" : " ");
        }
        _output.AppendLine();
    }
    
    private void WriteHeaderSeparator(int[] columnWidths)
    {
        int actualWidth = 0;  // How much space we've actually used
        int targetWidth = 0;  // How much space we're supposed to use
        
        for (int col = 0; col < _columnCount; col++)
        {
            int preSpace = 0;
            if (col > 0 || UseOuterPipes)
            {
                _output.Append("| ");
                preSpace = 2;
            }
            
            // Start with the calculated column width (which includes our three-number algorithm)
            _output.Append('-', columnWidths[col]);
            
            bool isLastColumn = col == _columnCount - 1;
            bool writeEndPipe = isLastColumn && UseOuterPipes;
            bool endWithText = isLastColumn && !UseOuterPipes;
            
            if (endWithText)
            {
                continue; // No padding needed for last column without outer pipes
            }
            
            int postSpace = writeEndPipe ? 2 : 1; // " |" vs " "
            
            // Track lengths using the calculated column width
            actualWidth += preSpace + columnWidths[col] + postSpace;
            targetWidth += preSpace + columnWidths[col] + postSpace;
            
            // For header separator, actualWidth and targetWidth should always match
            // since we're using the calculated width directly
            
            _output.Append(writeEndPipe ? " |" : " ");
        }
        _output.AppendLine();
    }
}