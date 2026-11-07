using System.Diagnostics;

namespace HorstMFG.EnvTool;

/// <summary>
/// A progress line for long file copies (PDFs into or out of a backup): percent, MB, files, speed
/// and time left. Redrawn in place on a console; when output goes to a file, a line every 30 s.
/// </summary>
internal class CopyProgress
{
    private const int BufferSize = 1024 * 1024;

    private readonly string _label;
    private readonly int _totalFiles;
    private readonly long _totalBytes;
    private readonly bool _inPlace = !Console.IsOutputRedirected;
    private readonly TimeSpan _interval;
    private readonly Stopwatch _clock = Stopwatch.StartNew();
    private TimeSpan _lastDraw;
    private int _lastLength;
    private int _files;
    private long _bytes;

    public CopyProgress(string label, int totalFiles, long totalBytes)
    {
        _label = label;
        _totalFiles = totalFiles;
        _totalBytes = totalBytes;
        _interval = _inPlace ? TimeSpan.FromMilliseconds(500) : TimeSpan.FromSeconds(30);
        Draw(force: true);
    }

    public void Copy(Stream input, Stream output)
    {
        var buffer = new byte[BufferSize];
        int read;
        while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
        {
            output.Write(buffer, 0, read);
            _bytes += read;
            Draw(force: false);
        }
    }

    public void FileDone()
    {
        _files++;
        Draw(force: false);
    }

    public void Finish()
    {
        Draw(force: true, done: true);
        if (_inPlace) Console.WriteLine();
    }

    private void Draw(bool force, bool done = false)
    {
        var elapsed = _clock.Elapsed;
        if (!force && elapsed - _lastDraw < _interval) return;
        _lastDraw = elapsed;

        var percent = _totalBytes == 0 ? 100 : _bytes * 100.0 / _totalBytes;
        var rate = elapsed.TotalSeconds >= 1 ? _bytes / elapsed.TotalSeconds : 0;
        var tail = done ? $"done in {Format(elapsed)}"
                 : rate > 0 ? $"{Mb(rate):N0} MB/s, ~{Format(TimeSpan.FromSeconds((_totalBytes - _bytes) / rate))} left"
                 : "starting...";
        var line = $"  {_label}: {percent,5:N1}%  {Mb(_bytes):N0} / {Mb(_totalBytes):N0} MB  " +
                   $"{_files:N0} / {_totalFiles:N0} files  {tail}";

        if (_inPlace)
        {
            Console.Write("\r" + line.PadRight(_lastLength));
            _lastLength = line.Length;
        }
        else
            Console.WriteLine(line);
    }

    private static double Mb(double bytes) => bytes / (1024 * 1024);

    private static string Format(TimeSpan t) =>
        t.TotalHours >= 1 ? $"{(int)t.TotalHours}h {t.Minutes:D2}m" : t.TotalMinutes >= 1 ? $"{t.Minutes}m {t.Seconds:D2}s" : $"{t.Seconds}s";
}
