using System;
using System.ComponentModel;
using CleanCode.NET.Common;
using Microsoft.VisualStudio.Shell;

namespace CleanCode.NET
{
    public class OptionPageGrid : DialogPage
    {
        [Category("Clean Code .NET")]
        [DisplayName("Switch enum check severity")]
        [Description("Switch enum check (CCN0001) severity")]
        public Severity SwitchEnumSeverity { get; set; }

        [Category("Clean Code .NET")]
        [DisplayName("Switch interface check severity")]
        [Description("Switch interface check (CCN0002) severity")]
        public Severity SwitchInterfaceSeverity { get; set; }

        [Category("Clean Code .NET")]
        [DisplayName("Switch class check severity")]
        [Description("Switch class check (CCN0003) severity")]
        public Severity SwitchClassSeverity { get; set; }

        [Category("Clean Code .NET")]
        [DisplayName("Exceptions check severity")]
        [Description("Exceptions check (CCN0021) severity")]
        public Severity ExceptionsSeverity { get; set; }

        protected override void OnApply(PageApplyEventArgs e)
        {
            base.OnApply(e);
            ApplyToSettings(Settings.Current);
        }

        internal void ApplyToSettings(Settings settings)
        {
            settings.SwitchEnumSeverity = SwitchEnumSeverity;
            settings.SwitchInterfaceSeverity = SwitchInterfaceSeverity;
            settings.SwitchClassSeverity = SwitchClassSeverity;
            settings.ExceptionsSeverity = ExceptionsSeverity;
            settings.IsInitialized = true;
        }
    }
}