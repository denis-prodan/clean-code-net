using System;

namespace CleanCode.NET.Common
{
    public class Settings
    {
        private static Settings current = new Settings();

        public static Settings Current => current;

        public bool IsInitialized { get; set; }

        public bool SwitchEnum { get; set; }

        public bool SwitchInterface { get; set; }

        public bool SwitchClass { get; set; }

        public bool ExceptionsNoCheck { get; set; }

        public bool ExceptionsRethrowSame { get; set; }

        public bool ExceptionsRwthrowWithoutInner { get; set; }

        public bool ConstructoNullCheck { get; set; }

        public bool NamedParameters { get; set; }

        /// <summary>
        /// If not initialized, then should process. Otherwise, use setting.
        /// </summary>
        /// <param name="getter">Setting getter.</param>
        /// <returns></returns>
        public bool ShouldProceed(Func<Settings, bool> getter) => getter.Invoke(this) || !IsInitialized;
    }
}
