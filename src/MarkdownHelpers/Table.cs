using System.Text;

namespace MarkdownHelpers;

public class Table
{
    private readonly StringBuilder _output = new();
    private readonly List<string[]> _rows = new();
    private string[] _headers = Array.Empty<string>();
    private int _columnCount = 0;
    private List<int>[] _columnLengths = Array.Empty<List<int>>();
    private bool _headerWritten = false;
    
    public bool UseOuterPipes { get; set; } = true;
    
    private double _percentileThreshold = 0.5;
    private double _toleranceMultiplier = 1.2;
    private int _minimumColumnWidth = 1;
    
    // Constants for table formatting
    private const int PipeSpacing = 2; // Space around pipes: "| "
    private const int ColumnSpacing = 1; // Space between columns without pipes
    
    /// <summary>
    /// Gets or sets the percentile threshold used for dynamic column width calculation.
    /// Default is 0.5 (50th percentile). Values must be between 0.0 and 1.0.
    /// Lower values result in narrower columns, higher values in wider columns.
    /// </summary>
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
    
    /// <summary>
    /// Gets or sets the tolerance multiplier for column width calculation.
    /// Default is 1.2 (20% tolerance). Values must be greater than 0.0.
    /// Higher values allow wider columns to accommodate outliers.
    /// </summary>
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
    
    /// <summary>
    /// Gets or sets the minimum column width.
    /// Default is 1. Values must be greater than 0.
    /// Ensures columns are never narrower than this value.
    /// </summary>
    public int MinimumColumnWidth
    {
        get => _minimumColumnWidth;
        set
        {
            if (value <= 0)
                throw new ArgumentOutOfRangeException(nameof(value), "MinimumColumnWidth must be greater than 0");
            _minimumColumnWidth = value;
        }
    }
    
    
    // New unified API with params support
    public Table AddHeader(params string[] columns) 
    {
        CompleteHeader(columns);
        return this;
    }
    
    public Table AddRow(params string[] columns) 
    {
        CompleteRow(columns);
        return this;
    }
    
    // Internal methods
    private void InitializeColumns(int columnCount)
    {
        if (_headerWritten)
            throw new InvalidOperationException("Cannot change column count after header is written.");
            
        _columnCount = columnCount;
        _columnLengths = new List<int>[_columnCount];
        for (int i = 0; i < _columnCount; i++)
        {
            _columnLengths[i] = new List<int>();
        }
    }
    
    private void CompleteHeader(string[] headerData)
    {
        if (_headerWritten)
            throw new InvalidOperationException("Header has already been written.");
            
        if (headerData.Length == 0)
            throw new ArgumentException("Header must have at least one column.");
            
        InitializeColumns(headerData.Length);
        _headers = new string[_columnCount];
        Array.Copy(headerData, _headers, _columnCount);
        _headerWritten = true;
    }
    
    private void CompleteRow(string[] rowData)
    {
        if (!_headerWritten)
            throw new InvalidOperationException("Header must be written before adding rows.");
            
        if (rowData.Length != _columnCount)
            throw new ArgumentException($"Row must have exactly {_columnCount} columns, but {rowData.Length} were provided.");
            
        // Record column lengths for width calculation
        for (int i = 0; i < _columnCount; i++)
        {
            _columnLengths[i].Add(rowData[i].Length);
        }
        
        var row = new string[_columnCount];
        Array.Copy(rowData, row, _columnCount);
        _rows.Add(row);
    }

    
    public override string ToString()
    {
        if (_columnCount == 0)
            return string.Empty;
            
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
    
    
    /// <summary>
    /// Calculates optimal column widths based on content analysis.
    /// Uses a three-factor algorithm considering:
    /// 1. Header length (minimum constraint)
    /// 2. Percentile-based content length 
    /// 3. Maximum length within tolerance of percentile
    /// The final width is the maximum of these three values.
    /// </summary>
    private int[] CalculateColumnWidths()
    {
        if (_columnCount == 0) return Array.Empty<int>();
        
        var alignmentWidths = new int[_columnCount];
        
        for (int col = 0; col < _columnCount; col++)
        {
            // 1. Header length (sets the minimum)
            int headerLength = _headers[col].Length;
            
            // 2. Get content lengths collected during row addition
            var contentLengths = _columnLengths[col];
            
            if (contentLengths.Count == 0)
            {
                alignmentWidths[col] = Math.Max(headerLength, MinimumColumnWidth);
                continue;
            }
            
            // Sort lengths for percentile calculation (creates a copy to avoid modifying original)
            var sortedLengths = contentLengths.ToArray();
            Array.Sort(sortedLengths);
            
            // 2. Calculate percentile-based length
            int percentileIndex = Math.Min((int)Math.Ceiling(sortedLengths.Length * PercentileThreshold) - 1, sortedLengths.Length - 1);
            percentileIndex = Math.Max(0, percentileIndex); // Ensure non-negative
            int percentileLength = sortedLengths[percentileIndex];
            
            // 3. Find maximum length within tolerance of percentile
            double tolerance = percentileLength * ToleranceMultiplier;
            int maxWithinToleranceLength = sortedLengths.Where(length => length <= tolerance).DefaultIfEmpty(percentileLength).Last();
            
            // Take the maximum of all three factors, ensuring minimum width is respected
            alignmentWidths[col] = Math.Max(MinimumColumnWidth, Math.Max(headerLength, Math.Max(percentileLength, maxWithinToleranceLength)));
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
                preSpace = PipeSpacing;
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
            
            int postSpace = writeEndPipe ? PipeSpacing : ColumnSpacing; // " |" vs " "
            
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
                preSpace = PipeSpacing;
            }
            
            // Use the calculated column width from the dynamic algorithm
            _output.Append('-', columnWidths[col]);
            
            bool isLastColumn = col == _columnCount - 1;
            bool writeEndPipe = isLastColumn && UseOuterPipes;
            bool endWithText = isLastColumn && !UseOuterPipes;
            
            if (endWithText)
            {
                continue; // No padding needed for last column without outer pipes
            }
            
            int postSpace = writeEndPipe ? PipeSpacing : ColumnSpacing; // " |" vs " "
            
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