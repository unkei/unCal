﻿using System;
using System.IO.IsolatedStorage;
using System.Windows.Media;

namespace unCal
{
    public class AppSettings
    {
        IsolatedStorageSettings settings;

        const string CultureSettingKeyName = "Culture";
        const string LastUpdatedKeyName = "LastUpdated";
        const string ForegroundColorKeyName = "ForegroundColor";
        const string BackgroundColorKeyName = "BackgroundColor";
        const string CultureSettingDefault = "";
        //const DateTime LastUpdatedDefault = DateTime.Today.AddDays(-1); // yesterday

        public AppSettings()
        {
            // Get the settings for this application.
            settings = IsolatedStorageSettings.ApplicationSettings;
        }

        public bool AddOrUpdateValue(string Key, Object value)
        {
            bool valueChanged = false;

            if (settings.Contains(Key))
            {
                if (settings[Key] != value)
                {
                    settings[Key] = value;
                    valueChanged = true;
                }
            }
            else
            {
                settings.Add(Key, value);
                valueChanged = true;
            }
           return valueChanged;
        }

        public T GetValueOrDefault<T>(string Key, T defaultValue)
        {
            T value;

            // If the key exists, retrieve the value.
            if (settings.Contains(Key))
            {
                value = (T)settings[Key];
            }
            // Otherwise, use the default value.
            else
            {
                value = defaultValue;
            }
            return value;
        }

        public void Save()
        {
            settings.Save();
        }

        public string CultureSetting
        {
            get
            {
                return GetValueOrDefault<string>(CultureSettingKeyName, CultureSettingDefault);
            }
            set
            {
                if (AddOrUpdateValue(CultureSettingKeyName, value))
                {
                    Save();
                }
            }
        }

        public DateTime LastUpdated
        {
            get
            {
                return GetValueOrDefault<DateTime>(LastUpdatedKeyName, DateTime.Today.AddDays(-1));
            }
            set
            {
                if (AddOrUpdateValue(LastUpdatedKeyName, value))
                {
                    Save();
                }
            }
        }

        public Color ForegroundColor
        {
            get
            {
                return GetValueOrDefault<Color>(ForegroundColorKeyName, Colors.White);
            }
            set
            {
                if (AddOrUpdateValue(ForegroundColorKeyName, value))
                {
                    Save();
                }
            }
        }

        public Color BackgroundColor
        {
            get
            {
                return GetValueOrDefault<Color>(BackgroundColorKeyName, Colors.Black);
            }
            set
            {
                if (AddOrUpdateValue(BackgroundColorKeyName, value))
                {
                    Save();
                }
            }
        }

        public bool updateCultureIfChanged(string c)
        {
            bool ret = false;

            if (c != CultureSetting)
            {
                CultureSetting = c;
                ret = true;
            }
            return ret;
        }

        public bool updateLastUpdatedIfChanged(DateTime d)
        {
            bool ret = false;

            if (d != LastUpdated)
            {
                LastUpdated = d;
                ret = true;
            }
            return ret;
        }

        public bool updateColorIfChanged(Color fg, Color bg)
        {
            bool ret = false;

            if (fg != ForegroundColor)
            {
                ForegroundColor = fg;
                ret = true;
            }
            if (bg != BackgroundColor)
            {
                BackgroundColor = bg;
                ret = true;
            }
            return ret;
        }

    }
}
