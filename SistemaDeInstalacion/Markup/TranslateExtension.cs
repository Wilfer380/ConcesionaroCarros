using ConcesionaroCarros.Services;
using System;
using System.Windows.Data;
using System.Windows.Markup;

namespace ConcesionaroCarros.Markup
{
    [MarkupExtensionReturnType(typeof(string))]
    public sealed class TranslateExtension : MarkupExtension
    {
        public TranslateExtension()
        {
        }

        public TranslateExtension(string key)
        {
            Key = key;
        }

        [ConstructorArgument("key")]
        public string Key { get; set; }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            if (string.IsNullOrWhiteSpace(Key))
                return string.Empty;

            var binding = new Binding($"[{Key}]")
            {
                Source = LocalizationService.Instance,
                Mode = BindingMode.OneWay,
                FallbackValue = $"[{Key}]",
                TargetNullValue = $"[{Key}]"
            };

            return binding.ProvideValue(serviceProvider);
        }
    }
}
