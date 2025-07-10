namespace progMap.ViewModels
{
    /// <summary>
    /// ViewModel для представления типа канала связи
    /// </summary>
    public class ChannelTypeVm : ViewModelBase
    {
        /// <summary>
        /// Название типа канала 
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Тип класса канала (например, typeof(UdpChannel), typeof(SerialChannel))
        /// </summary>
        public Type Type { get; }

        public ChannelTypeVm(string name, Type type)
        {
            Name = name;
            Type = type;
        }

        public override string ToString() => Name;
    }
}
