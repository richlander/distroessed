using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Common;
using System.IO.Compression;
using System.Text;
using Microsoft.VisualBasic.FileIO;

namespace MarkdownHelpers;

public class Table(IWriter writer, int[] columns)
{
    private readonly IWriter _writer = writer;
    private int[] _columns = columns;
    private readonly int _columnMax = columns.Length - 1;
    private int _column = 0;
    private int _realLength = 0;
    private int _specLength = 0;
    
    public Table(StringBuilder builder, int[] columns) : this(new StringBuilderIWriter(builder), columns)
    {
    }

    public Table(StreamWriter stream, int[] columns) : this(new StreamWriterIWriter(stream), columns)
    {
    }


    public bool UseOuterPipes { get; set; } = false;

    public void EndRow()
    {
        _column = 0;
        _realLength = 0;
        _specLength = 0;
        _writer.WriteLine();
    }

    public void WriteHeader(ReadOnlySpan<string> labels)
    {
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
        _specLength += _columns[_column];
        _column++;

        int remaining = _specLength - _realLength;
        if (remaining > 0)
        {
            WriteRepeatCharacter(repeatCharacter, remaining);
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

    private void WriteRepeatCharacter(char repeatCharacter, int repeats)
    {
        for (int i = 0; i < repeats; i++)
        {
            _writer.Write(repeatCharacter);
        }
    }

    private void ThrowIfTooManyColumns(int count)
    {
        if (count > _columnMax)
        {
            throw new("Too many columns");
        }
    }

}
