using System.ComponentModel;
using CleanCode.NET.Common;
using Microsoft.VisualStudio.Shell;

namespace CleanCode.NET
{
    public class OptionPageGrid : DialogPage
    {
        private const string Category = "Clean Code .NET";

        [Category(Category)]
        [DefaultValue(true)]
        [DisplayName("(CCN0001) Validate switch for enums")]
        public bool SwitchEnum { get; set; }

        [Category(Category)]
        [DefaultValue(true)]
        [DisplayName("(CCN0002) Validate switch for interfaces")]
        public bool SwitchInterface { get; set; }

        [Category(Category)]
        [DefaultValue(true)]
        [DisplayName("(CCN0003) Validate switch for classes")]
        public bool SwitchClass { get; set; }

        [Category(Category)]
        [DefaultValue(true)]
        [DisplayName("(CCN0011) Require constructors to have null checks for parameters")]
        public bool ConstructoNullCheck { get; set; }

        [Category(Category)]
        [DefaultValue(true)]
        [DisplayName("(CCN0021) Exceptions are not used in catch statement")]
        public bool ExceptionsNoCheck { get; set; }

        [Category(Category)]
        [DefaultValue(true)]
        [DisplayName("(CCN0022) Exceptions rethrow")]
        public bool ExceptionsRethrowSame { get; set; }

        [Category(Category)]
        [DefaultValue(true)]
        [DisplayName("(CCN0023) Exception rethrow without inner")]
        public bool ExceptionsRethrowWithoutInner { get; set; }

        [Category(Category)]
        [DefaultValue(true)]
        [DisplayName("(CCN0041) Require to have parameter names")]
        public bool NamedParameters { get; set; }

        protected override void OnApply(PageApplyEventArgs e)
        {
            base.OnApply(e);
            ApplyToSettings(Settings.Current);
        }

        internal void ApplyToSettings(Settings settings)
        {
            settings.SwitchEnum = SwitchEnum;
            settings.SwitchInterface= SwitchInterface;
            settings.SwitchClass = SwitchClass;
            settings.ExceptionsNoCheck = ExceptionsNoCheck;
            settings.ExceptionsRethrowSame = ExceptionsRethrowSame;
            settings.ExceptionsRwthrowWithoutInner = ExceptionsRethrowWithoutInner;
            settings.ConstructoNullCheck = ConstructoNullCheck;
            settings.NamedParameters = NamedParameters;
            settings.IsInitialized = true;
        }
    }
}