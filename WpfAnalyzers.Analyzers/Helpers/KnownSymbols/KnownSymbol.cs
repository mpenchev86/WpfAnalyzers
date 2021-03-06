// ReSharper disable InconsistentNaming
namespace WpfAnalyzers
{
    internal static class KnownSymbol
    {
        internal static readonly ObjectType Object = new ObjectType();
        internal static readonly QualifiedType String = Create("System.String");
        internal static readonly NullableType Nullable = new NullableType();

        internal static readonly QualifiedType CallerMemberNameAttribute = new QualifiedType("System.Runtime.CompilerServices.CallerMemberNameAttribute");
        internal static readonly INotifyPropertyChangedType INotifyPropertyChanged = new INotifyPropertyChangedType();
        internal static readonly QualifiedType PropertyChangedEventArgs = new QualifiedType("System.ComponentModel.PropertyChangedEventArgs");
        internal static readonly PropertyChangedEventHandlerType PropertyChangedEventHandler = new PropertyChangedEventHandlerType();

        internal static readonly DependencyObjectType DependencyObject = new DependencyObjectType();
        internal static readonly FrameworkElementType FrameworkElement = new FrameworkElementType();
        internal static readonly DependencyPropertyType DependencyProperty = new DependencyPropertyType();
        internal static readonly DependencyPropertyKeyType DependencyPropertyKey = new DependencyPropertyKeyType();
        internal static readonly QualifiedType PropertyMetadata = Create("System.Windows.PropertyMetadata");
        internal static readonly QualifiedType DependencyPropertyChangedEventArgs = Create("System.Windows.DependencyPropertyChangedEventArgs");

        internal static readonly QualifiedType PropertyChangedCallback = Create("System.Windows.PropertyChangedCallback");
        internal static readonly QualifiedType CoerceValueCallback = Create("System.Windows.CoerceValueCallback");
        internal static readonly QualifiedType ValidateValueCallback = Create("System.Windows.ValidateValueCallback");

        internal static readonly QualifiedType FontFamily = Create("System.Windows.Media.FontFamily");
        internal static readonly QualifiedType Freezable = Create("System.Windows.Freezable");
        internal static readonly QualifiedType DataTemplateSelector = Create("System.Windows.Controls.DataTemplateSelector");
        internal static readonly QualifiedType MarkupExtension = Create("System.Windows.Markup.MarkupExtension");
        internal static readonly QualifiedType IValueConverter = Create("System.Windows.Data.IValueConverter");
        internal static readonly QualifiedType IMultiValueConverter = Create("System.Windows.Data.IMultiValueConverter");

        internal static readonly XmlnsPrefixAttributeType XmlnsPrefixAttribute = new XmlnsPrefixAttributeType();
        internal static readonly XmlnsDefinitionAttributeType XmlnsDefinitionAttribute = new XmlnsDefinitionAttributeType();

        private static QualifiedType Create(string qualifiedName)
        {
            return new QualifiedType(qualifiedName);
        }
    }
}