using System;
using System.Collections.Generic;
using System.Text;

namespace ReMappa.FileListener
{
  public class LogFileParser
  {

    private string parseText;

    public LogFileParser(string parseText){
      this.parseText = parseText;
    }

    public bool ParseText(string line){
      return line.Contains(parseText);
    }
  }
}
