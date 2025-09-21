using System.IO.Pipelines;
using System.Text;

namespace Drift.Cli.Output;

internal class WriterReaderBridge {
  private readonly Pipe _pipe = new();
  private readonly StreamWriter _writer;
  private readonly StreamReader _reader;

  public WriterReaderBridge() {
    _writer = new StreamWriter( _pipe.Writer.AsStream(), Encoding.UTF8, leaveOpen: true ) { AutoFlush = true };
    _reader = new StreamReader( _pipe.Reader.AsStream(), Encoding.UTF8 );
  }

  public TextWriter Writer => _writer;
  public TextReader Reader => _reader;
}