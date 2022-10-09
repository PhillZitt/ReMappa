using Microsoft.Win32;
using ReMappa.Config;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;

namespace ReMappa
{
  /// <summary>
  /// Interaction logic for MainWindow.xaml
  /// 
  /// I really need to pull the non-window code into other classes...
  /// </summary>
  public partial class MainWindow : Window
  {

    public UserConfig config;

    public FileSystemWatcher watcher;

    private long LastPosition;

    private Regex regex;

    private Regex location;

    private Regex newLogCharacter;

    private MapConfig currentZone;

    private System.Windows.Shapes.Ellipse locationPoint;

    private System.Windows.Shapes.Ellipse startRefPoint;

    private System.Windows.Shapes.Ellipse endRefPoint;

    private readonly double LOCATION_POINT_DEFAULT_SIZE = 16;

    private Point ImageSize;

    private Point lastZoneLocation;

    public MainWindow()
    {
      InitializeComponent();

      config = UserConfig.LoadUserConfiguration();
      regex = new Regex("(" + config.triggerPhrase + @")([\w\s]*)\.");
      location = new Regex(@"Your Location is (-?\d*\.\d*), (-?\d*\.\d*), (-?\d*\.\d*)");
      newLogCharacter = new Regex(@"_(\w*)_(\w*)");
      LastPosition = 0;
      ImageSize = new Point(0, 0);

      locationPoint = new System.Windows.Shapes.Ellipse();
      locationPoint.Name = "LocationPoint";
      locationPoint.Fill = Brushes.Firebrick;
      locationPoint.StrokeThickness = 2;
      locationPoint.Stroke = Brushes.Black;
      UpdateLocationPointSize(locationPoint, LOCATION_POINT_DEFAULT_SIZE);

      startRefPoint = new System.Windows.Shapes.Ellipse();
      startRefPoint.Name = "StartReference";
      startRefPoint.Fill = Brushes.Blue;
      startRefPoint.StrokeThickness = 2;
      startRefPoint.Stroke = Brushes.Black;
      startRefPoint.Visibility = Visibility.Hidden;
      UpdateLocationPointSize(startRefPoint, LOCATION_POINT_DEFAULT_SIZE);

      endRefPoint = new System.Windows.Shapes.Ellipse();
      endRefPoint.Name = "EndReference";
      endRefPoint.Fill = Brushes.Green;
      endRefPoint.StrokeThickness = 2;
      endRefPoint.Stroke = Brushes.Black;
      endRefPoint.Visibility = Visibility.Hidden;
      UpdateLocationPointSize(endRefPoint, LOCATION_POINT_DEFAULT_SIZE);

      CharacterListChanged();
    }

    private void CharacterSelect_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
      string logpath = config.GetLogFileForCharacterName((string)CharacterSelect.SelectedItem);
      LastPosition = 0;
      if (!string.IsNullOrEmpty(logpath))
      {
        CreateFileWatcher(logpath);
      }
    }

    private void CreateFileWatcher(string logFilePath)
    {
      watcher = new FileSystemWatcher();

      watcher.Path = Path.GetDirectoryName(logFilePath);
      watcher.Filter = Path.GetFileName(logFilePath);

      watcher.NotifyFilter = NotifyFilters.LastWrite;

      watcher.Changed += new FileSystemEventHandler(FileWatcherOnChanged);
      LastPosition = 0;
      watcher.EnableRaisingEvents = true;
    }

    private void Window_SizeChanged(object sender, SizeChangedEventArgs e){
      //When the window changes size, we need to recompute all of the points
      if(null == currentZone){
        return;
      }
      Application.Current.Dispatcher.BeginInvoke(new Action(() => UpdateAllLocations(MapXFromZoneToScreen(lastZoneLocation.X), MapYFromZoneToScreen(lastZoneLocation.Y), locationPoint)));
    }

