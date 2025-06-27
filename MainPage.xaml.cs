using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Dispatching;
using Microsoft.Maui.ApplicationModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Timers;
using SQLite;
using HeatMapTracker.Models;
using Microsoft.Maui.Devices.Sensors;
using Timer = System.Timers.Timer;

namespace HeatMapTracker
{
    public partial class MainPage : ContentPage
    {
        private Timer _locationTimer;
        private SQLiteConnection _database;
        private List<Location> _locations = new();

        public MainPage()
        {
            SQLitePCL.Batteries.Init();
            InitializeComponent();

            string dbPath = Path.Combine(FileSystem.AppDataDirectory, "locations.db");
            _database = new SQLiteConnection(dbPath);
            _database.CreateTable<LocationData>();

            _locationTimer = new Timer(5000); // every 5 seconds
            _locationTimer.Elapsed += TrackLocation;
            // Timer will start after permissions are granted
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            var status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
            if (status == PermissionStatus.Granted)
            {
                _locationTimer.Start();
            }
            else
            {
                await DisplayAlert("Permission Required", "Location permission is needed to track heat map data.", "OK");
            }
        }

        private async void TrackLocation(object sender, ElapsedEventArgs e)
        {
            try
            {
                var location = await Geolocation.GetLastKnownLocationAsync();
                if (location != null)
                {
                    _locations.Add(location);

                    _database.Insert(new LocationData
                    {
                        Latitude = location.Latitude,
                        Longitude = location.Longitude,
                        Timestamp = DateTime.UtcNow
                    });

                    MainThread.BeginInvokeOnMainThread(UpdateHeatMap);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Location error: {ex.Message}");
            }
        }

        private void UpdateHeatMap()
        {
            heatMap.Pins.Clear();
            foreach (var loc in _locations)
            {
                heatMap.Pins.Add(new Pin
                {
                    Location = new Location(loc.Latitude, loc.Longitude),
                    Label = "Visited",
                    Address = $"Lat: {loc.Latitude}, Lon: {loc.Longitude}"
                });
            }
        }
    }
}
