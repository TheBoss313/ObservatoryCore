﻿namespace Observatory.UI
{
    internal class ThemeManager
    {
        #region Constructor
        private ThemeManager()
        {
            controls = new List<object>();
            Themes = new()
            {
                { "Dark", DarkTheme },
                { "Light", LightTheme }
            };
            LoadThemes();
            SelectedTheme = string.IsNullOrWhiteSpace(Properties.Core.Default.Theme)
                ? Properties.Core.Default.Theme : "Dark";
        }
        #endregion

        #region Singleton Boilerplate
        public static ThemeManager GetInstance
        {
            get
            {
                return _instance.Value;
            }
        }
        private static HashSet<string> _excludedControlNames = new()
        {
            "ColourButton",
        };
        private static readonly Lazy<ThemeManager> _instance = new(() => new ThemeManager());
        #endregion

        #region Private Fields/Properties

        private Dictionary<string, Dictionary<string, Color>> Themes;

        private string SelectedTheme;

        private readonly List<object> controls;

        #region Hardcoded Themes
        static private Dictionary<string, Color> LightTheme = new Dictionary<string, Color>
        {
            { "Default.ForeColor", SystemColors.ControlText },
            { "Default.BackColor", SystemColors.Control },
            { "Button.BackColor", SystemColors.ControlLight },
            { "ComboBox.BackColor", SystemColors.Window },
            { "ComboBox.ForeColor", SystemColors.WindowText },
            { "LinkLabel.ActiveLinkColor", Color.Red },
            { "LinkLabel.DisabledLinkColor", Color.FromArgb(0x85, 0x85, 0x85) },
            { "LinkLabel.LinkColor", Color.FromArgb(0, 0, 0xFF) },
            { "LinkLabel.VisitedLinkColor", Color.FromArgb(0x80, 0, 0x80) },
            { "ListView.ForeColor", SystemColors.WindowText },
            { "ListView.BackColor", SystemColors.Window },
            { "NumericUpDown.ForeColor", SystemColors.WindowText },
            { "NumericUpDown.BackColor", SystemColors.Window },
            { "TrackBar.ForeColor", SystemColors.WindowText },
            { "UpDownButtons.BackColor", SystemColors.Window },
            { "UpDownButtons.ForeColor", SystemColors.WindowText },
            { "UpDownEdit.BackColor", SystemColors.Window },
            { "UpDownEdit.ForeColor", SystemColors.WindowText }
        };

        static private Dictionary<string, Color> DarkTheme = new Dictionary<string, Color>
        {
            { "Default.ForeColor", Color.LightGray },
            { "Default.BackColor", Color.Black },
            { "Button.ForeColor", Color.LightGray },
            { "Button.BackColor", Color.DimGray },
            { "LinkLabel.ActiveLinkColor", Color.Red },
            { "LinkLabel.DisabledLinkColor", Color.FromArgb(0x85, 0x85, 0x85) },
            { "LinkLabel.LinkColor", Color.FromArgb(0xA0, 0xA0, 0xFF) },
            { "LinkLabel.VisitedLinkColor", Color.FromArgb(0x80, 0, 0x80) },
        };
        #endregion

        #endregion

        #region Private Methods

        private Dictionary<string, Color> DeserializeTheme(ThemeSerializationContainer themeContainer)
        {
            Dictionary<string, Color> parsedTheme = new();
            foreach (var value in themeContainer.Theme)
            {
                var color = DeserializeColor(value.Value);
                parsedTheme.Add(value.Key, color);
            }
            return parsedTheme;
        }

        private Color DeserializeColor(ColorSerializationContainer colorContainer)
        {
            Color color;
            var nameOrRGB = colorContainer;
            if (nameOrRGB.Name != null)
            {
                color = Color.FromName(nameOrRGB.Name);
            }
            else
            {
                color = Color.FromArgb(
                    nameOrRGB.R ?? 0,
                    nameOrRGB.G ?? 0,
                    nameOrRGB.B ?? 0
                    );
            }
            return color;
        }

        private void LoadThemes()
        {
            string savedThemes = Properties.Core.Default.SavedThemes;
            if (string.IsNullOrEmpty(savedThemes)) savedThemes = "[]";
            List<ThemeSerializationContainer>? savedThemeContainers;
            try
            {
                savedThemeContainers = System.Text.Json.JsonSerializer.Deserialize
                    <List<ThemeSerializationContainer>>
                    (savedThemes) ?? new();
            }
            catch
            {
                // If we get an error here just blow away saved themes and start fresh
                savedThemeContainers = new();
            }

            foreach (var savedTheme in savedThemeContainers)
            {
                var theme = DeserializeTheme(savedTheme);
                Themes.Add(savedTheme.Name ?? "", theme);
            }
        }

        private void AddTheme(string themeName, Dictionary<string, Color> theme)
        {
            Themes.Add(themeName, theme);
        }

        private void SaveTheme(ThemeSerializationContainer theme)
        {
            string savedThemes = Properties.Core.Default.SavedThemes;
            List<ThemeSerializationContainer>? savedThemeContainers;
            try
            {
                if (!string.IsNullOrWhiteSpace(savedThemes))
                {
                    savedThemeContainers = System.Text.Json.JsonSerializer.Deserialize
                        <List<ThemeSerializationContainer>>
                        (savedThemes) ?? new();
                }
                else
                {
                    savedThemeContainers = new();
                }
            }
            catch
            {
                // If we get an error here just blow away saved themes and start fresh
                savedThemeContainers = new();
            }

            var existingTheme = savedThemeContainers.Where(saved => saved.Name == theme.Name);
            if (existingTheme.Any())
            {
                existingTheme.First().Theme = theme.Theme;
            }
            else
            {
                savedThemeContainers.Add(theme);
            }

            Properties.Core.Default.SavedThemes = System.Text.Json.JsonSerializer.Serialize(savedThemeContainers);
            Properties.Core.Default.Save();
        }

        private void ApplyTheme(object control)
        {
            var controlType = control.GetType();

            var theme = Themes.ContainsKey(SelectedTheme)
                ? Themes[SelectedTheme] : Themes["Light"];

            if (controlType == typeof(MenuStrip))
            {
                var menuStrip = (MenuStrip)control;
                foreach (ToolStripMenuItem item in menuStrip.Items)
                {
                    ApplyTheme(item);
                }
            }

            foreach (var property in controlType.GetProperties().Where(p => p.PropertyType == typeof(Color)))
            {
                string themeControl = Themes[SelectedTheme].ContainsKey(controlType.Name + "." + property.Name)
                    ? controlType.Name
                    : "Default";

                if (Themes[SelectedTheme].ContainsKey(themeControl + "." + property.Name))
                    property.SetValue(control, Themes[SelectedTheme][themeControl + "." + property.Name]);
            }

            if (control.GetType().IsSubclassOf(typeof(Control)))
            {
                var typedControl = (Control)control;
                if (typedControl.HasChildren)
                    foreach (Control child in typedControl.Controls)
                    {
                        if (_excludedControlNames.Contains(child.Name)) continue;
                        ApplyTheme(child);
                    }
            }

        }

        #endregion

        #region Private Classes
        // Used for System.Text.Json deserialization of loaded themes
        private class ThemeSerializationContainer
        {
            public string? Name { get; set; }
            public Dictionary<string, ColorSerializationContainer>? Theme { get; set; }
        }

        private class ColorSerializationContainer
        {
            public int? R { get; set; }
            public int? G { get; set; }
            public int? B { get; set; }
            public string? Name { get; set; }
        }

        #endregion

        #region Public Properties

        public List<string> GetThemes
        {
            get => Themes.Keys.ToList();
        }

        public string CurrentTheme
        {
            get => SelectedTheme;

            set
            {
                if (Themes.ContainsKey(value))
                {
                    SelectedTheme = value;
                    foreach (var control in controls)
                    {
                        ApplyTheme(control);
                    }
                    Properties.Core.Default.Theme = value;
                    Properties.Core.Default.Save();
                }
            }
        }

        public Dictionary<string, Color> CurrentThemeDetails
        {
            get => Themes[SelectedTheme];
        }

        #endregion

        #region Public Methods

        public void RegisterControl(object control)
        {
            controls.Add(control);
            ApplyTheme(control);
        }

        public void UnregisterControl(object control)
        {
            controls.Remove(control);
        }

        public string AddTheme(string themeJson)
        {
            ThemeSerializationContainer? themeContainer;
            try
            {
                themeContainer = System.Text.Json.JsonSerializer.Deserialize
                    <ThemeSerializationContainer>
                    (themeJson);
            }
            catch (Exception ex)
            {
                throw new Exception("Theme file error:\r\n" + ex.Message);
            }

            if (themeContainer == null)
                throw new Exception("File does not contain a theme.");
            if (themeContainer.Name == null)
                throw new Exception("Missing theme name.");
            if (themeContainer.Theme == null)
                throw new Exception("Missing theme content.");

            SaveTheme(themeContainer);

            var parsedTheme = DeserializeTheme(themeContainer);

            AddTheme(themeContainer.Name, parsedTheme);

            return themeContainer.Name;
        }

        #endregion
    }
}
