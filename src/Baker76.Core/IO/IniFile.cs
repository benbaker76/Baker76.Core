using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;

namespace Baker76.Core.IO
{
    public class IniFile
    {
        private Dictionary<string, Dictionary<string, string>> _data;
        private string _fileName;

        public IniFile()
        {
            _data = new Dictionary<string, Dictionary<string, string>>();
        }

        public IniFile(string fileName)
        {
            _fileName = fileName;
            _data = new Dictionary<string, Dictionary<string, string>>();
            LoadFromFile(fileName);
        }

        public IniFile(Stream stream)
        {
            _data = new Dictionary<string, Dictionary<string, string>>();
            LoadFromStream(stream);
        }

        private void LoadFromFile(string fileName)
        {
            using (var stream = new FileStream(fileName, FileMode.Open, FileAccess.Read))
            {
                LoadFromStream(stream);
            }
        }

        private void LoadFromStream(Stream stream)
        {
            using (var reader = new StreamReader(stream))
            {
                string line;
                string currentSection = null;

                while ((line = reader.ReadLine()) != null)
                {
                    line = line.Trim();

                    // Ignore comments
                    if (line.StartsWith("#"))
                        continue;

                    // Section
                    if (line.StartsWith("[") && line.EndsWith("]"))
                    {
                        currentSection = line.Substring(1, line.Length - 2).Trim();
                        if (!_data.ContainsKey(currentSection))
                        {
                            _data[currentSection] = new Dictionary<string, string>();
                        }
                    }
                    // Key-value pair
                    else if (currentSection != null && line.Contains("="))
                    {
                        var parts = line.Split(new[] { '=' }, 2);

                        if (parts.Length == 2)
                        {
                            var key = parts[0].Trim();
                            var value = parts[1].Trim();

                            _data[currentSection][key] = value;
                        }
                    }
                }
            }
        }

        public void SaveToFile(string fileName)
        {
            using (var stream = new FileStream(fileName, FileMode.Create, FileAccess.Write))
            {
                SaveToStream(stream);
            }
        }

        public void SaveToStream(Stream stream)
        {
            using (var writer = new StreamWriter(stream))
            {
                foreach (var section in _data)
                {
                    writer.WriteLine($"[{section.Key}]");
                    foreach (var kvp in section.Value)
                        writer.WriteLine($"{kvp.Key}={kvp.Value}");
                }
            }
        }

        public void AddSection(string section)
        {
            if (_data.ContainsKey(section))
                return;

            _data[section] = new Dictionary<string, string>();
        }

        public void RemoveSection(string section)
        {
            if (!_data.ContainsKey(section))
                return;

            _data.Remove(section);
        }

        public void SetValue<T>(string section, string key, T value)
        {
            AddSection(section);
            _data[section][key] = value?.ToString() ?? string.Empty;
        }

        public T GetValue<T>(string section, string key, T defaultValue = default)
        {
            if (_data.ContainsKey(section) && _data[section].ContainsKey(key))
            {
                string value = _data[section][key];
                try
                {
                    TypeConverter converter = TypeDescriptor.GetConverter(typeof(T));
                    if (converter != null && converter.CanConvertFrom(typeof(string)))
                    {
                        return (T)converter.ConvertFromString(value);
                    }
                    return (T)Convert.ChangeType(value, typeof(T));
                }
                catch
                {
                    // Return default value if conversion fails
                    return defaultValue;
                }
            }
            return defaultValue;
        }

        public void RemoveKey(string section, string key)
        {
            if (!_data.ContainsKey(section))
                return;

            if (!_data[section].ContainsKey(key))
                return;

            _data[section].Remove(key);
        }

        public IEnumerable<string> EnumerateSections()
        {
            return _data.Keys;
        }

        public IEnumerable<string> EnumerateKeys(string section)
        {
            if (_data.ContainsKey(section))
                return _data[section].Keys;

            return new List<string>();
        }
    }
}