    private void FileWatcherOnChanged(object source, FileSystemEventArgs e)
    {
      FileInfo fi = new FileInfo(e.FullPath);
      string line;

      using (FileStream fileStream = new FileStream(e.FullPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
      {
        fileStream.Seek(LastPosition, SeekOrigin.Begin);
        StreamReader reader = new StreamReader(fileStream);
        while ((line = reader.ReadLine()) != null)
        {
          var match = regex.Match(line);
          if (match.Success)
          {
            ZoneChanged(match.Groups[2].Value.Trim());
          }
          match = location.Match(line);
          if (match.Success)
          {
            UpdateLocation(match.Groups[2].Value, match.Groups[1].Value);
          }
        }
        LastPosition = fileStream.Position;
      }
    }

    private async void ZoneChanged(string newZone)
    {
      var curZone = config.GetMapInfoForZoneName(newZone);
      await Application.Current.Dispatcher.BeginInvoke(new Action(() => UpdateMapImage(curZone)));
    }

    private void CharacterListChanged()
    {
      CharacterSelect.ItemsSource = config.characterLogs.Select(x => x.name);
    }

    private void UpdateMapImage(MapConfig curZone)
    {
      if (null == curZone)
      {
        return;
      }
      currentZone = curZone;

      //Reset reference point locations
      UpdateLocationPointFromImage(currentZone.imageStartX, currentZone.imageStartY, startRefPoint);
      UpdateLocationPointFromImage(currentZone.imageEndX, currentZone.imageEndY, endRefPoint);

      var bitmap = new BitmapImage();
      bitmap.BeginInit();
      bitmap.UriSource = new Uri(currentZone.imagePath, UriKind.Relative);
      bitmap.EndInit();
      bitmap.Freeze();
      var brush = new ImageBrush(bitmap);
      
      //This breaks the display points.
      //Need to find the fix to ensure canvas sticks to image.
      //brush.Stretch = Stretch.Uniform;
     
      MapDisplay.Background = brush;
      ImageSize = new Point(bitmap.PixelWidth, bitmap.PixelHeight);
      Canvas.SetLeft(locationPoint, -50);
      Canvas.SetTop(locationPoint, -50);
    }

    private void AddCharacterButton_Click(object sender, RoutedEventArgs e)
    {
      var fileDialog = new OpenFileDialog();

      var currentLog = config.GetLogFileForCharacterName((string)CharacterSelect.SelectedItem);
      if (!string.IsNullOrEmpty(currentLog))
      {
        fileDialog.InitialDirectory = Path.GetDirectoryName(currentLog);
      }

      var success = fileDialog.ShowDialog();

      if (success.GetValueOrDefault(false))
      {
        var newFile = fileDialog.FileName;
        var match = newLogCharacter.Match(Path.GetFileName(newFile));
        var characterName = match.Groups[1].Value + " (" + match.Groups[2].Value + ")";
        config.AddNewCharacter(characterName, newFile);
        Application.Current.Dispatcher.BeginInvoke(new Action(() => CharacterListChanged()));
        CharacterSelect.SelectedItem = characterName;
      }
    }

    private void RemoveCharacterButton_Click(object sender, RoutedEventArgs e)
    {
      config.RemoveCharacter((string)CharacterSelect.SelectedItem);
      Application.Current.Dispatcher.BeginInvoke(new Action(() => CharacterListChanged()));
    }

    private void OpenWikiButton_Click(object sender, RoutedEventArgs e)
    {
      if(currentZone == null){
        return;
      }

      var psi = new ProcessStartInfo
      {
        FileName = currentZone.infoUrl,
        UseShellExecute = true
      };
      Process.Start(psi);
    }

    private void OpenConfigButton_Click(object sender, RoutedEventArgs e){
      var psi = new ProcessStartInfo
      {
        FileName = Directory.GetCurrentDirectory(),
        UseShellExecute = true
      };
      Process.Start(psi);
    }

    private void ToggleRefPointsButton_Click(object sender, RoutedEventArgs e){
      if(startRefPoint.IsVisible){
        startRefPoint.Visibility = Visibility.Hidden;
      } else {
        startRefPoint.Visibility = Visibility.Visible;
      }
      if (endRefPoint.IsVisible)
      {
        endRefPoint.Visibility = Visibility.Hidden;
      }
      else
      {
        endRefPoint.Visibility = Visibility.Visible;
      }
    }

    private void UpdateLocationPointSize(System.Windows.Shapes.Ellipse point, double size)
    {
      point.Height = size;
      point.Width = size;
    }

    private void UpdateAllLocations(double x, double y, System.Windows.Shapes.Ellipse point){
      UpdateLocationPoint(x, y, point);
      UpdateLocationPointFromImage(currentZone.imageStartX, currentZone.imageStartY, startRefPoint);
      UpdateLocationPointFromImage(currentZone.imageEndX, currentZone.imageEndY, endRefPoint);
    }

    private void UpdateLocationPoint(double x, double y, System.Windows.Shapes.Ellipse point)
    {
      MapDisplay.Children.Remove(point);
      Canvas.SetLeft(point, x - (point.Width / 2));
      Canvas.SetTop(point, y - (point.Height / 2));
      Canvas.SetZIndex(point, 1);
      MapDisplay.Children.Add(point);
    }

    private void UpdateReferencePoint(double x, double y, System.Windows.Shapes.Ellipse point)
    {
      MapDisplay.Children.Remove(point);
      Canvas.SetLeft(point, x - (point.Width / 2));
      Canvas.SetBottom(point, y - (point.Height / 2));
      Canvas.SetZIndex(point, 1);
      MapDisplay.Children.Add(point);
    }

    private void UpdateLocationPointFromImage(double x, double y, System.Windows.Shapes.Ellipse point)
    {
      var displayX = MapXFromImageToScreen(x);
      var displayY = MapYFromImageToScreen(y);

      if(Double.IsInfinity(displayX) || Double.IsInfinity(displayY)){
        return;
      }

      UpdateReferencePoint(displayX, displayY, point);
    }

    private void UpdateLocation(string stringX, string stringY)
    {
      if (null == currentZone)
      {
        return;
      }
      double zoneLocationX, zoneLocationY;

      try
      {
        zoneLocationX = Double.Parse(stringX);
        zoneLocationY = Double.Parse(stringY);
      }
      catch
      {
        return;
      }

      lastZoneLocation.X = zoneLocationX;
      lastZoneLocation.Y = zoneLocationY;

      double displayLocationX = MapXFromZoneToScreen(zoneLocationX);
      double displayLocationY = MapYFromZoneToScreen(zoneLocationY);

      if(!Double.IsFinite(displayLocationX) || !Double.IsFinite(displayLocationY)){
        return;
      }

      Application.Current.Dispatcher.BeginInvoke(new Action(() => UpdateAllLocations(displayLocationX,displayLocationY,locationPoint)));
    }

    private double MapXFromZoneToScreen(double x){
      return MapXFromImageToScreen(MapXFromZoneToImage(x));
    }

    private double MapYFromZoneToScreen(double y)
    {
      return MapYFromImageToScreen(MapYFromZoneToImage(y));
    }

    private double MapXFromImageToScreen(double x){
      return MapValueFromRangeToRange(x, 0, ImageSize.X, 0, MapDisplay.ActualWidth);
    }

    private double MapYFromImageToScreen(double y)
    {
      return MapValueFromRangeToRange(y, 0, ImageSize.Y, 0, MapDisplay.ActualHeight);
    }

    private double MapXFromZoneToImage(double x){
      return MapValueFromRangeToRange(x, currentZone.zoneStartX, currentZone.zoneEndX, currentZone.imageStartX, currentZone.imageEndX);
    }

    private double MapYFromZoneToImage(double y)
    {
      return MapValueFromRangeToRange(y, currentZone.zoneStartY, currentZone.zoneEndY, currentZone.imageEndY, currentZone.imageStartY);
    }

    private double MapValueFromRangeToRange(double value, double min_old, double max_old, double min_new, double max_new){
      return ((value - min_old) * ((max_new - min_new) / (max_old - min_old))) + min_new;
    }

  }
}
