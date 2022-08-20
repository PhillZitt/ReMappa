using System;
using System.Collections.Generic;
using System.Text;

namespace ReMappa.Config
{
  public class CharacterConfig
  {
    public string name;

    public string logPath;

    public CharacterConfig(){ }

    public CharacterConfig(string name, string logPath){
      this.name = name;
      this.logPath = logPath;
    }
  }
}
