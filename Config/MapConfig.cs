using System;
using System.Collections.Generic;
using System.Text;

namespace ReMappa.Config
{
  public class MapConfig
  {

    public string name;

    public string trigger;

    public string imagePath;

    public string infoUrl;

    public int imageStartX;

    public int imageStartY;

    public int imageEndX;

    public int imageEndY;

    public int zoneStartX;

    public int zoneStartY;

    public int zoneEndX;

    public int zoneEndY;

    public int GetImageXDiff()
    {
      return imageEndX - imageStartX;
    }

    public int GetImageYDiff()
    {
      return imageEndY - imageStartY;
    }

    public int GetZoneXDiff()
    {
      return zoneStartX - zoneEndX;
    }

    public int GetZoneYDiff()
    {
      return zoneStartY - zoneEndY;
    }

    public double GetZoneToImageScaleX(){
      return Math.Abs((double)GetImageXDiff() / (double)GetZoneXDiff());
    }

    public double GetZoneToImageScaleY()
    {
      return Math.Abs((double)GetImageYDiff() / (double)GetZoneYDiff());
    }

    public MapConfig(){

    }
  }
}
