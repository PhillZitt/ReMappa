using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ReMappa.FileListener
{
  public class LogFileListener
  {
    private string logFilePath;

    private FileSystemWatcher watcher;

    private long LastPosition;

    private LogFileParser parser;

    public LogFileListener(string path, LogFileParser parser){
      logFilePath = path;
      this.parser = parser;

      CreateFileWatcher();
    }

    private void CreateFileWatcher(){
      watcher = new FileSystemWatcher();

      watcher.Path = logFilePath;

      watcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.LastAccess;

      watcher.Changed += new FileSystemEventHandler(OnChanged);
      LastPosition = 0;
      watcher.EnableRaisingEvents = true;
    }

    private void OnChanged(object source, FileSystemEventArgs e){
      FileInfo fi = new FileInfo(e.FullPath);
      string line;

      using (FileStream fileStream = new FileStream(e.FullPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)){
        fileStream.Seek(LastPosition, SeekOrigin.Begin);
        StreamReader reader = new StreamReader(fileStream);
        while((line = reader.ReadLine())!= null){
          if(parser.ParseText(line)){
            Console.WriteLine("Match!");
          }
        }
      }

    }
  }
}
