using progMap.Core;

namespace progMap.ViewModels
{
    /// <summary>
    /// Базовый класс ViewModel для конфигурации секции
    /// </summary>
    public abstract class SectionConfigurationBaseVm : ViewModelBase
    {
        protected SectionConfigurationBase Model;

        /// <summary>
        /// Имя секции
        /// </summary>
        public string Name
        {
            get => Model.Name;
            set => Model.Name = value;
        }

        /// <summary>
        /// Адрес секции в памяти
        /// </summary>
        public uint Address
        {
            get => Model.Address;
            set => Model.Address = value;
        }

        /// <summary>
        /// Размер секции в памяти
        /// </summary>
        public uint Size
        {
            get => Model.Size;
            set => Model.Size = value;
        }

        /// <summary>
        /// Флаг, указывающий является ли секция виртуальной
        /// </summary>
        public bool IsVirtual
        {
            get => Model.IsVirtual;
            set => Model.IsVirtual = value;
        }

        /// <summary>
        /// Флаг, указывающий является ли секция физической
        /// </summary>
        public bool IsPhysical => !Model.IsVirtual;

        public SectionConfigurationBaseVm(SectionConfigurationBase model) => Model = model;

        /// <summary>
        /// Метод для получения базовой модели конфигурации
        /// </summary>
        public SectionConfigurationBase GetModel() => Model;


        public override string ToString() => Name;
    }
}