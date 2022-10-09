using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using System.Linq;
using System.Xml.Linq;

namespace ReMappa.Config
{
  public class UserConfig
  {
    public List<CharacterConfig> characterLogs;

    public List<MapConfig> mapConfigs;

    public string searchURL;

    public string triggerPhrase;

    private static string CONFIG_FILE = Path.Combine(Directory.GetCurrentDirectory(), "ReMappa.xml");

    private static string OLD_CONFIG_FILE = Path.Combine(Directory.GetCurrentDirectory(), "config.xml");


    public UserConfig()
    {
      characterLogs = new List<CharacterConfig>();
      mapConfigs = new List<MapConfig>();
    }

    public static UserConfig LoadUserConfiguration()
    {
      UserConfig uc = new UserConfig();
      XmlDocument document = new XmlDocument();

      if (File.Exists(CONFIG_FILE))
      {
        document.Load(CONFIG_FILE);
        //This is our config, load directly
        using (XmlTextReader tr = new XmlTextReader(CONFIG_FILE))
        {
          XmlSerializer xml = new XmlSerializer(uc.GetType());
          uc = (UserConfig)xml.Deserialize(tr);
        }

      }
      else if (File.Exists(OLD_CONFIG_FILE))
      {
        document.Load(OLD_CONFIG_FILE);
        //This is an old Mappa config, so load with that language
        XmlNode characters = document.SelectSingleNode("/Mappa/Characters");
        foreach (XmlNode n in characters)
        {
          string charName = n.SelectSingleNode("./Charactername").InnerText;
          string logFile = n.SelectSingleNode("./Characterlogfile").InnerText;
          uc.characterLogs.Add(new CharacterConfig(charName, logFile));
        }

        XmlNode config = document.SelectSingleNode("/Mappa/Configuration");
        uc.searchURL = config.SelectSingleNode("./Searchurl").InnerText;
        uc.triggerPhrase = config.SelectSingleNode("./Triggerphrase").InnerText;

        XmlNode zones = document.SelectSingleNode("/Mappa/Zones");
        foreach (XmlNode z in zones)
        {
          MapConfig map = new MapConfig();
          map.name = z.SelectSingleNode("./ZoneName").InnerText;
          map.trigger = z.SelectSingleNode("./Zonetrigger").InnerText;
          map.imagePath = z.SelectSingleNode("./Zonepicpath").InnerText;
          map.infoUrl = z.SelectSingleNode("./Zoneinfourl").InnerText;

          map.imageStartX = Convert.ToInt32(z.SelectSingleNode("./Zonepicstarthoriz").InnerText);
          map.imageStartY = Convert.ToInt32(z.SelectSingleNode("./Zonepicstartvert").InnerText);
          map.imageEndX = Convert.ToInt32(z.SelectSingleNode("./Zonepicendhoriz").InnerText);
          map.imageEndY = Convert.ToInt32(z.SelectSingleNode("./Zonepicendvert").InnerText);

          map.zoneStartX = Convert.ToInt32(z.SelectSingleNode("./Zonemapstarthoriz").InnerText);
          map.zoneStartY = Convert.ToInt32(z.SelectSingleNode("./Zonemapstartvert").InnerText);
          map.zoneEndX = Convert.ToInt32(z.SelectSingleNode("./Zonemapendhoriz").InnerText);
          map.zoneEndY = Convert.ToInt32(z.SelectSingleNode("./Zonemapendvert").InnerText);

          uc.mapConfigs.Add(map);
        }

        //Then resave into our config layout
        uc.SaveUserConfiguration();

      } else {
        var baseConfig = new BaseConfig();
        var decoded = baseConfig.ReturnDecodedAndDeflatedString();
        var bytes = Encoding.UTF8.GetBytes(decoded);
        var stream = new MemoryStream(bytes);
        XmlSerializer xml = new XmlSerializer(uc.GetType());
        uc = (UserConfig)xml.Deserialize(stream);
        uc.SaveUserConfiguration();
      }

      uc.characterLogs = uc.characterLogs.OrderBy(x => x.name).ToList();
      uc.mapConfigs = uc.mapConfigs.OrderBy(x => x.name).ToList();

      return uc;
    }

    public void SaveUserConfiguration()
    {
      this.characterLogs.OrderBy(x => x.name);
      this.mapConfigs.OrderBy(x => x.name);
      using (XmlTextWriter tw = new XmlTextWriter(CONFIG_FILE, Encoding.UTF8))
      {
        tw.Formatting = Formatting.Indented;
        XmlSerializer xml = new XmlSerializer(this.GetType());
        xml.Serialize(tw, this);
      }
    }

    public MapConfig GetMapInfoForZoneName(string newZone)
    {
      return mapConfigs.Where(x => x.trigger.Equals(newZone, StringComparison.CurrentCultureIgnoreCase)).FirstOrDefault() ?? mapConfigs.Where(x => x.name.Equals(newZone, StringComparison.CurrentCultureIgnoreCase)).FirstOrDefault();
    }

    public void RemoveCharacter(string selectedItem)
    {
      var characterToRemove = characterLogs.Where(x => x.name.Equals(selectedItem)).FirstOrDefault();
      characterLogs.Remove(characterToRemove);
      SaveUserConfiguration();
    }

    public string GetLogFileForCharacterName(string charName)
    {
      if (string.IsNullOrEmpty(charName))
      {
        return "";
      }
      return characterLogs.Where(x => x.name.Equals(charName)).FirstOrDefault().logPath;
    }

    public void AddNewCharacter(string characterName, string newFile)
    {
      if (string.IsNullOrEmpty(characterName) || string.IsNullOrEmpty(newFile))
      {
        return;
      }

      if (characterLogs.Where(x => x.logPath.Equals(newFile)).Any())
      {
        return;
      }

      characterLogs.Add(new CharacterConfig(characterName, newFile));
      characterLogs = characterLogs.OrderBy(x => x.name).ToList();
      SaveUserConfiguration();
    }

  }
}
