namespace CleanCode.NET.Common
{
    public class Settings
    {
        private static Settings current = new Settings();

        public static Settings Current => current;

        public bool IsInitialized { get; set; }

        public Severity SwitchEnumSeverity { get; set; }

        public Severity SwitchInterfaceSeverity { get; set; }

        public Severity SwitchClassSeverity { get; set; }

        public Severity ExceptionsSeverity { get; set; }
    }
}
